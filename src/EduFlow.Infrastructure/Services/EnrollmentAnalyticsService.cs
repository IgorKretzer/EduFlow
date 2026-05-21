using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Infrastructure.Etl;
using EduFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduFlow.Infrastructure.Services;

public sealed class EnrollmentAnalyticsService : IEnrollmentAnalyticsService
{
    private readonly StagingDbContext _db;

    public EnrollmentAnalyticsService(StagingDbContext db) => _db = db;

    public async Task<EnrollmentPageDto> ListPagedAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? unitCode = null,
        string? risk = null,
        bool riskOnly = false,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var summaries = await BuildAllSummariesAsync(tenantId, ct);

        IEnumerable<EnrollmentSummaryDto> query = summaries;

        if (riskOnly)
            query = query.Where(e => e.ChurnRisk is "Alto" or "Médio");

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.EnrollmentCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.UnitName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(unitCode))
            query = query.Where(e => e.UnitCode.Equals(unitCode, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(risk))
            query = query.Where(e => e.ChurnRisk.Equals(risk, StringComparison.OrdinalIgnoreCase));

        var ordered = query
            .OrderByDescending(e => e.ChurnRisk == "Alto")
            .ThenByDescending(e => e.OperationalScore)
            .ThenByDescending(e => e.DebtAmount)
            .ToList();

        var total = ordered.Count;
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new EnrollmentPageDto(items, total, page, pageSize);
    }

    public async Task<EnrollmentDetailDto?> GetByEnrollmentCodeAsync(
        Guid tenantId,
        string enrollmentCode,
        CancellationToken ct = default)
    {
        var summaries = await BuildAllSummariesAsync(tenantId, ct);
        var summary = summaries.FirstOrDefault(e =>
            e.EnrollmentCode.Equals(enrollmentCode, StringComparison.OrdinalIgnoreCase));

        if (summary is null) return null;

        var lines = await _db.Financials.AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.EnrollmentCode == enrollmentCode)
            .OrderByDescending(f => f.DueDate)
            .Select(f => new EnrollmentFinancialLineDto(
                f.ExternalId,
                f.DebtAmount,
                f.PaidAmount,
                f.PaymentStatus,
                f.DueDate,
                f.SyncedAt))
            .ToListAsync(ct);

        return new EnrollmentDetailDto(summary, lines);
    }

    private async Task<List<EnrollmentSummaryDto>> BuildAllSummariesAsync(Guid tenantId, CancellationToken ct)
    {
        var students = await _db.Students.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .Select(s => new { s.EnrollmentCode, s.Status, s.UnitId })
            .ToListAsync(ct);

        var financialAgg = await _db.Financials.AsNoTracking()
            .Where(f => f.TenantId == tenantId)
            .GroupBy(f => f.EnrollmentCode)
            .Select(g => new
            {
                Code = g.Key,
                Debt = g.Sum(x => x.DebtAmount),
                Paid = g.Sum(x => x.PaidAmount),
                LatestDue = g.Max(x => x.DueDate),
                HasOverdue = g.Any(x =>
                    x.PaymentStatus == "overdue" ||
                    (x.DebtAmount > 0 && x.DueDate < DateOnly.FromDateTime(DateTime.UtcNow))),
                Latest = g.OrderByDescending(x => x.DueDate).First()
            })
            .ToListAsync(ct);

        var studentByCode = students.ToDictionary(s => s.EnrollmentCode, StringComparer.OrdinalIgnoreCase);
        var result = new List<EnrollmentSummaryDto>();

        foreach (var agg in financialAgg)
        {
            studentByCode.TryGetValue(agg.Code, out var student);
            var (_, unitName) = UnitDisplayNames.FromSponteCode(
                agg.Latest.UnitCode,
                agg.Latest.UnitId != Guid.Empty ? agg.Latest.UnitId : Guid.Empty);

            result.Add(BuildSummary(
                agg.Code,
                agg.Latest.UnitCode,
                unitName,
                student?.Status ?? "unknown",
                agg.HasOverdue ? "overdue" : agg.Latest.PaymentStatus,
                agg.Debt,
                agg.Paid,
                agg.LatestDue));
        }

        foreach (var s in students.Where(s => financialAgg.All(f => f.Code != s.EnrollmentCode)))
        {
            var (_, unitName) = UnitDisplayNames.FromSponteCode("", s.UnitId);
            result.Add(BuildSummary(s.EnrollmentCode, "", unitName, s.Status, "none", 0, 0, null));
        }

        return result;
    }

    private static EnrollmentSummaryDto BuildSummary(
        string code,
        string unitCode,
        string unitName,
        string studentStatus,
        string paymentStatus,
        decimal debt,
        decimal paid,
        DateOnly? latestDue)
    {
        var daysSince = latestDue.HasValue
            ? (int?)(DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - latestDue.Value.DayNumber)
            : null;

        var churn = ClassifyChurn(paymentStatus, debt, studentStatus, daysSince);
        var retention = ClassifyRetention(studentStatus, paymentStatus);
        var trend = ClassifyTrend(churn, debt, daysSince);
        var score = ComputeOperationalScore(churn, paymentStatus, debt, daysSince);

        return new EnrollmentSummaryDto(
            code,
            unitCode,
            unitName,
            MapStatus(studentStatus),
            MapPayment(paymentStatus),
            debt,
            paid,
            churn,
            retention,
            trend,
            daysSince > 0 ? daysSince : null,
            score);
    }

    private static int ComputeOperationalScore(string churn, string payment, decimal debt, int? daysSince)
    {
        var score = churn switch
        {
            "Alto" => 28,
            "Médio" => 52,
            _ => 82
        };
        if (payment == "overdue") score -= 18;
        if (debt > 500) score -= 12;
        if (daysSince is > 30) score -= 10;
        return Math.Clamp(score, 5, 99);
    }

    private static string ClassifyChurn(string payment, decimal debt, string studentStatus, int? daysSince)
    {
        if (payment == "overdue" || debt > 500) return "Alto";
        if (studentStatus is "inactive") return "Alto";
        if (debt > 0 || daysSince is > 30) return "Médio";
        return "Baixo";
    }

    private static string ClassifyRetention(string studentStatus, string payment) =>
        studentStatus is "active" && payment is not "overdue" ? "Alta"
        : studentStatus is "delinquent" or "inactive" ? "Baixa"
        : "Média";

    private static string ClassifyTrend(string churn, decimal debt, int? daysSince)
    {
        if (churn == "Alto") return "Risco operacional crescente";
        if (debt > 0 && daysSince is > 15) return "Atenção financeira";
        if (churn == "Baixo") return "Estável";
        return "Monitorar";
    }

    private static string MapStatus(string s) => s switch
    {
        "active" => "Ativo",
        "inactive" => "Inativo",
        "delinquent" => "Inadimplente",
        _ => s
    };

    private static string MapPayment(string p) => p switch
    {
        "paid" => "Em dia",
        "overdue" => "Inadimplente",
        "pending" => "Pendente",
        "none" => "Sem movimento",
        _ => p
    };
}
