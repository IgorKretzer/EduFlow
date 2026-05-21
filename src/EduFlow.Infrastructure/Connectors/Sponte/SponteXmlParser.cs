using System.Text;
using System.Xml.Linq;
using EduFlow.Application.Interfaces;

namespace EduFlow.Infrastructure.Connectors.Sponte;

/// <summary>
/// Extrai registros do XML Sponte (ArrayOfWs* / SOAP Body).
/// Responsabilidade: parsing do ERP — não conhece o modelo canônico.
/// </summary>
internal static class SponteXmlParser
{
    public const string SponteNamespace = "http://api.sponteeducacional.net.br/";
    private const string FinancialWrapperRoot = "EduFlowSponteFinancialSource";
    private const string PayableWrapperRoot = "EduFlowSpontePayableSource";
    private const string CategoryWrapperRoot = "EduFlowSponteCategorySource";

    public static IReadOnlyList<RawErpRecord> ParseResponse(string xml, string entityType)
    {
        var doc = XDocument.Parse(xml);
        var payload = UnwrapSoapBody(doc) ?? doc.Root;
        if (payload is null) return [];

        return entityType switch
        {
            "student" => ParseStudents(payload),
            "financial" => ParseFinancial(payload),
            "payable" => ParsePayables(payload),
            "category" => ParseCategories(payload),
            "contract" => ParseMatriculas(payload),
            _ => []
        };
    }

