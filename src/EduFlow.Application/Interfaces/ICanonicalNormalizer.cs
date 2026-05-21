using EduFlow.Domain.Entities;

namespace EduFlow.Application.Interfaces;

/// <summary>
/// Normalization Layer — traduz XML/JSON de um ERP para o modelo canônico interno.
/// Cada fornecedor (Sponte, outro ERP) possui sua própria implementação.
/// O Domain e o DW nunca dependem do formato Sponte.
/// </summary>
public interface ICanonicalNormalizer
{
    CanonicalStudent? NormalizeStudent(string erpProvider, string rawXml);
    CanonicalFinancial? NormalizeFinancial(string erpProvider, string rawXml);
    CanonicalFinancial? NormalizePayable(string erpProvider, string rawXml);
    CanonicalCategory? NormalizeCategory(string erpProvider, string rawXml);
    CanonicalContract? NormalizeContract(string erpProvider, string rawXml);
}
