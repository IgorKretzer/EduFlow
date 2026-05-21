using EduFlow.Application.DTOs;

namespace EduFlow.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid tenantId, DashboardFilterDto filter, CancellationToken ct = default);
}
