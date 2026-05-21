using EduFlow.Application.DTOs;

namespace EduFlow.Application.Interfaces;

/// <summary>
/// Orquestra sincronização ERP → Connector → RabbitMQ. Frontend nunca fala com ERP.
/// </summary>
public interface ISyncOrchestrator
{
    Task<SyncJobStatusDto> TriggerSyncAsync(Guid tenantId, TriggerSyncRequest request, CancellationToken ct = default);
}
