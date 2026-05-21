using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Application.Options;
using Microsoft.Extensions.Options;

namespace EduFlow.Application.Services;

public sealed class ErpConfigService : IErpConfigService
{
    public const string DefaultSponteEndpoint = "https://api.sponteeducacional.net.br/WSAPIEdu.asmx";

    private readonly ITenantErpConfigRepository _repository;
    private readonly SyncSchedulerSettings _scheduler;

    public ErpConfigService(
        ITenantErpConfigRepository repository,
        IOptions<SyncSchedulerSettings> scheduler)
    {
        _repository = repository;
        _scheduler = scheduler.Value;
    }

    public async Task<ErpConfigDto?> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var config = await _repository.GetAsync(tenantId, ct);
        return config is null ? null : ToDto(config);
    }

    public async Task<ErpConfigDto> UpsertAsync(
        Guid tenantId,
        UpdateErpConfigRequest request,
        CancellationToken ct = default)
    {
        if (request.SyncIntervalMinutes < 5)
            throw new ArgumentException("Intervalo mínimo de sync é 5 minutos.");

        var existing = await _repository.GetAsync(tenantId, ct);
        var password = existing?.Password ?? "";

        if (!string.IsNullOrWhiteSpace(request.Password))
            password = request.Password;

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Informe o token/senha do ERP.");

        var provider = NormalizeProvider(request.ProviderKey ?? existing?.ProviderKey ?? "sponte");
        var endpoint = string.IsNullOrWhiteSpace(request.EndpointUrl)
            ? provider == "openapi" ? "https://api.exemplo.com/v1" : DefaultSponteEndpoint
            : request.EndpointUrl.Trim();

        var pageSize = request.PageSize <= 0 ? 100 : request.PageSize;
        var defaultSponteTop = $"Situacao=2|TOP={pageSize}";
        var defaultOpenApiStudents = $"/students?limit={pageSize}";
        var defaultOpenApiFinancial = $"/financial?limit={pageSize}";
        var defaultOpenApiContracts = $"/contracts?limit={pageSize}";

        var config = new TenantErpConfig(
            tenantId,
            provider,
            endpoint,
            request.Username.Trim(),
            password,
            pageSize,
            request.SyncEnabled,
            request.SyncIntervalMinutes,
            existing?.LastSyncAtUtc,
            existing?.LastSyncStatus,
            existing?.LastSyncMessage,
            ResolveSearchParam(
                request.SearchParametersStudents,
                existing?.SearchParametersStudents,
                provider,
                defaultSponteTop,
                defaultOpenApiStudents),
            ResolveSearchParam(
                request.SearchParametersFinancial,
                existing?.SearchParametersFinancial,
                provider,
                defaultSponteTop,
                defaultOpenApiFinancial),
            ResolveSearchParam(
                request.SearchParametersContracts,
                existing?.SearchParametersContracts,
                provider,
                defaultSponteTop,
                defaultOpenApiContracts));

        await _repository.UpsertAsync(config, ct);
        return ToDto(config);
    }

    public SyncSchedulerStatusDto GetSchedulerStatus() =>
        new(_scheduler.Enabled, _scheduler.PollIntervalSeconds, _scheduler.DefaultIntervalMinutes);

    private static string NormalizeProvider(string provider)
    {
        var key = provider.Trim().ToLowerInvariant();
        if (key is "sponte" or "openapi") return key;
        throw new ArgumentException("Fornecedor ERP inválido. Use 'sponte' ou 'openapi'.");
    }

    private static string ResolveSearchParam(
        string? requestValue,
        string? existingValue,
        string provider,
        string sponteDefault,
        string openApiDefault)
    {
        if (!string.IsNullOrWhiteSpace(requestValue))
            return requestValue.Trim();

        if (!string.IsNullOrWhiteSpace(existingValue))
            return existingValue;

        return provider == "openapi" ? openApiDefault : sponteDefault;
    }

    private static ErpConfigDto ToDto(TenantErpConfig c) =>
        new(
            c.ProviderKey,
            c.EndpointUrl,
            c.Username,
            !string.IsNullOrEmpty(c.Password),
            c.PageSize,
            c.SyncEnabled,
            c.SyncIntervalMinutes,
            c.LastSyncAtUtc,
            c.LastSyncStatus,
            c.LastSyncMessage,
            c.SearchParametersStudents,
            c.SearchParametersFinancial,
            c.SearchParametersContracts);
}
