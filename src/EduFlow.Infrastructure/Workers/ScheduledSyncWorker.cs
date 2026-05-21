using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlow.Infrastructure.Workers;

/// <summary>
/// Dispara sync ERP → Sponte para cada tenant com agendamento vencido.
/// Não substitui os consumers RabbitMQ — apenas inicia o pipeline.
/// </summary>
public sealed class ScheduledSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SyncSchedulerSettings _settings;
    private readonly ILogger<ScheduledSyncWorker> _logger;

    public ScheduledSyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<SyncSchedulerSettings> settings,
        ILogger<ScheduledSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SyncScheduler desabilitado em configuração.");
            return;
        }

        _logger.LogInformation(
            "ScheduledSyncWorker ativo. Poll={Poll}s Entidades={Entities}",
            _settings.PollIntervalSeconds,
            string.Join(", ", _settings.EntityTypes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDueSyncsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo de sync agendado");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task RunDueSyncsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantErpConfigRepository>();
        var sync = scope.ServiceProvider.GetRequiredService<ISyncOrchestrator>();

        var configs = await repo.GetAllEnabledForSyncAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var config in configs)
        {
            var interval = config.SyncIntervalMinutes > 0
                ? config.SyncIntervalMinutes
                : _settings.DefaultIntervalMinutes;

            if (config.LastSyncAtUtc.HasValue &&
                config.LastSyncAtUtc.Value.AddMinutes(interval) > now)
                continue;

            _logger.LogInformation(
                "Sync agendado iniciando TenantId={TenantId} Intervalo={Interval}min",
                config.TenantId, interval);

            var errors = new List<string>();
            var total = 0;

            foreach (var entityType in _settings.EntityTypes)
            {
                var result = await sync.TriggerSyncAsync(
                    config.TenantId,
                    new TriggerSyncRequest(entityType, FromDate: null),
                    ct);

                total += result.RecordsProcessed;
                if (result.Status == "failed" && !string.IsNullOrEmpty(result.ErrorMessage))
                    errors.Add($"{entityType}: {result.ErrorMessage}");
            }

            var status = errors.Count == 0 ? "completed" : "partial";
            var message = errors.Count == 0
                ? $"{total} registros processados"
                : string.Join("; ", errors);

            await repo.UpdateSyncResultAsync(config.TenantId, now, status, message, ct);

            _logger.LogInformation(
                "Sync agendado concluído TenantId={TenantId} Status={Status} Total={Total}",
                config.TenantId, status, total);
        }
    }
}
