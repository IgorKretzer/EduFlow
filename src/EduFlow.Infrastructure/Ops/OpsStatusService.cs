using EduFlow.Analytics.Queries;
using EduFlow.Application.Interfaces;
using EduFlow.Application.Options;
using EduFlow.Infrastructure.Messaging;
using EduFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EduFlow.Infrastructure.Ops;

public sealed class OpsStatusService
{
    private static readonly string[] PipelineQueues =
        ["students", "financial", "contracts", "analytics"];

    private readonly StagingDbContext _db;
    private readonly IDwConnectionFactory _dw;
    private readonly RabbitMqSettings _rabbit;
    private readonly OpsSettings _ops;
    private readonly SyncSchedulerSettings _scheduler;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public OpsStatusService(
        StagingDbContext db,
        IDwConnectionFactory dw,
        IOptions<RabbitMqSettings> rabbit,
        IOptions<OpsSettings> ops,
        IOptions<SyncSchedulerSettings> scheduler,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _db = db;
        _dw = dw;
        _rabbit = rabbit.Value;
        _ops = ops.Value;
        _scheduler = scheduler.Value;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<OpsStatusResponse> GetStatusAsync(
        Guid? tenantId,
        CancellationToken ct = default)
    {
        var components = new List<OpsComponentStatus>
        {
            Component("api", "API EduFlow", "healthy", "Processo ativo (esta requisição confirma).")
        };

        components.Add(await ProbeSqlAsync("sql-staging", "SQL Staging", _configuration.GetConnectionString("Staging"), ct));
        components.Add(await ProbeSqlAsync("sql-dw", "SQL Data Warehouse", _configuration.GetConnectionString("Warehouse"), ct));

        var (rabbitComponent, queues, exchangeOk) = ProbeRabbit();
        components.Add(rabbitComponent);
        if (!exchangeOk)
        {
            components.Add(Component(
                "rabbit-exchange",
                "Exchange eduflow.events",
                "unhealthy",
                "Exchange ausente. Suba API ou Workers para criar a topologia."));
        }
        else
        {
            components.Add(Component(
                "rabbit-exchange",
                "Exchange eduflow.events",
                "healthy",
                "Exchange topic presente."));
        }

        components.Add(await ProbeWorkersAsync(ct));

        OpsPipelineInfo? pipeline = null;
        if (tenantId.HasValue)
        {
            var erp = await _db.ErpConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId.Value, ct);

            pipeline = new OpsPipelineInfo(
                _scheduler.Enabled,
                _scheduler.PollIntervalSeconds,
                erp?.LastSyncAtUtc,
                erp?.LastSyncStatus,
                erp?.LastSyncMessage);
        }

        var links = new List<OpsExternalLink>
        {
            new("RabbitMQ Management", _ops.RabbitManagementUrl),
            new("Swagger API", _ops.ApiSwaggerUrl),
            new("Front-end", _ops.FrontendUrl),
            new("Health API (live)", $"{_ops.ApiSwaggerUrl.Replace("/swagger", "")}/health/live"),
            new("Health API (ready)", $"{_ops.ApiSwaggerUrl.Replace("/swagger", "")}/health/ready"),
            new("Workers health", _ops.WorkersHealthUrl)
        };

        return new OpsStatusResponse(
            DateTime.UtcNow,
            components,
            queues,
            pipeline,
            links);
    }

    private async Task<OpsComponentStatus> ProbeSqlAsync(
        string id,
        string name,
        string? connectionString,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return Component(id, name, "unhealthy", "Connection string não configurada.");

        try
        {
            if (id == "sql-staging")
            {
                if (!await _db.Database.CanConnectAsync(ct))
                    return Component(id, name, "unhealthy", "Não conectou ao EduFlow_Staging.");
                return Component(id, name, "healthy", "EduFlow_Staging acessível.");
            }

            await using var conn = _dw.Create();
            await conn.OpenAsync(ct);
            return Component(id, name, "healthy", "EduFlow_DW acessível.");
        }
        catch (Exception ex)
        {
            return Component(id, name, "unhealthy", ex.Message);
        }
    }

    private (OpsComponentStatus Rabbit, IReadOnlyList<OpsQueueStatus> Queues, bool ExchangeOk) ProbeRabbit()
    {
        var queueResults = new List<OpsQueueStatus>();
        var exchangeOk = false;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbit.Host,
                Port = _rabbit.Port,
                UserName = _rabbit.Username,
                Password = _rabbit.Password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            using var conn = factory.CreateConnection();
            using var channel = conn.CreateModel();

            try
            {
                channel.ExchangeDeclarePassive(_rabbit.Exchange);
                exchangeOk = true;
            }
            catch
            {
                exchangeOk = false;
            }

            foreach (var queueName in PipelineQueues)
            {
                try
                {
                    var declare = channel.QueueDeclarePassive(queueName);
                    queueResults.Add(new OpsQueueStatus(
                        queueName,
                        true,
                        declare.MessageCount,
                        declare.ConsumerCount));
                }
                catch
                {
                    queueResults.Add(new OpsQueueStatus(queueName, false, 0, 0));
                }
            }

            var totalMessages = queueResults.Sum(q => (long)q.Messages);
            var totalConsumers = queueResults.Sum(q => (long)q.Consumers);
            var detail =
                $"Broker {_rabbit.Host}:{_rabbit.Port}. " +
                $"Filas: {queueResults.Count(q => q.Exists)}/{PipelineQueues.Length}. " +
                $"Mensagens: {totalMessages}. Consumidores: {totalConsumers}.";

            return (Component("rabbitmq", "RabbitMQ", "healthy", detail), queueResults, exchangeOk);
        }
        catch (Exception ex)
        {
            foreach (var queueName in PipelineQueues)
                queueResults.Add(new OpsQueueStatus(queueName, false, 0, 0));

            return (
                Component("rabbitmq", "RabbitMQ", "unhealthy", ex.Message),
                queueResults,
                false);
        }
    }

    private async Task<OpsComponentStatus> ProbeWorkersAsync(CancellationToken ct)
    {
        var url = _ops.WorkersHealthUrl;
        if (string.IsNullOrWhiteSpace(url))
            return Component("workers", "Workers EduFlow", "degraded", "Ops:WorkersHealthUrl não configurada.");

        try
        {
            var client = _httpClientFactory.CreateClient("ops-probe");
            using var response = await client.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
                return Component("workers", "Workers EduFlow", "healthy", $"Respondeu em {url}.");

            return Component(
                "workers",
                "Workers EduFlow",
                "unhealthy",
                $"HTTP {(int)response.StatusCode} em {url}. Rode: dotnet run --project src\\EduFlow.Workers");
        }
        catch (Exception ex)
        {
            return Component(
                "workers",
                "Workers EduFlow",
                "unhealthy",
                $"{ex.Message}. Inicie: dotnet run --project src\\EduFlow.Workers");
        }
    }

    private static OpsComponentStatus Component(string id, string name, string status, string detail) =>
        new(id, name, status, detail);
}
