using System.Text;
using System.Text.Json;
using EduFlow.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EduFlow.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqEventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;

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
        RabbitMqTopology.EnsureExchange(_channel, _settings.Exchange);

        RabbitMqTopology.DeclareQueueWithDlq(_channel, _settings.Exchange, "students", "student.*");
        RabbitMqTopology.DeclareQueueWithDlq(_channel, _settings.Exchange, "financial", "financial.*");
        RabbitMqTopology.DeclareQueueWithDlq(_channel, _settings.Exchange, "contracts", "contract.*");
        RabbitMqTopology.DeclareQueueWithDlq(_channel, _settings.Exchange, "analytics", "analytics.*");
    }

    public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class
    {
        var correlationId = ExtractCorrelationId(message);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(_settings.Exchange, routingKey, props, body);

        _logger.LogInformation(
            "Evento publicado RoutingKey={RoutingKey} CorrelationId={CorrelationId}",
            routingKey, correlationId);

        return Task.CompletedTask;
    }

    private static string ExtractCorrelationId<T>(T message) where T : class
    {
        var prop = message.GetType().GetProperty("CorrelationId");
        return prop?.GetValue(message) as string ?? Guid.NewGuid().ToString("N");
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
