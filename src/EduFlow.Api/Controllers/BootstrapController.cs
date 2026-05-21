using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Infrastructure.Connectors;
using EduFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduFlow.Api.Controllers;

/// <summary>
/// Bootstrap de desenvolvimento — cria tenant demo + config Sponte. Desativado em Production.
/// </summary>
[ApiController]
[Route("api/bootstrap")]
public sealed class BootstrapController : ControllerBase
{
    private readonly StagingDbContext _db;
    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;

    public BootstrapController(StagingDbContext db, IAuthService auth, IWebHostEnvironment env)
    {
        _db = db;
        _auth = auth;
        _env = env;
    }

    [HttpPost("demo")]
    public async Task<ActionResult<LoginResponse>> SetupDemo(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();
        const string slug = "demo";
        var existing = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);
        if (existing is not null)
        {
            var login = await _auth.LoginAsync(
                new LoginRequest("admin@demo.eduflow", "Demo@123"), ct);
            return login is null ? BadRequest("Tenant existe mas login falhou.") : Ok(login);
        }

        var response = await _auth.RegisterTenantAsync(
            new RegisterTenantRequest("Escola Demo", slug, "admin@demo.eduflow", "Demo@123"), ct);

        await _db.ErpConfigs.AddAsync(new TenantErpConfigEntity
        {
            TenantId = response.TenantId,
            ProviderKey = "sponte",
            EndpointUrl = SponteSoapConnector.DefaultServiceUrl,
            Username = "SEU_CODIGO_CLIENTE",
            Password = "SEU_TOKEN",
            PageSize = 100,
            SyncEnabled = true,
            SyncIntervalMinutes = 15
        }, ct);

        await _db.SaveChangesAsync(ct);
        return Ok(response);
    }
}
