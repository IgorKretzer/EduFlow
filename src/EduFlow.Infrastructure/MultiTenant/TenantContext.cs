using EduFlow.Application.Interfaces;

namespace EduFlow.Infrastructure.MultiTenant;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
