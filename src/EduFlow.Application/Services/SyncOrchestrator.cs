using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EduFlow.Application.Services;

public sealed class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IEnumerable<IErpConnector> _connectors;
    private readonly ITenantErpConfigRepository _tenantConfig;
    private readonly IIngestionStore _ingestionStore;
    private readonly ICanonicalNormalizer _normalizer;
    private readonly IEventPublisher _publisher;
    private readonly IEtlService _etl;
    private readonly ILogger<SyncOrchestrator> _logger;

    public SyncOrchestrator(
        IEnumerable<IErpConnector> connectors,
        ITenantErpConfigRepository tenantConfig,
        IIngestionStore ingestionStore,
        ICanonicalNormalizer normalizer,
        IEventPublisher publisher,
        IEtlService etl,
        ILogger<SyncOrchestrator> logger)
    {
        _connectors = connectors;
        _tenantConfig = tenantConfig;
        _ingestionStore = ingestionStore;
        _normalizer = normalizer;
        _publisher = publisher;
        _etl = etl;
        _logger = logger;
    }

    public async Task<SyncJobStatusDto> TriggerSyncAsync(
        Guid tenantId,
        TriggerSyncRequest request,
        CancellationToken ct = default)
    {
        var jobId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var processed = 0;

        try
        {
            var config = await _tenantConfig.GetAsync(tenantId, ct)
                ?? throw new InvalidOperationException("Configuração ERP não encontrada para o tenant.");

            var connector = _connectors.FirstOrDefault(c => c.ProviderKey == config.ProviderKey)
                ?? throw new InvalidOperationException($"Connector '{config.ProviderKey}' não registrado.");

            var context = new ErpSyncContext(
                tenantId,
                config.EndpointUrl,
                config.Username,
                config.Password,
                config.PageSize,
                request.FromDate,
                config.SearchParametersStudents,
                config.SearchParametersFinancial,
                null,
                config.SearchParametersContracts);

            var entityKey = request.EntityType.ToLowerInvariant();
            var batches = entityKey switch
            {
                "students" => [await connector.FetchStudentsAsync(context, ct)],
                "financial" => [await connector.FetchFinancialAsync(context, ct)],
                "payables" => await FetchPayablesBatchAsync(connector, context, ct),
                "categories" => await FetchCategoriesBatchAsync(connector, context, ct),
                "contracts" => [await connector.FetchContractsAsync(context, ct)],
                "financial-full" => await FetchFinancialFullAsync(connector, context, ct),
                _ => throw new ArgumentException($"EntityType inválido: {request.EntityType}")
            };

            foreach (var records in batches)
            {
                foreach (var record in records)
                {
                    await _ingestionStore.SaveRawAsync(new IngestionRawPayload
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ErpProvider = config.ProviderKey,
                        EntityType = record.EntityType,
                        PayloadXml = record.RawXml,
                        CorrelationId = jobId.ToString()
                    }, ct);

                    await PublishNormalizedAsync(tenantId, config.ProviderKey, record, jobId.ToString(), ct);
                    processed++;
                }
            }

            await _publisher.PublishAsync(
                EventRoutingKeys.AnalyticsRefresh,
                new IntegrationEvent<object>(
                    tenantId,
                    EventRoutingKeys.AnalyticsRefresh,
                    new { },
                    DateTime.UtcNow,
                    jobId.ToString()),
                ct);

            _logger.LogInformation(
                "Sync concluído. JobId={JobId} TenantId={TenantId} Entity={Entity} Count={Count}",
                jobId, tenantId, request.EntityType, processed);

            return new SyncJobStatusDto(jobId, request.EntityType, "completed", processed, startedAt, DateTime.UtcNow, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync falhou. JobId={JobId} TenantId={TenantId}", jobId, tenantId);
            return new SyncJobStatusDto(jobId, request.EntityType, "failed", processed, startedAt, DateTime.UtcNow, ex.Message);
        }
    }

    private static async Task<IReadOnlyList<RawErpRecord>[]> FetchPayablesBatchAsync(
        IErpConnector connector,
        ErpSyncContext context,
        CancellationToken ct)
    {
        if (connector is IErpExtendedConnector ext)
            return [await ext.FetchPayablesAsync(context, ct)];
        return [[]];
    }

    private static async Task<IReadOnlyList<RawErpRecord>[]> FetchCategoriesBatchAsync(
        IErpConnector connector,
        ErpSyncContext context,
        CancellationToken ct)
    {
        if (connector is IErpExtendedConnector ext)
            return [await ext.FetchCategoriesAsync(context, ct)];
        return [[]];
    }

    private static async Task<IReadOnlyList<RawErpRecord>[]> FetchFinancialFullAsync(
        IErpConnector connector,
        ErpSyncContext context,
        CancellationToken ct)
    {
        if (connector is not IErpExtendedConnector ext)
            return [await connector.FetchFinancialAsync(context, ct)];

        var receivables = connector.FetchFinancialAsync(context, ct);
        var payables = ext.FetchPayablesAsync(context, ct);
        var categories = ext.FetchCategoriesAsync(context, ct);
        await Task.WhenAll(receivables, payables, categories);

        return [await receivables, await payables, await categories];
    }

    private async Task PublishNormalizedAsync(
        Guid tenantId,
        string provider,
        RawErpRecord record,
        string correlationId,
        CancellationToken ct)
    {
        switch (record.EntityType)
        {
            case "student":
                var student = _normalizer.NormalizeStudent(provider, record.RawXml);
                if (student is not null)
                    await _publisher.PublishAsync(EventRoutingKeys.StudentUpdated,
                        new IntegrationEvent<CanonicalStudent>(
                            tenantId, EventRoutingKeys.StudentUpdated,
                            student with { TenantId = tenantId },
                            DateTime.UtcNow, correlationId), ct);
                break;
            case "financial":
                var financial = _normalizer.NormalizeFinancial(provider, record.RawXml);
                if (financial is not null)
                    await _publisher.PublishAsync(EventRoutingKeys.FinancialChanged,
                        new IntegrationEvent<CanonicalFinancial>(
                            tenantId, EventRoutingKeys.FinancialChanged,
                            financial with { TenantId = tenantId },
                            DateTime.UtcNow, correlationId), ct);
                break;
            case "payable":
                var payable = _normalizer.NormalizePayable(provider, record.RawXml);
                if (payable is not null)
                    await _publisher.PublishAsync(EventRoutingKeys.FinancialChanged,
                        new IntegrationEvent<CanonicalFinancial>(
                            tenantId, EventRoutingKeys.FinancialChanged,
                            payable with { TenantId = tenantId },
                            DateTime.UtcNow, correlationId), ct);
                break;
            case "category":
                var category = _normalizer.NormalizeCategory(provider, record.RawXml);
                if (category is not null)
                    await _etl.UpsertCategoryAsync(tenantId, category with { TenantId = tenantId }, ct);
                break;
            case "contract":
                var contract = _normalizer.NormalizeContract(provider, record.RawXml);
                if (contract is not null)
                    await _publisher.PublishAsync(EventRoutingKeys.ContractCreated,
                        new IntegrationEvent<CanonicalContract>(
                            tenantId, EventRoutingKeys.ContractCreated,
                            contract with { TenantId = tenantId },
                            DateTime.UtcNow, correlationId), ct);
                break;
        }
    }
}

public interface IIngestionStore
{
    Task SaveRawAsync(IngestionRawPayload payload, CancellationToken ct);
}
