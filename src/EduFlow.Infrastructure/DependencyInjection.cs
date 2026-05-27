using EduFlow.Analytics.Queries;
using EduFlow.Application.Interfaces;
using EduFlow.Application.Options;
using EduFlow.Application.Services;
using EduFlow.Infrastructure.Connectors;
using EduFlow.Infrastructure.Messaging;
using EduFlow.Infrastructure.Normalization;
using EduFlow.Infrastructure.Persistence;
using EduFlow.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using EduFlow.Infrastructure.Services;
using EduFlow.Infrastructure.Etl;
using EduFlow.Infrastructure.MultiTenant;
using EduFlow.Infrastructure.Ops;
using EduFlow.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace EduFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEduFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool registerWorkers = false)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        services.Configure<SyncSchedulerSettings>(
            configuration.GetSection(SyncSchedulerSettings.SectionName));
        services.Configure<OpsSettings>(configuration.GetSection(OpsSettings.SectionName));
        services.Configure<ErpConnectorSettings>(
            configuration.GetSection(ErpConnectorSettings.SectionName));
        services.Configure<WorkerRuntimeSettings>(
            configuration.GetSection(WorkerRuntimeSettings.SectionName));

        services.AddHttpClient("ops-probe", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(3);
        });
        services.AddScoped<OpsStatusService>();

        services.AddDbContext<StagingDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Staging")));

        services.AddSingleton<IDwConnectionFactory>(_ =>
            new DwConnectionFactory(configuration.GetConnectionString("Warehouse")!));

        var dataProtection = services.AddDataProtection();
        var dataProtectionKeysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();
        services.AddScoped<IErpConfigService, ErpConfigService>();
        services.AddScoped<IAnalyticsReadRepository, AnalyticsReadRepository>();
        services.AddScoped<SponteCanonicalNormalizer>();
        services.AddScoped<OpenApiCanonicalNormalizer>();
        services.AddScoped<ICanonicalNormalizer, CompositeCanonicalNormalizer>();
        services.AddScoped<IIngestionStore, IngestionStore>();
        services.AddScoped<ITenantErpConfigRepository, TenantErpConfigRepository>();
        services.AddScoped<IEtlService, StagingToDwEtlService>();
        services.AddScoped<IEnrollmentAnalyticsService, EnrollmentAnalyticsService>();
        services.AddScoped<IFinanceAnalyticsService, FinanceAnalyticsService>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddHttpClient<SponteSoapConnector>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        });
        services.AddHttpClient<OpenApiRestConnector>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        });
        services.AddScoped<IErpConnector, SponteSoapConnector>();
        services.AddScoped<IErpConnector, OpenApiRestConnector>();

        if (registerWorkers)
        {
            var workers = configuration
                .GetSection(WorkerRuntimeSettings.SectionName)
                .Get<WorkerRuntimeSettings>() ?? new WorkerRuntimeSettings();

            if (workers.EnableStudents)
                services.AddHostedService<IngestionWorker>();
            if (workers.EnableFinancial)
                services.AddHostedService<SnapshotWorker>();
            if (workers.EnableContracts)
                services.AddHostedService<TransformWorker>();
            if (workers.EnableAnalytics)
                services.AddHostedService<AnalyticsWorker>();
            if (workers.EnableScheduler)
                services.AddHostedService<ScheduledSyncWorker>();
        }

        return services;
    }
}
