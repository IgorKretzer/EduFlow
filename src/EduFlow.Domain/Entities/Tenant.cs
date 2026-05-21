namespace EduFlow.Domain.Entities;

/// <summary>
/// Instituição cliente (multi-tenant). Isola dados por escola/rede.
/// </summary>
public sealed class Tenant
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
