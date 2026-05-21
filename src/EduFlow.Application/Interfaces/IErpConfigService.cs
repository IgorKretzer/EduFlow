using EduFlow.Application.DTOs;

namespace EduFlow.Application.Interfaces;

public interface IErpConfigService
{
    Task<ErpConfigDto?> GetAsync(Guid tenantId, CancellationToken ct = default);
    Task<ErpConfigDto> UpsertAsync(Guid tenantId, UpdateErpConfigRequest request, CancellationToken ct = default);
    SyncSchedulerStatusDto GetSchedulerStatus();
}
