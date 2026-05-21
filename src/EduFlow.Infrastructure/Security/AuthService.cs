using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;
using EduFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EduFlow.Infrastructure.Security;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "EduFlow";
    public string Audience { get; set; } = "EduFlow.Clients";
    public string Secret { get; set; } = "CHANGE_ME_IN_PRODUCTION_MIN_32_CHARS!!";
    public int ExpirationMinutes { get; set; } = 480;
}

public sealed class AuthService : IAuthService
{
    private readonly StagingDbContext _db;
    private readonly JwtSettings _jwt;

    public AuthService(StagingDbContext db, IOptions<JwtSettings> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        var tenant = await _db.Tenants.AsNoTracking().FirstAsync(t => t.Id == user.TenantId, ct);
        return CreateToken(user, tenant);
    }

    public async Task<LoginResponse> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken ct = default)
    {
        ValidateAdminPassword(request.AdminPassword);

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _db.Tenants.AnyAsync(t => t.Slug == slug, ct))
            throw new ArgumentException("Já existe uma escola com este identificador (slug).");
        if (await _db.Users.AnyAsync(u => u.Email == request.AdminEmail, ct))
            throw new ArgumentException("E-mail administrativo já cadastrado.");

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = request.Name.Trim(),
            Slug = slug
        };

        var user = new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = request.AdminEmail,
            PasswordHash = HashPassword(request.AdminPassword)
        };

        _db.Tenants.Add(tenant);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreateToken(user, tenant);
    }

    private LoginResponse CreateToken(TenantUser user, Tenant tenant)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("tenant_id", tenant.Id.ToString()),
            new Claim("tenant_slug", tenant.Slug),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: expires,
            signingCredentials: creds);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires,
            tenant.Id,
            tenant.Slug);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    internal static void ValidateAdminPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new ArgumentException("Senha deve ter pelo menos 10 caracteres.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            throw new ArgumentException("Senha deve incluir maiúscula, minúscula e número.");
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
