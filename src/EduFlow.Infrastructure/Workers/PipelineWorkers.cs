using System.Text;
using System.Text.Json;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;
using EduFlow.Infrastructure.Messaging;
using EduFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EduFlow.Infrastructure.Workers;

public abstract class RabbitMqConsumerWorker : BackgroundService
{
    private const int MaxRetries = 3;
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private IConnection? _connection;
    private IModel? _channel;

    protected RabbitMqConsumerWorker(
        IOptions<RabbitMqSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected abstract string QueueName { get; }
    protected abstract string WorkerName { get; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        RabbitMqTopology.DeclareQueueWithDlq(_channel, _settings.Exchange, QueueName, GetBindingKey());
        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId ?? "unknown";
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                using var scope = _scopeFactory.CreateScope();
                await ProcessMessageAsync(scope.ServiceProvider, json, correlationId, stoppingToken);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var retry = RabbitMqTopology.GetRetryCount(ea.BasicProperties) + 1;
                _logger.LogError(ex,
                    "{Worker} falhou CorrelationId={CorrelationId} Retry={Retry}",
                    WorkerName, correlationId, retry);

                if (retry >= MaxRetries)
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
                else
                {
                    var props = _channel.CreateBasicProperties();
                    props.Headers = new Dictionary<string, object> { ["x-retry-count"] = retry };
                    props.CorrelationId = correlationId;
                    _channel.BasicPublish("", QueueName, props, ea.Body.ToArray());
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            }
        };

        _channel.BasicConsume(QueueName, autoAck: false, consumer);
        _logger.LogInformation("{Worker} escutando {Queue}", WorkerName, QueueName);
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    protected virtual string GetBindingKey() => QueueName switch
    {
        "students" => "student.*",
        "financial" => "financial.*",
        "contracts" => "contract.*",
        "analytics" => "analytics.*",
        _ => "#"
    };

    protected abstract Task ProcessMessageAsync(
        IServiceProvider services, string json, string correlationId, CancellationToken ct);

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

public sealed class IngestionWorker : RabbitMqConsumerWorker
{
    public IngestionWorker(IOptions<RabbitMqSettings> s, IServiceScopeFactory f, ILogger<IngestionWorker> l)
        : base(s, f, l) { }

    protected override string QueueName => "students";
    protected override string WorkerName => "IngestionWorker";

    protected override async Task ProcessMessageAsync(
        IServiceProvider services, string json, string correlationId, CancellationToken ct)
    {
        var etl = services.GetRequiredService<IEtlService>();
        var evt = JsonSerializer.Deserialize<IntegrationEvent<CanonicalStudent>>(json);
        if (evt?.Payload is null) return;

        await etl.UpsertStudentAsync(evt.TenantId, evt.Payload, ct);

        var db = services.GetRequiredService<StagingDbContext>();
        if (!string.IsNullOrEmpty(correlationId))
        {
            var raws = await db.RawPayloads
                .Where(x => x.CorrelationId == correlationId && x.ProcessingStatus == "pending")
                .ToListAsync(ct);
            foreach (var raw in raws.Where(r => r.EntityType == "student"))
                raw.ProcessingStatus = "ingested";
            await db.SaveChangesAsync(ct);
        }
    }
}

public sealed class SnapshotWorker : RabbitMqConsumerWorker
{
    public SnapshotWorker(IOptions<RabbitMqSettings> s, IServiceScopeFactory f, ILogger<SnapshotWorker> l)
        : base(s, f, l) { }

    protected override string QueueName => "financial";
    protected override string WorkerName => "SnapshotWorker";

    protected override async Task ProcessMessageAsync(
        IServiceProvider services, string json, string correlationId, CancellationToken ct)
    {
        var etl = services.GetRequiredService<IEtlService>();
        var evt = JsonSerializer.Deserialize<IntegrationEvent<CanonicalFinancial>>(json);
        if (evt?.Payload is null) return;
        await etl.UpsertFinancialAsync(evt.TenantId, evt.Payload, ct);
    }
}

public sealed class TransformWorker : RabbitMqConsumerWorker
{
    public TransformWorker(IOptions<RabbitMqSettings> s, IServiceScopeFactory f, ILogger<TransformWorker> l)
        : base(s, f, l) { }

    protected override string QueueName => "contracts";
    protected override string WorkerName => "TransformWorker";

    protected override async Task ProcessMessageAsync(
        IServiceProvider services, string json, string correlationId, CancellationToken ct)
    {
        var etl = services.GetRequiredService<IEtlService>();
        var publisher = services.GetRequiredService<IEventPublisher>();
        var evt = JsonSerializer.Deserialize<IntegrationEvent<CanonicalContract>>(json);
        if (evt?.Payload is null) return;

        await etl.UpsertContractAsync(evt.TenantId, evt.Payload, ct);

        await publisher.PublishAsync(
            EventRoutingKeys.AnalyticsRefresh,
            new IntegrationEvent<object>(evt.TenantId, EventRoutingKeys.AnalyticsRefresh, new { }, DateTime.UtcNow, correlationId),
            ct);
    }
}

public sealed class AnalyticsWorker : RabbitMqConsumerWorker
{
    public AnalyticsWorker(IOptions<RabbitMqSettings> s, IServiceScopeFactory f, ILogger<AnalyticsWorker> l)
        : base(s, f, l) { }

    protected override string QueueName => "analytics";
    protected override string WorkerName => "AnalyticsWorker";

    protected override async Task ProcessMessageAsync(
        IServiceProvider services, string json, string correlationId, CancellationToken ct)
    {
        var etl = services.GetRequiredService<IEtlService>();
        var envelope = JsonSerializer.Deserialize<JsonElement>(json);
        var tenantId = envelope.GetProperty("TenantId").GetGuid();
        await etl.RefreshAnalyticsAsync(tenantId, ct);
    }
}
