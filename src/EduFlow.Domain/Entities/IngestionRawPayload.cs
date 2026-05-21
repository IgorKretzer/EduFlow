namespace EduFlow.Domain.Entities;

/// <summary>
/// Payload bruto do ERP para rastreabilidade e reprocessamento.
/// </summary>
public sealed class IngestionRawPayload
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public required string ErpProvider { get; init; }
    public required string EntityType { get; init; }
    public required string PayloadXml { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
    public string ProcessingStatus { get; set; } = "pending";
}
