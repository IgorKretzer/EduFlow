using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;

namespace EduFlow.Infrastructure.Normalization;

/// <summary>
/// Encaminha a normalização para o adapter do fornecedor (Sponte, OpenAPI, …).
/// </summary>
public sealed class CompositeCanonicalNormalizer : ICanonicalNormalizer
{
    private readonly SponteCanonicalNormalizer _sponte;
    private readonly OpenApiCanonicalNormalizer _openApi;

    public CompositeCanonicalNormalizer(
        SponteCanonicalNormalizer sponte,
        OpenApiCanonicalNormalizer openApi)
    {
        _sponte = sponte;
        _openApi = openApi;
    }

    public CanonicalStudent? NormalizeStudent(string erpProvider, string rawXml) =>
        erpProvider.Equals("openapi", StringComparison.OrdinalIgnoreCase)
            ? _openApi.NormalizeStudent(erpProvider, rawXml)
            : _sponte.NormalizeStudent(erpProvider, rawXml);

    public CanonicalFinancial? NormalizeFinancial(string erpProvider, string rawXml) =>
        erpProvider.Equals("openapi", StringComparison.OrdinalIgnoreCase)
            ? _openApi.NormalizeFinancial(erpProvider, rawXml)
            : _sponte.NormalizeFinancial(erpProvider, rawXml);

    public CanonicalFinancial? NormalizePayable(string erpProvider, string rawXml) =>
        erpProvider.Equals("openapi", StringComparison.OrdinalIgnoreCase)
            ? _openApi.NormalizePayable(erpProvider, rawXml)
            : _sponte.NormalizePayable(erpProvider, rawXml);

    public CanonicalCategory? NormalizeCategory(string erpProvider, string rawXml) =>
        erpProvider.Equals("openapi", StringComparison.OrdinalIgnoreCase)
            ? _openApi.NormalizeCategory(erpProvider, rawXml)
            : _sponte.NormalizeCategory(erpProvider, rawXml);

    public CanonicalContract? NormalizeContract(string erpProvider, string rawXml) =>
        erpProvider.Equals("openapi", StringComparison.OrdinalIgnoreCase)
            ? _openApi.NormalizeContract(erpProvider, rawXml)
            : _sponte.NormalizeContract(erpProvider, rawXml);
}
