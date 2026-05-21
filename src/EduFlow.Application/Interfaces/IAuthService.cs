using EduFlow.Application.DTOs;

namespace EduFlow.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken ct = default);
}
