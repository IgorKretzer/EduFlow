namespace EduFlow.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class;
}

public static class EventRoutingKeys
{
    public const string StudentUpdated = "student.updated";
    public const string FinancialChanged = "financial.changed";
    public const string ContractCreated = "contract.created";
    public const string AnalyticsRefresh = "analytics.refresh";
}

public sealed record IntegrationEvent<T>(
    Guid TenantId,
    string EventType,
    T Payload,
    DateTime OccurredAt,
    string? CorrelationId = null)
    where T : class;
