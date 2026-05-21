namespace EduFlow.Domain.Entities;

/// <summary>
/// Modelo canônico de aluno — sem PII desnecessária (LGPD).
/// </summary>
public sealed record CanonicalStudent
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public required string ExternalId { get; init; }
    public required string EnrollmentCode { get; init; }
    public Guid UnitId { get; init; }
    public Guid? CourseId { get; init; }
    public string Status { get; init; } = "active";
    public DateTime? EnrolledAt { get; init; }
    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}
