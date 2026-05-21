namespace EduFlow.Domain.Entities;

public sealed record CanonicalContract
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public required string ExternalId { get; init; }
    public required string EnrollmentCode { get; init; }
    public Guid UnitId { get; init; }
    public decimal TotalAmount { get; init; }
    public string ContractStatus { get; init; } = "active";
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}
