using System.Security.Claims;

namespace EduFlow.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredTenantId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("tenant_id")
            ?? throw new UnauthorizedAccessException("Sessão sem identificação da escola (tenant).");
        return Guid.Parse(claim);
    }
}
