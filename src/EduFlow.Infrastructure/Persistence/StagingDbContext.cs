using EduFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduFlow.Infrastructure.Persistence;

public sealed class StagingDbContext : DbContext
{
    public StagingDbContext(DbContextOptions<StagingDbContext> options) : base(options) { }

    public DbSet<IngestionRawPayload> RawPayloads => Set<IngestionRawPayload>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<CanonicalStudent> Students => Set<CanonicalStudent>();
    public DbSet<CanonicalFinancial> Financials => Set<CanonicalFinancial>();
    public DbSet<CanonicalCategory> Categories => Set<CanonicalCategory>();
    public DbSet<CanonicalContract> Contracts => Set<CanonicalContract>();
    public DbSet<TenantUser> Users => Set<TenantUser>();
    public DbSet<TenantErpConfigEntity> ErpConfigs => Set<TenantErpConfigEntity>();
    public DbSet<StudentSnapshot> StudentSnapshots => Set<StudentSnapshot>();
    public DbSet<FinancialSnapshot> FinancialSnapshots => Set<FinancialSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<IngestionRawPayload>(e =>
        {
            e.ToTable("RawPayloads");
            e.HasKey(x => x.Id);
            e.Property(x => x.PayloadXml).HasColumnType("nvarchar(max)");
            e.HasIndex(x => new { x.TenantId, x.ReceivedAt });
        });

        modelBuilder.Entity<CanonicalStudent>(e =>
        {
            e.ToTable("CanonicalStudents");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.ExternalId }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.EnrollmentCode });
        });

        modelBuilder.Entity<CanonicalFinancial>(e =>
        {
            e.ToTable("CanonicalFinancials");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitCode).HasMaxLength(64);
            e.Property(x => x.FlowDirection).HasMaxLength(16).HasDefaultValue("receivable");
            e.Property(x => x.CategoryId).HasMaxLength(64);
            e.Property(x => x.CategoryName).HasMaxLength(256);
            e.Property(x => x.PaymentMethodLabel).HasMaxLength(128);
            e.Property(x => x.CounterpartyName).HasMaxLength(256);
            e.HasIndex(x => new { x.TenantId, x.ExternalId }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.EnrollmentCode, x.DueDate });
            e.HasIndex(x => new { x.TenantId, x.FlowDirection, x.CashFlowDate });
        });

        modelBuilder.Entity<CanonicalCategory>(e =>
        {
            e.ToTable("CanonicalCategories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256);
            e.HasIndex(x => new { x.TenantId, x.ExternalId }).IsUnique();
        });

        modelBuilder.Entity<CanonicalContract>(e =>
        {
            e.ToTable("CanonicalContracts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.ExternalId }).IsUnique();
        });

        modelBuilder.Entity<StudentSnapshot>(e =>
        {
            e.ToTable("StudentSnapshots");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.ExternalId, x.CapturedAt });
        });

        modelBuilder.Entity<FinancialSnapshot>(e =>
        {
            e.ToTable("FinancialSnapshots");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.ExternalId, x.CapturedAt });
        });

        modelBuilder.Entity<TenantUser>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<TenantErpConfigEntity>(e =>
        {
            e.ToTable("ErpConfigs");
            e.HasKey(x => x.TenantId);
        });
    }
}

public sealed class TenantUser
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string Role { get; set; } = "admin";
}

public sealed class StudentSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string ExternalId { get; set; }
    public required string EnrollmentCode { get; set; }
    public required string Status { get; set; }
    public DateTime CapturedAt { get; set; }
}

public sealed class FinancialSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string ExternalId { get; set; }
    public required string EnrollmentCode { get; set; }
    public decimal DebtAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public required string PaymentStatus { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime CapturedAt { get; set; }
}

public sealed class TenantErpConfigEntity
{
    public Guid TenantId { get; set; }
    public required string ProviderKey { get; set; }
    public required string EndpointUrl { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public int PageSize { get; set; } = 100;
    public bool SyncEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 15;
    public DateTime? LastSyncAtUtc { get; set; }
    public string? LastSyncStatus { get; set; }
    public string? LastSyncMessage { get; set; }
    public string? SearchParametersStudents { get; set; }
    public string? SearchParametersFinancial { get; set; }
    public string? SearchParametersContracts { get; set; }
}
