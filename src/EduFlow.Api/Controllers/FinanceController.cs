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

    [HttpGet("pulse")]
    public async Task<ActionResult<FinancePulseDto>> GetPulse(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] string dataBasis,
        [FromQuery] Guid? unitId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        if (year < 2000 || year > 2100)
            return BadRequest("Ano inválido.");
        if (month is < 1 or > 12)
            return BadRequest("Mês inválido.");

        var tenantId = User.GetRequiredTenantId();
        var filter = new DashboardFilterDto(unitId, from, to);
        var result = await _finance.GetPulseAsync(tenantId, year, month, dataBasis, filter, ct);
        return Ok(result);
    }

    [HttpGet("goals")]
    public async Task<ActionResult<IReadOnlyList<FinanceGoalDto>>> GetGoals(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? unitId,
        CancellationToken ct)
    {
        if (year < 2000 || year > 2100)
            return BadRequest("Ano inválido.");
        if (month is < 1 or > 12)
            return BadRequest("Mês inválido.");

        var tenantId = User.GetRequiredTenantId();
        var result = await _finance.GetGoalsAsync(tenantId, year, month, unitId, ct);
        return Ok(result);
    }

    [HttpPut("goals/{key}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<FinanceGoalDto>> UpsertGoal(
        [FromRoute] string key,
        [FromBody] UpsertFinanceGoalRequest request,
        CancellationToken ct)
    {
        try
        {
            var tenantId = User.GetRequiredTenantId();
            var result = await _finance.UpsertGoalAsync(tenantId, key, request, ct);
            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
