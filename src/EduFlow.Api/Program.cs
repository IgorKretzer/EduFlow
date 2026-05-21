using System.Text;
using EduFlow.Analytics.Queries;
using EduFlow.Api.Health;
using EduFlow.Api.Middleware;
using EduFlow.Infrastructure;
using EduFlow.Infrastructure.Persistence;
using EduFlow.Infrastructure.Security;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "EduFlow.Api")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<RabbitMqHealthOptions>(
    builder.Configuration.GetSection("RabbitMq"));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"] ?? "";

if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase))
    {
        Log.Fatal("Produção: defina Jwt:Secret seguro (≥32 caracteres) via Jwt__Secret ou variável de ambiente.");
        throw new InvalidOperationException(
            "Jwt:Secret inválido em Production. Configure segredos por Key Vault / ambiente.");
    }

    if (jwtSecret.Length < 32)
        throw new InvalidOperationException("Jwt:Secret deve ter pelo menos 32 caracteres em Production.");
}

builder.Services.AddSingleton<RabbitMqConnectionHealthCheck>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<StagingDbContext>("staging-database", tags: ["ready", "db"])
    .AddCheck<RabbitMqConnectionHealthCheck>("rabbitmq", tags: ["ready", "messaging"]);

var jwt = jwtSection.Get<JwtSettings>()
    ?? throw new InvalidOperationException("Seção Jwt ausente.");

builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("sync", TimeSpan.FromMinutes(5));
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
        };
    });

builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                  ?? ["http://localhost:3000", "http://localhost:3001"];

if (builder.Environment.IsProduction())
{
    corsOrigins = corsOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToArray();
    if (corsOrigins.Length == 0)
    {
        throw new InvalidOperationException(
            "Produção: configure Cors:Origins com o domínio do frontend (ex.: https://app.suaescola.com.br).");
    }

    if (corsOrigins.Any(o =>
            o.Contains("SEU_DOMINIO", StringComparison.OrdinalIgnoreCase) ||
            o.Contains("SEU-PROJETO", StringComparison.OrdinalIgnoreCase) ||
            !o.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException(
            "Produção: Cors:Origins deve usar HTTPS e a URL exata do frontend (sem placeholders).");
    }
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await ctx.HttpContext.Response.WriteAsync(
            "Muitas requisições. Tente novamente em instantes.", token);
    };

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("sync", httpContext =>
    {
        var key = httpContext.User.FindFirst("tenant_id")?.Value
                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                  ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

builder.Services.AddEduFlowInfrastructure(builder.Configuration, registerWorkers: false);

var app = builder.Build();

DapperConfiguration.EnsureDateOnlyHandlers();

if (string.Equals(
        Environment.GetEnvironmentVariable("EDUFLOW_INIT_SCHEMA"),
        "true",
        StringComparison.OrdinalIgnoreCase))
{
    if (app.Environment.IsProduction())
        throw new InvalidOperationException("EDUFLOW_INIT_SCHEMA não é permitido em Production.");

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StagingDbContext>();
    var created = await db.Database.EnsureCreatedAsync();
    Log.Information("EDUFLOW_INIT_SCHEMA: staging {Status}", created ? "criado" : "já existia");
    return;
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();

app.Use(async (ctx, next) =>
{
    var cid = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
              ?? Guid.NewGuid().ToString("N");
    ctx.Response.Headers["X-Correlation-Id"] = cid;
    using (LogContext.PushProperty("CorrelationId", cid))
    {
        await next();
    }
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.UseRequestTimeouts();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<StagingDbContext>();
        await db.Database.EnsureCreatedAsync();
        Log.Warning(
            "EnsureCreated apenas em Development. Produção: aplicar sql/01-*.sql e migrations conforme pipeline.");
    }
}
else
{
    Log.Information("Produção: schema do banco via CI/CD (scripts SQL ou EF migrations), sem EnsureCreated.");
}

app.MapControllers();
app.Run();
