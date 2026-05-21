using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;
using EduFlow.Infrastructure.Connectors.OpenApi;

namespace EduFlow.Infrastructure.Normalization;

/// <summary>
/// Adapter REST/OpenAPI → modelo canônico EduFlow (JSON flexível por campo).
/// </summary>
public sealed class OpenApiCanonicalNormalizer
{
    public CanonicalStudent? NormalizeStudent(string erpProvider, string rawXml)
    {
        if (!IsOpenApi(erpProvider)) return null;
        if (!OpenApiRecordEnvelope.TryGetJson(rawXml, out var json)) return null;

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        var externalId = PickString(el, "id", "externalId", "alunoId", "AlunoID", "studentId");
        var enrollment = PickString(el, "enrollmentCode", "numeroMatricula", "NumeroMatricula", "matricula", "registrationNumber");
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(enrollment))
            return null;

        return new CanonicalStudent
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = externalId,
            EnrollmentCode = enrollment,
            UnitId = Guid.Empty,
            CourseId = ParseOptionalGuid(PickString(el, "courseId", "cursoId")),
            Status = MapStudentStatus(PickString(el, "status", "situacao", "Situacao"), PickBool(el, "delinquent", "inadimplente")),
            EnrolledAt = ParseDateTime(PickString(el, "enrolledAt", "dataCadastro", "createdAt"))
        };
    }

    public CanonicalFinancial? NormalizeFinancial(string erpProvider, string rawXml)
    {
        if (!IsOpenApi(erpProvider)) return null;
        if (!OpenApiRecordEnvelope.TryGetJson(rawXml, out var json)) return null;

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        var externalId = PickString(el, "id", "externalId", "parcelaId", "invoiceId", "ContaReceberID");
        var enrollment = PickString(el, "enrollmentCode", "numeroMatricula", "matricula");
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(enrollment))
            return null;

        var unitCode = PickString(el, "unitCode", "codigoUnidade", "CodigoUnidade") ?? "";
        var debt = PickDecimal(el, "debtAmount", "valorEmAberto", "remainingAmount") ?? 0m;
        var paid = PickDecimal(el, "paidAmount", "valorPago", "ValorPago") ?? 0m;
        var status = PickString(el, "paymentStatus", "situacaoParcela", "status");

        if (debt <= 0 && paid > 0)
            debt = Math.Max(0, (PickDecimal(el, "amount", "valorParcela", "ValorParcela") ?? paid) - paid);

        var dueDate = ParseDateOnly(PickString(el, "dueDate", "vencimento", "Vencimento"))
                      ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var paymentDate = ParseDateOnly(PickString(el, "paymentDate", "dataPagamento", "DataPagamento"));
        var direction = PickString(el, "flowDirection", "tipo", "direction")
                        ?? FinancialFlowDirection.Receivable;

        return new CanonicalFinancial
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = externalId,
            EnrollmentCode = enrollment,
            UnitId = string.IsNullOrWhiteSpace(unitCode)
                ? Guid.Empty
                : ToDeterministicGuid($"openapi-unit:{unitCode}"),
            UnitCode = unitCode,
            DebtAmount = debt,
            PaidAmount = paid,
            PaymentStatus = MapParcelStatus(status, debt),
            DueDate = dueDate,
            FlowDirection = direction.Contains("pay", StringComparison.OrdinalIgnoreCase)
                ? FinancialFlowDirection.Payable
                : FinancialFlowDirection.Receivable,
            CategoryName = PickString(el, "categoryName", "categoria", "Categoria") ?? "",
            PaymentMethodLabel = PickString(el, "paymentMethod", "formaCobranca") ?? "",
            CashFlowDate = paymentDate ?? dueDate
        };
    }

    public CanonicalFinancial? NormalizePayable(string erpProvider, string rawXml) =>
        NormalizeFinancial(erpProvider, rawXml) is { } f
        && f.FlowDirection == FinancialFlowDirection.Payable
            ? f
            : null;

    public CanonicalCategory? NormalizeCategory(string erpProvider, string rawXml)
    {
        if (!IsOpenApi(erpProvider)) return null;
        if (!OpenApiRecordEnvelope.TryGetJson(rawXml, out var json)) return null;

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;
        var id = PickString(el, "id", "categoryId", "CategoriaID");
        var name = PickString(el, "name", "nome", "Nome");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            return null;

        return new CanonicalCategory
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = id,
            Name = name.Trim()
        };
    }

    public CanonicalContract? NormalizeContract(string erpProvider, string rawXml)
    {
        if (!IsOpenApi(erpProvider)) return null;
        if (!OpenApiRecordEnvelope.TryGetJson(rawXml, out var json)) return null;

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        var externalId = PickString(el, "id", "externalId", "contratoId", "ContratoID", "contractId");
        if (string.IsNullOrWhiteSpace(externalId)) return null;

        var enrollment = PickString(el, "enrollmentCode", "alunoId", "AlunoID", "studentId") ?? "";
        var turma = PickString(el, "classId", "turmaId", "TurmaID");

        return new CanonicalContract
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            ExternalId = externalId,
            EnrollmentCode = enrollment,
            UnitId = ToDeterministicGuid($"openapi-turma:{turma}"),
            TotalAmount = PickDecimal(el, "totalAmount", "valorTotal") ?? 0m,
            ContractStatus = MapContractStatus(PickString(el, "contractStatus", "status", "situacao")),
            StartDate = ParseDateOnly(PickString(el, "startDate", "dataInicio", "DataInicio"))
                        ?? DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = ParseDateOnly(PickString(el, "endDate", "dataTermino", "DataTermino", "dataEncerramento"))
        };
    }

    private static bool IsOpenApi(string erpProvider) =>
        erpProvider.Equals("openapi", StringComparison.OrdinalIgnoreCase);

    private static string? PickString(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var v)) continue;
            if (v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
            else if (v.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                return v.GetRawText();
        }

        return null;
    }

    private static bool PickBool(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var v)) continue;
            if (v.ValueKind == JsonValueKind.True) return true;
            if (v.ValueKind == JsonValueKind.False) return false;
            if (v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                return s?.Equals("sim", StringComparison.OrdinalIgnoreCase) == true
                       || s == "1";
            }
        }

        return false;
    }

    private static decimal? PickDecimal(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var v)) continue;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d))
                return d;
            if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return d;
        }

        return null;
    }

    private static string MapStudentStatus(string? status, bool delinquent)
    {
        if (delinquent) return "delinquent";
        return status?.ToLowerInvariant() switch
        {
            "active" or "ativo" or "matriculado" => "active",
            "inactive" or "inativo" or "trancado" or "cancelado" => "inactive",
            _ => "unknown"
        };
    }

    private static string MapContractStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "active" or "ativo" or "vigente" => "active",
        "inactive" or "cancelado" or "encerrado" => "inactive",
        _ => "unknown"
    };

    private static string MapParcelStatus(string? status, decimal debt)
    {
        if (debt <= 0) return "paid";
        return status?.ToLowerInvariant() switch
        {
            "paid" or "pago" or "quitado" => "paid",
            "overdue" or "vencido" or "vencida" or "atrasado" => "overdue",
            _ => "pending"
        };
    }

    private static Guid ToDeterministicGuid(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return Guid.Empty;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static Guid? ParseOptionalGuid(string? v) =>
        Guid.TryParse(v, out var g) ? g : null;

    private static DateOnly? ParseDateOnly(string? v) =>
        DateOnly.TryParse(v, out var d) ? d : null;

    private static DateTime? ParseDateTime(string? v) =>
        DateTime.TryParse(v, out var d) ? d : null;
}
