namespace EduFlow.Application.Interfaces;

public interface ITenantErpConfigRepository
{
    Task<TenantErpConfig?> GetAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<TenantErpConfig>> GetAllEnabledForSyncAsync(CancellationToken ct);
    Task UpsertAsync(TenantErpConfig config, CancellationToken ct);
    Task UpdateSyncResultAsync(
        Guid tenantId,
        DateTime lastSyncAtUtc,
        string status,
        string? message,
        CancellationToken ct);
}

public sealed record TenantErpConfig(
    Guid TenantId,
    string ProviderKey,
    string EndpointUrl,
    string Username,
    string Password,
    int PageSize,
    bool SyncEnabled,
    int SyncIntervalMinutes,
    DateTime? LastSyncAtUtc,
    string? LastSyncStatus,
    string? LastSyncMessage,
    string? SearchParametersStudents = null,
    string? SearchParametersFinancial = null,
    string? SearchParametersContracts = null);