    private static XElement? UnwrapSoapBody(XDocument doc)
    {
        var body = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("Body", StringComparison.OrdinalIgnoreCase));
        return body?.Elements().FirstOrDefault() ?? doc.Root;
    }

    public static string? ExtractOperationMessage(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var msg = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName.Equals("RetornoOperacao", StringComparison.OrdinalIgnoreCase))
                ?.Value;
            return string.IsNullOrWhiteSpace(msg) ? null : msg.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static List<RawErpRecord> ParseStudents(XElement root)
    {
        return root.Descendants()
            .Where(e => e.Name.LocalName.Equals("wsAluno", StringComparison.OrdinalIgnoreCase))
            .Where(IsValidSponteRecord)
            .GroupBy(aluno => Child(aluno, "AlunoID"))
            .Select(g => g.First())
            .Select(aluno =>
            {
                var id = Child(aluno, "AlunoID")!;
                return new RawErpRecord("student", id, aluno.ToString(SaveOptions.DisableFormatting), DateTime.UtcNow);
            })
            .ToList();
    }

    private static bool IsValidSponteRecord(XElement row)
    {
        var op = Child(row, "RetornoOperacao");
        if (!string.IsNullOrEmpty(op) &&
            !op.Contains("Sucesso", StringComparison.OrdinalIgnoreCase))
            return false;

        var id = Child(row, "AlunoID") ?? Child(row, "ContratoID");
        return int.TryParse(id, out var numeric) && numeric > 0;
    }

    private static List<RawErpRecord> ParseMatriculas(XElement root)
    {
        return root.Descendants()
            .Where(e => e.Name.LocalName.Equals("wsMatricula", StringComparison.OrdinalIgnoreCase))
            .Where(IsValidSponteRecord)
            .Select(mat =>
            {
                var id = Child(mat, "ContratoID") ?? Child(mat, "NumeroContrato") ?? Guid.NewGuid().ToString("N");
                return new RawErpRecord("contract", id, mat.ToString(SaveOptions.DisableFormatting), DateTime.UtcNow);
            })
            .ToList();
    }

    /// <summary>
    /// GetFinanceiro: 1 registro RAW por parcela (wsParcela), com contexto do contrato/unidade.
    /// </summary>
    private static List<RawErpRecord> ParseFinancial(XElement root)
    {
        var records = new List<RawErpRecord>();

        foreach (var financeiro in root.Descendants()
                     .Where(e => e.Name.LocalName.Equals("wsFinanceiro", StringComparison.OrdinalIgnoreCase)))
        {
            var unitCode = Child(financeiro, "CodigoUnidade");
            var contractNumber = Child(financeiro, "NumeroContrato");
            var contaReceberId = Child(financeiro, "ContaReceberID");
            foreach (var parcela in financeiro.Descendants()
                         .Where(e => e.Name.LocalName.Equals("wsParcela", StringComparison.OrdinalIgnoreCase)))
            {
                var enrollment = ResolveEnrollmentCode(financeiro, parcela);
                var parcelaId = Child(parcela, "ContaReceberID") ?? contaReceberId ?? "0";
                var numeroParcela = Child(parcela, "NumeroParcela") ?? "0";
                var externalId = $"{parcelaId}-{numeroParcela}";

                var categoria = FirstNonEmpty(
                    Child(parcela, "Categoria"),
                    Child(financeiro, "Categoria"),
                    Child(financeiro, "TipoPlano"),
                    Child(financeiro, "Curso"));
                var categoriaId = FirstNonEmpty(
                    Child(parcela, "CategoriaID"),
                    Child(financeiro, "CategoriaID"));
                var formaCobranca = FirstNonEmpty(
                    Child(parcela, "FormaCobranca"),
                    Child(parcela, "TipoRecebimento"));

                var wrapper = new XElement(FinancialWrapperRoot,
                    new XElement("CodigoUnidade", unitCode ?? ""),
                    new XElement("NumeroContrato", contractNumber ?? ""),
                    new XElement("NumeroMatricula", enrollment ?? ""),
                    new XElement("Categoria", categoria ?? ""),
                    new XElement("CategoriaID", categoriaId ?? ""),
                    new XElement("TipoPlano", Child(financeiro, "TipoPlano") ?? ""),
                    new XElement("Curso", Child(financeiro, "Curso") ?? ""),
                    new XElement("FormaCobranca", formaCobranca ?? ""),
                    new XElement(parcela));

                records.Add(new RawErpRecord(
                    "financial",
                    externalId,
                    wrapper.ToString(SaveOptions.DisableFormatting),
                    DateTime.UtcNow));
            }
        }

        return records;
    }

    /// <summary>GetParcelasPagar — uma linha por wsParcelaPagar.</summary>
    private static List<RawErpRecord> ParsePayables(XElement root)
    {
        return root.Descendants()
            .Where(e => e.Name.LocalName.Equals("wsParcelaPagar", StringComparison.OrdinalIgnoreCase))
            .Select(parcela =>
            {
                var contaId = Child(parcela, "ContaPagarID") ?? "0";
                var numero = Child(parcela, "NumeroParcela") ?? "0";
                var externalId = $"pagar-{contaId}-{numero}";
                var wrapper = new XElement(PayableWrapperRoot, new XElement(parcela));
                return new RawErpRecord(
                    "payable",
                    externalId,
                    wrapper.ToString(SaveOptions.DisableFormatting),
                    DateTime.UtcNow);
            })
            .ToList();
    }

    /// <summary>GetCategorias — itens CategoriaID/Nome.</summary>
    private static List<RawErpRecord> ParseCategories(XElement root)
    {
        var records = new List<RawErpRecord>();

        foreach (var item in root.Descendants()
                     .Where(e => e.Name.LocalName.Equals("Categorias", StringComparison.OrdinalIgnoreCase))
                     .Where(e => Child(e, "CategoriaID") is not null))
        {
            var id = Child(item, "CategoriaID")!;
            var nome = Child(item, "Nome") ?? id;
            var wrapper = new XElement(CategoryWrapperRoot,
                new XElement("CategoriaID", id),
                new XElement("Nome", nome));
            records.Add(new RawErpRecord("category", id, wrapper.ToString(SaveOptions.DisableFormatting), DateTime.UtcNow));
        }

        return records
            .GroupBy(r => r.ExternalId)
            .Select(g => g.First())
            .ToList();
    }

    private static string? ResolveEnrollmentCode(XElement wsFinanceiro, XElement wsParcela)
    {
        var parcelaAlunoId = Child(wsParcela, "AlunoID");
        var alunos = wsFinanceiro.Descendants()
            .Where(e => e.Name.LocalName.Equals("wsInfoAluno", StringComparison.OrdinalIgnoreCase));

        var match = alunos.FirstOrDefault(a => Child(a, "AlunoID") == parcelaAlunoId) ?? alunos.FirstOrDefault();
        return Child(match, "NumeroMatricula");
    }

    internal static string? Child(XElement? parent, string localName)
    {
        if (parent is null) return null;
        return parent.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }
        return null;
    }
}
