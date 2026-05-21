namespace EduFlow.Application.Interfaces;

/// <summary>
/// Extensões financeiras opcionais do ERP (contas a pagar, categorias).
/// Connectors REST/OpenAPI podem não implementar; Sponte SOAP sim.
/// </summary>
public interface IErpExtendedConnector : IErpConnector
{
    Task<IReadOnlyList<RawErpRecord>> FetchPayablesAsync(ErpSyncContext context, CancellationToken ct = default);
    Task<IReadOnlyList<RawErpRecord>> FetchCategoriesAsync(ErpSyncContext context, CancellationToken ct = default);
}
