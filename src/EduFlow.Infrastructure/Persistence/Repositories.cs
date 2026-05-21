using EduFlow.Application.Interfaces;
using EduFlow.Application.Services;
using EduFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduFlow.Infrastructure.Persistence;

public sealed class IngestionStore : IIngestionStore
{
    private readonly StagingDbContext _db;

    public IngestionStore(StagingDbContext db) => _db = db;

    public async Task SaveRawAsync(IngestionRawPayload payload, CancellationToken ct)
    {
        _db.RawPayloads.Add(payload);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class TenantErpConfigRepository : ITenantErpConfigRepository
{
    private readonly StagingDbContext _db;
    private readonly ISecretProtector _secrets;

    public TenantErpConfigRepository(StagingDbContext db, ISecretProtector secrets)
    {
        _db = db;
        _secrets = secrets;
    }

    public async Task<TenantErpConfig?> GetAsync(Guid tenantId, CancellationToken ct)
    {
        var entity = await _db.ErpConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<TenantErpConfig>> GetAllEnabledForSyncAsync(CancellationToken ct)
    {
        var list = await _db.ErpConfigs.AsNoTracking()
            .Where(x => x.SyncEnabled)
            .ToListAsync(ct);

        return list.Select(Map).ToList();
    }

    public async Task UpsertAsync(TenantErpConfig config, CancellationToken ct)
    {
        var entity = await _db.ErpConfigs.FirstOrDefaultAsync(x => x.TenantId == config.TenantId, ct);
        if (entity is null)
        {
            entity = new TenantErpConfigEntity
            {
                TenantId = config.TenantId,
                ProviderKey = config.ProviderKey,
                EndpointUrl = config.EndpointUrl,
                Username = config.Username,
                Password = _secrets.Protect(config.Password)
            };
            _db.ErpConfigs.Add(entity);
        }

        entity.ProviderKey = config.ProviderKey;
        entity.EndpointUrl = config.EndpointUrl;
        entity.Username = config.Username;
        if (!string.IsNullOrEmpty(config.Password))
            entity.Password = _secrets.Protect(config.Password);
        entity.PageSize = config.PageSize;
        entity.SyncEnabled = config.SyncEnabled;
        entity.SyncIntervalMinutes = config.SyncIntervalMinutes;
        entity.LastSyncAtUtc = config.LastSyncAtUtc;
        entity.LastSyncStatus = config.LastSyncStatus;
        entity.LastSyncMessage = config.LastSyncMessage;
        entity.SearchParametersStudents = config.SearchParametersStudents;
        entity.SearchParametersFinancial = config.SearchParametersFinancial;
        entity.SearchParametersContracts = config.SearchParametersContracts;

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateSyncResultAsync(
        Guid tenantId,
        DateTime lastSyncAtUtc,
        string status,
        string? message,
        CancellationToken ct)
    {
        var entity = await _db.ErpConfigs.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (entity is null) return;

        entity.LastSyncAtUtc = lastSyncAtUtc;
        entity.LastSyncStatus = status;
        entity.LastSyncMessage = message;
        await _db.SaveChangesAsync(ct);
    }

    private TenantErpConfig Map(TenantErpConfigEntity e) =>
        new(
            e.TenantId,
            e.ProviderKey,
            e.EndpointUrl,
            e.Username,
            _secrets.Unprotect(e.Password),
            e.PageSize,
            e.SyncEnabled,
            e.SyncIntervalMinutes,
            e.LastSyncAtUtc,
            e.LastSyncStatus,
            e.LastSyncMessage,
            e.SearchParametersStudents,
            e.SearchParametersFinancial,
            e.SearchParametersContracts);
}
