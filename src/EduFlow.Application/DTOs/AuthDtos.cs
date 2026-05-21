namespace EduFlow.Application.DTOs;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    Guid TenantId,
    string TenantSlug);

public sealed record RegisterTenantRequest(
    string Name,
    string Slug,
    string AdminEmail,
    string AdminPassword);
