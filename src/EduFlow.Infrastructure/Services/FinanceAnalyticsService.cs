using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;
using EduFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduFlow.Infrastructure.Services;

public sealed class FinanceAnalyticsService : IFinanceAnalyticsService
{
    private readonly StagingDbContext _db;

    public FinanceAnalyticsService(StagingDbContext db) => _db = db;

    public async Task<FinanceReceivablesDto> GetReceivablesAsync(
        Guid tenantId,
        DashboardFilterDto filter,
        CancellationToken ct = default)
    {
        var lines = await LoadReceivableLinesAsync(tenantId, filter, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var open = lines.Where(l => l.DebtAmount > 0).ToList();
        var overdue = open.Where(l => l.DueDate < today).ToList();
        var toMature = open.Where(l => l.DueDate >= today).ToList();

        var overdueAmount = overdue.Sum(l => l.DebtAmount);
        var toMatureAmount = toMature.Sum(l => l.DebtAmount);
        var totalReceivable = overdueAmount + toMatureAmount;
        var totalPaid = lines.Sum(l => l.PaidAmount);
        var totalInvoice = lines.Sum(l => l.DebtAmount + l.PaidAmount);

        var people = open
            .Select(l => l.EnrollmentCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var overdueShare = totalReceivable > 0 ? overdueAmount / totalReceivable * 100m : 0m;

        var summary = new FinanceReceivablesSummaryDto(
            totalReceivable,
            overdueAmount,
            toMatureAmount,
            totalPaid,
            totalInvoice,
            people,
            overdueShare,
            100m - overdueShare);

        return new FinanceReceivablesDto(
            summary,
            BuildSlices(overdue),
            BuildSituations(overdue),
            BuildSlices(toMature),
            BuildSituations(toMature));
    }

    public async Task<FinanceCashFlowDto> GetCashFlowAsync(
        Guid tenantId,
        int year,
        int month,
        DashboardFilterDto filter,
        CancellationToken ct = default)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Mês inválido.");

        var all = await LoadAllFinancialLinesAsync(tenantId, filter, ct);
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var receivables = all.Where(IsReceivable).ToList();
        var payables = all.Where(IsPayable).ToList();

        decimal NetBefore(DateOnly date) =>
            receivables.Where(l => l.CashFlowDate < date && l.PaidAmount > 0).Sum(l => l.PaidAmount)
            - payables.Where(l => l.CashFlowDate < date && l.PaidAmount > 0).Sum(l => l.PaidAmount);

        var openingBalance = NetBefore(periodStart);

        var totalInflow = receivables
            .Where(l => l.CashFlowDate >= periodStart && l.CashFlowDate <= periodEnd && l.PaidAmount > 0)
            .Sum(l => l.PaidAmount);

        var totalOutflow = payables
            .Where(l => l.CashFlowDate >= periodStart && l.CashFlowDate <= periodEnd && l.PaidAmount > 0)
            .Sum(l => l.PaidAmount);

        var closingBalance = openingBalance + totalInflow - totalOutflow;

        var openDebt = receivables.Where(l => l.DebtAmount > 0).ToList();
        var totalOpen = openDebt.Sum(l => l.DebtAmount);
        var overdueOpen = openDebt.Where(l => l.DueDate < today).Sum(l => l.DebtAmount);
        var delinquencyPct = totalOpen > 0 ? overdueOpen / totalOpen * 100m : 0m;

        var inflowShare = totalInflow + totalOutflow > 0
            ? totalInflow / (totalInflow + totalOutflow) * 100m
            : 100m;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var days = new List<FinanceCashFlowDayDto>(daysInMonth);
        var running = openingBalance;

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var openingDay = running;

            var inflow = receivables
                .Where(l => l.CashFlowDate == date && l.PaidAmount > 0)
                .Sum(l => l.PaidAmount);

            var outflow = payables
                .Where(l => l.CashFlowDate == date && l.PaidAmount > 0)
                .Sum(l => l.PaidAmount);

            var operational = inflow - outflow;
            running = openingDay + operational;

            var scheduled = receivables
                .Where(l => l.DueDate == date && l.DebtAmount > 0)
                .Sum(l => l.DebtAmount);

            days.Add(new FinanceCashFlowDayDto(
                date.ToString("yyyy-MM-dd"),
                day,
                openingDay,
                inflow,
                outflow,
                operational,
                running,
                scheduled));
        }

        var inflowBreakdown = receivables
            .Where(l => l.CashFlowDate >= periodStart && l.CashFlowDate <= periodEnd && l.PaidAmount > 0)
            .GroupBy(l => SliceGroupKey(l), StringComparer.OrdinalIgnoreCase)
            .Select(g => new FinanceBreakdownDto(g.Key, g.Sum(x => x.PaidAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var outflowBreakdown = payables
            .Where(l => l.CashFlowDate >= periodStart && l.CashFlowDate <= periodEnd && l.PaidAmount > 0)
            .GroupBy(l => SliceGroupKey(l), StringComparer.OrdinalIgnoreCase)
            .Select(g => new FinanceBreakdownDto(g.Key, g.Sum(x => x.PaidAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var summary = new FinanceCashFlowSummaryDto(
            openingBalance,
            totalInflow,
            totalOutflow,
            closingBalance,
            delinquencyPct,
            inflowShare,
            100m - inflowShare,
            inflowBreakdown,
            outflowBreakdown);

        return new FinanceCashFlowDto(summary, days);
    }

    private async Task<List<CanonicalFinancial>> LoadReceivableLinesAsync(
        Guid tenantId,
        DashboardFilterDto filter,
        CancellationToken ct)
    {
        var all = await LoadAllFinancialLinesAsync(tenantId, filter, ct);
        return all.Where(IsReceivable).ToList();
    }

    private async Task<List<CanonicalFinancial>> LoadAllFinancialLinesAsync(
        Guid tenantId,
        DashboardFilterDto filter,
        CancellationToken ct)
    {
        var query = _db.Financials.AsNoTracking().Where(f => f.TenantId == tenantId);

        if (filter.UnitId is Guid unitId)
            query = query.Where(f => f.UnitId == unitId);

        if (filter.From is DateOnly from)
            query = query.Where(f => f.DueDate >= from);

        if (filter.To is DateOnly to)
            query = query.Where(f => f.DueDate <= to);

        var rows = await query.ToListAsync(ct);
        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(ct);
        var categoryByName = categories
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ExternalId, StringComparer.OrdinalIgnoreCase);

        return rows.Select(r => EnrichCategoryMetadata(r, categoryByName)).ToList();
    }

    private static CanonicalFinancial EnrichCategoryMetadata(
        CanonicalFinancial row,
        IReadOnlyDictionary<string, string> categoryByName)
    {
        var flow = string.IsNullOrWhiteSpace(row.FlowDirection)
            ? FinancialFlowDirection.Receivable
            : row.FlowDirection;
        var cashFlowDate = row.CashFlowDate == default ? row.DueDate : row.CashFlowDate;

        var categoryName = row.CategoryName;
        var categoryId = row.CategoryId;

        if (string.IsNullOrWhiteSpace(categoryName) && !string.IsNullOrWhiteSpace(row.PaymentMethodLabel))
            categoryName = row.PaymentMethodLabel;

        if (!string.IsNullOrWhiteSpace(categoryName) && string.IsNullOrWhiteSpace(categoryId)
            && categoryByName.TryGetValue(categoryName.Trim(), out var mappedId))
            categoryId = mappedId;

        return row with
        {
            FlowDirection = flow,
            CashFlowDate = cashFlowDate,
            CategoryName = categoryName ?? "",
            CategoryId = categoryId ?? ""
        };
    }

    private static bool IsReceivable(CanonicalFinancial l) =>
        string.IsNullOrWhiteSpace(l.FlowDirection)
        || l.FlowDirection.Equals(FinancialFlowDirection.Receivable, StringComparison.OrdinalIgnoreCase);

    private static bool IsPayable(CanonicalFinancial l) =>
        l.FlowDirection.Equals(FinancialFlowDirection.Payable, StringComparison.OrdinalIgnoreCase);

    private static string SliceGroupKey(CanonicalFinancial l)
    {
        if (!string.IsNullOrWhiteSpace(l.CategoryName)) return l.CategoryName.Trim();
        if (!string.IsNullOrWhiteSpace(l.PaymentMethodLabel)) return l.PaymentMethodLabel.Trim();
        if (!string.IsNullOrWhiteSpace(l.CategoryId)) return $"Categoria {l.CategoryId}";
        return StatusLabel(l.PaymentStatus);
    }

    private static IReadOnlyList<FinanceAmountSliceDto> BuildSlices(IReadOnlyList<CanonicalFinancial> lines)
    {
        return lines
            .GroupBy(SliceGroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var open = g.Sum(x => x.DebtAmount);
                var invoice = g.Sum(x => x.DebtAmount + x.PaidAmount);
                var id = g.Key.ToLowerInvariant().Replace(' ', '-');
                return new FinanceAmountSliceDto(id, g.Key, invoice, open);
            })
            .OrderByDescending(s => s.OpenAmount)
            .ToList();
    }

    private static IReadOnlyList<FinanceSituationDto> BuildSituations(IReadOnlyList<CanonicalFinancial> lines)
    {
        return lines
            .GroupBy(SliceGroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FinanceSituationDto(
                g.Key.ToLowerInvariant().Replace(' ', '-'),
                g.Key,
                g.Sum(x => x.DebtAmount)))
            .OrderByDescending(s => s.Amount)
            .ToList();
    }

    private static string StatusLabel(string status) =>
        status.ToLowerInvariant() switch
        {
            "overdue" => "Vencido",
            "pending" => "Em aberto",
            "paid" => "Pago",
            _ => string.IsNullOrWhiteSpace(status) ? "Sem status" : status
        };
}
