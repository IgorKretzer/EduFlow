namespace EduFlow.Application.Interfaces;

public interface IEtlService
{
    Task UpsertStudentAsync(Guid tenantId, Domain.Entities.CanonicalStudent student, CancellationToken ct = default);
    Task UpsertFinancialAsync(Guid tenantId, Domain.Entities.CanonicalFinancial financial, CancellationToken ct = default);
    Task UpsertCategoryAsync(Guid tenantId, Domain.Entities.CanonicalCategory category, CancellationToken ct = default);
    Task UpsertContractAsync(Guid tenantId, Domain.Entities.CanonicalContract contract, CancellationToken ct = default);
    Task RefreshAnalyticsAsync(Guid tenantId, CancellationToken ct = default);
}
