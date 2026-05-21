using EduFlow.Api.Extensions;
using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/erp")]
public sealed class ErpConfigController : ControllerBase
{
    private readonly IErpConfigService _erp;

    public ErpConfigController(IErpConfigService erp) => _erp = erp;

    [HttpGet("config")]
    public async Task<ActionResult<ErpConfigDto>> Get(CancellationToken ct)
    {
        var tenantId = User.GetRequiredTenantId();
        var config = await _erp.GetAsync(tenantId, ct);
        if (config is null)
            return NotFound(new { message = "Configure a integração ERP em Configurações." });

        return Ok(config);
    }

    [HttpPut("config")]
    public async Task<ActionResult<ErpConfigDto>> Put(
        [FromBody] UpdateErpConfigRequest request,
        CancellationToken ct)
    {
        var tenantId = User.GetRequiredTenantId();
        try
        {
            var config = await _erp.UpsertAsync(tenantId, request, ct);
            return Ok(config);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("scheduler")]
    public ActionResult<SyncSchedulerStatusDto> SchedulerStatus() => Ok(_erp.GetSchedulerStatus());
}
