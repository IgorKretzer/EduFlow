using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EduFlow.Api.Health;

/// <summary>
/// Verifica conectividade com RabbitMQ (amadurecimento operacional / observabilidade).
/// </summary>
public sealed class RabbitMqConnectionHealthCheck : IHealthCheck
{
    private readonly RabbitMqHealthOptions _opt;

    public RabbitMqConnectionHealthCheck(IOptions<RabbitMqHealthOptions> opt)
        => _opt = opt.Value;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _opt.Host,
                Port = _opt.Port,
                UserName = _opt.Username,
                Password = _opt.Password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            using var conn = factory.CreateConnection();
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ indisponível.", ex));
        }
    }
}

public sealed class RabbitMqHealthOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
