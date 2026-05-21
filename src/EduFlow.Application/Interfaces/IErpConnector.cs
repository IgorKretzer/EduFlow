namespace EduFlow.Application.Interfaces;

/// <summary>
/// Contrato do Connector Engine — cada ERP implementa este adapter.
/// </summary>
public interface IErpConnector
{
    string ProviderKey { get; }
    Task<IReadOnlyList<RawErpRecord>> FetchStudentsAsync(ErpSyncContext context, CancellationToken ct = default);
    Task<IReadOnlyList<RawErpRecord>> FetchFinancialAsync(ErpSyncContext context, CancellationToken ct = default);
    Task<IReadOnlyList<RawErpRecord>> FetchContractsAsync(ErpSyncContext context, CancellationToken ct = default);
}

public sealed record ErpSyncContext(
    Guid TenantId,
    string EndpointUrl,
    string Username,
    string Password,
    int PageSize,
    DateOnly? FromDate,
    string? SearchParametersStudents = null,
    string? SearchParametersFinancial = null,
    string? SearchParametersPayables = null,
    string? SearchParametersContracts = null);

public sealed record RawErpRecord(
    string EntityType,
    string ExternalId,
    string RawXml,
    DateTime FetchedAt);
