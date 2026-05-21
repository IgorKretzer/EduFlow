using EduFlow.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.WithProperty("Application", "EduFlow.Workers")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Workers em execução."), tags: ["live"]);

    builder.Services.AddEduFlowInfrastructure(builder.Configuration, registerWorkers: true);

    var app = builder.Build();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });

    Log.Information("Workers HTTP health em {Url}", "http://localhost:5055/health/live");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Workers encerrados com erro");
}
finally
{
    Log.CloseAndFlush();
}
