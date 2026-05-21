using RabbitMQ.Client;

namespace EduFlow.Infrastructure.Messaging;

internal static class RabbitMqTopology
{
    public static void EnsureExchange(IModel channel, string exchange) =>
        channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);

    public static void DeclareQueueWithDlq(IModel channel, string exchange, string queueName, string bindingKey)
    {
        EnsureExchange(channel, exchange);

        var dlqName = $"{queueName}.dlq";
        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = dlqName
        };

        channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        channel.QueueBind(queueName, exchange, bindingKey);
    }

    public static int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers?.TryGetValue("x-retry-count", out var val) == true)
        {
            return val switch
            {
                int i => i,
                byte b => b,
                long l => (int)l,
                _ => 0
            };
        }
        return 0;
    }
}
