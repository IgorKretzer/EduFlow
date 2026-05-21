using EduFlow.Api.Extensions;
using EduFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IEnrollmentAnalyticsService _enrollments;

    public AnalyticsController(IEnrollmentAnalyticsService enrollments) => _enrollments = enrollments;

    [HttpGet("enrollments")]
    public async Task<IActionResult> ListEnrollments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? unitCode = null,
        [FromQuery] string? risk = null,
        [FromQuery] bool riskOnly = false,
        CancellationToken ct = default)
    {
        var tenantId = User.GetRequiredTenantId();
        var result = await _enrollments.ListPagedAsync(
            tenantId, page, pageSize, search, unitCode, risk, riskOnly, ct);
        return Ok(result);
    }

    [HttpGet("enrollments/{enrollmentCode}")]
    public async Task<IActionResult> GetEnrollment(string enrollmentCode, CancellationToken ct)
    {
        var tenantId = User.GetRequiredTenantId();
        var detail = await _enrollments.GetByEnrollmentCodeAsync(tenantId, enrollmentCode, ct);
        return detail is null ? NotFound() : Ok(detail);
    }
}
