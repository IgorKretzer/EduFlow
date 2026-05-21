using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EduFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public AuthController(IAuthService auth, IWebHostEnvironment env, IConfiguration config)
    {
        _auth = auth;
        _env = env;
        _config = config;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterTenantRequest request, CancellationToken ct)
    {
        var pilotRegistration = _config.GetValue<bool>("Pilot:AllowRegistration");
        if (!_env.IsDevelopment() && !pilotRegistration)
            return NotFound();

        try
        {
            var result = await _auth.RegisterTenantAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
