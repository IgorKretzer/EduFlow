namespace EduFlow.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "eduflow";
    public string Password { get; set; } = "eduflow_secret";
    public string Exchange { get; set; } = "eduflow.events";
}
