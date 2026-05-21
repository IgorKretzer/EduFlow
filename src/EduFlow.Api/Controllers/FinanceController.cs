using EduFlow.Api.Extensions;
using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/finance")]
public sealed class FinanceController : ControllerBase
{
    private readonly IFinanceAnalyticsService _finance;

    public FinanceController(IFinanceAnalyticsService finance) => _finance = finance;

    [HttpGet("receivables")]
    public async Task<ActionResult<FinanceReceivablesDto>> GetReceivables(
        [FromQuery] Guid? unitId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var tenantId = User.GetRequiredTenantId();
        var filter = new DashboardFilterDto(unitId, from, to);
        var result = await _finance.GetReceivablesAsync(tenantId, filter, ct);
        return Ok(result);
    }

    [HttpGet("cash-flow")]
    public async Task<ActionResult<FinanceCashFlowDto>> GetCashFlow(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? unitId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        if (year < 2000 || year > 2100)
            return BadRequest("Ano inválido.");

        var tenantId = User.GetRequiredTenantId();
        var filter = new DashboardFilterDto(unitId, from, to);
        var result = await _finance.GetCashFlowAsync(tenantId, year, month, filter, ct);
        return Ok(result);
    }
}
