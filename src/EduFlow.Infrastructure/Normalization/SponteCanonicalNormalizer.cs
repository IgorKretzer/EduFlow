using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;
using EduFlow.Infrastructure.Connectors.Sponte;

namespace EduFlow.Infrastructure.Normalization;

/// <summary>
/// Adapter Sponte → modelo canônico EduFlow.
/// Outro ERP = outra classe (ex.: TOTVSCanonicalNormalizer), mesmas entidades de Domain.
/// </summary>
public sealed class SponteCanonicalNormalizer : ICanonicalNormalizer
{
    public CanonicalStudent? NormalizeStudent(string erpProvider, string rawXml)
    {
        if (!IsSponte(erpProvider)) return null;

        var root = LoadRoot(rawXml);
        if (root is null) return null;

        // wsAluno (GetAlunos) — LGPD: não mapear Nome, CPF, Email, Endereco, etc.
        var alunoId = SponteXmlParser.Child(root, "AlunoID");
        var matricula = SponteXmlParser.Child(root, "NumeroMatricula");
        if (string.IsNullOrWhiteSpace(alunoId) || string.IsNullOrWhiteSpace(matricula))
            return null;

        return new CanonicalStudent
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = alunoId,
            EnrollmentCode = matricula,
            UnitId = Guid.Empty,
            CourseId = ParseOptionalGuid(SponteXmlParser.Child(root, "CursoInteresse")),
            Status = MapStudentStatus(SponteXmlParser.Child(root, "Situacao"), SponteXmlParser.Child(root, "Inadimplente")),
            EnrolledAt = ParseDateTime(SponteXmlParser.Child(root, "DataCadastro"))
        };
    }

    public CanonicalFinancial? NormalizeFinancial(string erpProvider, string rawXml)
    {
        if (!IsSponte(erpProvider)) return null;

        var root = LoadRoot(rawXml);
        if (root is null) return null;

        // Pacote montado pelo SponteXmlParser (1 parcela + contexto)
        var parcela = root.Name.LocalName.Equals("wsParcela", StringComparison.OrdinalIgnoreCase)
            ? root
            : root.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("wsParcela", StringComparison.OrdinalIgnoreCase));

        if (parcela is null) return null;

        var unitCode = SponteXmlParser.Child(root, "CodigoUnidade")?.Trim() ?? "";
        var matricula = SponteXmlParser.Child(root, "NumeroMatricula")
                        ?? $"ALU-{SponteXmlParser.Child(parcela, "AlunoID")}";

        var valorParcela = ParseDecimal(SponteXmlParser.Child(parcela, "ValorParcela")) ?? 0m;
        var valorPago = ParseDecimal(SponteXmlParser.Child(parcela, "ValorPago")) ?? 0m;
        var situacao = SponteXmlParser.Child(parcela, "SituacaoParcela");
        var debt = CalculateDebt(valorParcela, valorPago, situacao);

        var parcelaId = SponteXmlParser.Child(parcela, "ContaReceberID") ?? "0";
        var numeroParcela = SponteXmlParser.Child(parcela, "NumeroParcela") ?? "0";
        var dueDate = ParseDateOnly(SponteXmlParser.Child(parcela, "Vencimento"))
                      ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var (categoryId, categoryName) = ResolveCategoryFields(root, parcela);
        var forma = SponteXmlParser.Child(root, "FormaCobranca")
                    ?? SponteXmlParser.Child(parcela, "FormaCobranca")
                    ?? SponteXmlParser.Child(parcela, "TipoRecebimento");

        return new CanonicalFinancial
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = $"{parcelaId}-{numeroParcela}",
            EnrollmentCode = matricula,
            UnitId = string.IsNullOrWhiteSpace(unitCode)
                ? Guid.Empty
                : ToDeterministicGuid($"sponte-unit:{unitCode}"),
            UnitCode = unitCode,
            DebtAmount = debt,
            PaidAmount = valorPago,
            PaymentStatus = MapParcelStatus(situacao, debt),
            DueDate = dueDate,
            FlowDirection = FinancialFlowDirection.Receivable,
            CategoryId = categoryId,
            CategoryName = categoryName,
            PaymentMethodLabel = forma ?? "",
            CashFlowDate = ResolveCashFlowDate(
                ParseDateOnly(SponteXmlParser.Child(parcela, "DataPagamento")),
                dueDate)
        };
    }

    public CanonicalFinancial? NormalizePayable(string erpProvider, string rawXml)
    {
        if (!IsSponte(erpProvider)) return null;

        var root = LoadRoot(rawXml);
        if (root is null) return null;

        var parcela = root.Name.LocalName.Equals("wsParcelaPagar", StringComparison.OrdinalIgnoreCase)
            ? root
            : root.Descendants().FirstOrDefault(e =>
                e.Name.LocalName.Equals("wsParcelaPagar", StringComparison.OrdinalIgnoreCase));

        if (parcela is null) return null;

        var valorParcela = ParseDecimal(SponteXmlParser.Child(parcela, "ValorParcela")) ?? 0m;
        var valorPago = ParseDecimal(SponteXmlParser.Child(parcela, "ValorPago")) ?? 0m;
        var situacao = SponteXmlParser.Child(parcela, "SituacaoParcela");
        var debt = CalculateDebt(valorParcela, valorPago, situacao);

        var contaId = SponteXmlParser.Child(parcela, "ContaPagarID") ?? "0";
        var numero = SponteXmlParser.Child(parcela, "NumeroParcela") ?? "0";
        var sacado = SponteXmlParser.Child(parcela, "Sacado") ?? "";
        var dueDate = ParseDateOnly(SponteXmlParser.Child(parcela, "Vencimento"))
                      ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var (categoryId, categoryName) = ResolveCategoryFields(root, parcela);

        return new CanonicalFinancial
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = $"pagar-{contaId}-{numero}",
            EnrollmentCode = sacado,
            UnitId = Guid.Empty,
            DebtAmount = debt,
            PaidAmount = valorPago,
            PaymentStatus = MapParcelStatus(situacao, debt),
            DueDate = dueDate,
            FlowDirection = FinancialFlowDirection.Payable,
            CategoryId = categoryId,
            CategoryName = categoryName,
            PaymentMethodLabel = SponteXmlParser.Child(parcela, "FormaCobranca")
                                 ?? SponteXmlParser.Child(parcela, "TipoRecebimento") ?? "",
            CounterpartyName = sacado,
            CashFlowDate = ResolveCashFlowDate(
                ParseDateOnly(SponteXmlParser.Child(parcela, "DataPagamento")),
                dueDate)
        };
    }

    public CanonicalCategory? NormalizeCategory(string erpProvider, string rawXml)
    {
        if (!IsSponte(erpProvider)) return null;

        var root = LoadRoot(rawXml);
        if (root is null) return null;

        var id = SponteXmlParser.Child(root, "CategoriaID");
        var nome = SponteXmlParser.Child(root, "Nome");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nome))
            return null;

        return new CanonicalCategory
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = id,
            Name = nome.Trim()
        };
    }

    public CanonicalContract? NormalizeContract(string erpProvider, string rawXml)
    {
        if (!IsSponte(erpProvider)) return null;

        var root = LoadRoot(rawXml);
        if (root is null) return null;

        // wsMatricula (GetMatriculas) — vínculo aluno via AlunoID; sem PII (nome do aluno ignorado)
        var contratoId = SponteXmlParser.Child(root, "ContratoID");
        var alunoId = SponteXmlParser.Child(root, "AlunoID");
        if (string.IsNullOrWhiteSpace(contratoId))
            return null;

        // Vínculo com aluno via AlunoID (mesmo ExternalId do GetAlunos). ETL pode enriquecer com NumeroMatricula.
        var enrollmentCode = alunoId ?? "";

        var turmaId = SponteXmlParser.Child(root, "TurmaID");

        return new CanonicalContract
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = contratoId,
            EnrollmentCode = enrollmentCode,
            UnitId = ToDeterministicGuid($"sponte-turma:{turmaId}"),
            TotalAmount = 0m,
            ContractStatus = MapContractStatus(SponteXmlParser.Child(root, "Situacao")),
            StartDate = ParseDateOnly(SponteXmlParser.Child(root, "DataInicio"))
                        ?? DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = ParseDateOnly(SponteXmlParser.Child(root, "DataTermino"))
                      ?? ParseDateOnly(SponteXmlParser.Child(root, "DataEncerramento"))
        };
    }

    private static bool IsSponte(string erpProvider) =>
        erpProvider.Equals("sponte", StringComparison.OrdinalIgnoreCase);

    private static XElement? LoadRoot(string rawXml)
    {
        try
        {
            return XDocument.Parse(rawXml).Root;
        }
        catch
        {
            return null;
        }
    }

    private static string MapStudentStatus(string? situacao, string? inadimplente)
    {
        if (inadimplente?.Equals("Sim", StringComparison.OrdinalIgnoreCase) == true
            || inadimplente?.Equals("1", StringComparison.OrdinalIgnoreCase) == true)
            return "delinquent";

        return situacao?.ToLowerInvariant() switch
        {
            "ativo" or "matriculado" => "active",
            "inativo" or "trancado" or "cancelado" => "inactive",
            _ => "unknown"
        };
    }

    private static string MapContractStatus(string? situacao) => situacao?.ToLowerInvariant() switch
    {
        "ativo" or "vigente" or "matriculado" => "active",
        "cancelado" or "encerrado" => "inactive",
        _ => "unknown"
    };

    private static string MapParcelStatus(string? situacaoParcela, decimal debt)
    {
        if (debt <= 0) return "paid";

        return situacaoParcela?.ToLowerInvariant() switch
        {
            "pago" or "quitado" => "paid",
            "vencido" or "vencida" or "atrasado" or "atrasada" => "overdue",
            _ => "pending"
        };
    }

    private static decimal CalculateDebt(decimal valorParcela, decimal valorPago, string? situacao)
    {
        var remaining = valorParcela - valorPago;
        if (remaining < 0) remaining = 0;

        if (situacao?.Contains("pago", StringComparison.OrdinalIgnoreCase) == true
            || situacao?.Contains("quitad", StringComparison.OrdinalIgnoreCase) == true)
            return 0;

        return remaining;
    }

    private static Guid ToDeterministicGuid(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return Guid.Empty;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static Guid? ParseOptionalGuid(string? v) =>
        Guid.TryParse(v, out var g) ? g : null;

    private static decimal? ParseDecimal(string? v) =>
        decimal.TryParse(v, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), out var d)
            ? d
            : decimal.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out d)
                ? d
                : null;

    private static DateOnly? ParseDateOnly(string? v) =>
        DateOnly.TryParse(v, out var d) ? d : null;

    private static DateTime? ParseDateTime(string? v) =>
        DateTime.TryParse(v, out var d) ? d : null;

    private static DateOnly ResolveCashFlowDate(DateOnly? paymentDate, DateOnly dueDate) =>
        paymentDate ?? dueDate;

    /// <summary>
    /// Categoria na parcela (Sponte) → contrato → TipoPlano/Curso. ID do ERP ou hash estável pelo nome.
    /// </summary>
    private static (string CategoryId, string CategoryName) ResolveCategoryFields(XElement root, XElement parcela)
    {
        var name = FirstNonEmpty(
            SponteXmlParser.Child(parcela, "Categoria"),
            SponteXmlParser.Child(root, "Categoria"),
            SponteXmlParser.Child(root, "TipoPlano"),
            SponteXmlParser.Child(root, "Curso"),
            SponteXmlParser.Child(parcela, "TipoRecebimento"));

        var erpId = FirstNonEmpty(
            SponteXmlParser.Child(parcela, "CategoriaID"),
            SponteXmlParser.Child(root, "CategoriaID"));

        if (string.IsNullOrWhiteSpace(erpId) && !string.IsNullOrWhiteSpace(name))
            erpId = ToDeterministicGuid($"sponte-cat:{name.Trim()}").ToString("N");

        return (erpId ?? "", name ?? "");
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
