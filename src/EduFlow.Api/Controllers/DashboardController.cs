using EduFlow.Api.Extensions;
using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly IWebHostEnvironment _env;

    public DashboardController(IDashboardService dashboard, IWebHostEnvironment env)
    {
        _dashboard = dashboard;
        _env = env;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] Guid? unitId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        try
        {
            var tenantId = User.GetRequiredTenantId();
            var filter = new DashboardFilterDto(unitId, from, to);
            var summary = await _dashboard.GetSummaryAsync(tenantId, filter, ct);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Falha ao carregar o painel. Verifique a sincronização e o ambiente de dados.",
                detail = _env.IsDevelopment() ? ex.Message : null
            });
        }
    }
}
