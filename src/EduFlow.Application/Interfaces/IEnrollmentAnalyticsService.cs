using EduFlow.Application.DTOs;

namespace EduFlow.Application.Interfaces;

public interface IEnrollmentAnalyticsService
{
    Task<EnrollmentPageDto> ListPagedAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? unitCode = null,
        string? risk = null,
        bool riskOnly = false,
        CancellationToken ct = default);

    Task<EnrollmentDetailDto?> GetByEnrollmentCodeAsync(
        Guid tenantId,
        string enrollmentCode,
        CancellationToken ct = default);
}
