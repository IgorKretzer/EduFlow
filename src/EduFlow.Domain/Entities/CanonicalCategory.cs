namespace EduFlow.Domain.Entities;

public sealed record CanonicalCategory
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public required string ExternalId { get; init; }
    public required string Name { get; init; }
    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}
