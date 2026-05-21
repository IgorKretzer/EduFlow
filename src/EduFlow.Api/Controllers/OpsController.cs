using System.Security.Claims;
using EduFlow.Application.Interfaces;
using EduFlow.Infrastructure.Ops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/ops")]
public sealed class OpsController : ControllerBase
{
    private readonly OpsStatusService _ops;
    private readonly ITenantContext _tenant;

    public OpsController(OpsStatusService ops, ITenantContext tenant)
    {
        _ops = ops;
        _tenant = tenant;
    }

    /// <summary>Painel operacional: SQL, Rabbit, Workers, filas e último sync.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<OpsStatusResponse>> GetStatus(CancellationToken ct)
    {
        Guid? tenantId = null;
        var claim = User.FindFirstValue("tenant_id");
        if (Guid.TryParse(claim, out var parsed))
            tenantId = parsed;
        else if (_tenant.TenantId.HasValue)
            tenantId = _tenant.TenantId;

        var status = await _ops.GetStatusAsync(tenantId, ct);
        return Ok(status);
    }
}
