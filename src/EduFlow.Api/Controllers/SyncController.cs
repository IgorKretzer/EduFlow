using EduFlow.Api.Extensions;
using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;

namespace EduFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/sync")]
public sealed class SyncController : ControllerBase
{
    private readonly ISyncOrchestrator _sync;

    public SyncController(ISyncOrchestrator sync) => _sync = sync;

    /// <summary>
    /// Dispara sincronização ERP → Connector → RabbitMQ. Frontend nunca acessa o ERP.
    /// </summary>
    [HttpPost("trigger")]
    [EnableRateLimiting("sync")]
    [RequestTimeout("sync")]
    public async Task<ActionResult<SyncJobStatusDto>> Trigger(
        [FromBody] TriggerSyncRequest request,
        CancellationToken ct)
    {
        var tenantId = User.GetRequiredTenantId();
        var status = await _sync.TriggerSyncAsync(tenantId, request, ct);
        return Ok(status);
    }
}
