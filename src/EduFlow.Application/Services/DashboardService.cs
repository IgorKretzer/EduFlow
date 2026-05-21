using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Rules;

namespace EduFlow.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IAnalyticsReadRepository _analytics;
    private readonly InsightRuleEngine _insightEngine = new();

    public DashboardService(IAnalyticsReadRepository analytics)
    {
        _analytics = analytics;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        Guid tenantId,
        DashboardFilterDto filter,
        CancellationToken ct = default)
    {
        var kpis = await _analytics.GetKpisAsync(tenantId, filter, ct);
        var delinquency = await _analytics.GetDelinquencyByUnitAsync(tenantId, filter, ct);
        var revenueTrend = await _analytics.GetRevenueTrendAsync(tenantId, filter, ct);

        var insights = BuildInsights(tenantId, delinquency, kpis);

        return new DashboardSummaryDto(kpis, delinquency, insights, revenueTrend);
    }

    private IReadOnlyList<InsightDto> BuildInsights(
        Guid tenantId,
        IReadOnlyList<DelinquencyByUnitDto> units,
        IReadOnlyList<DashboardKpiDto> kpis)
    {
        var result = new List<InsightDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddFromContext(InsightRuleContext ctx)
        {
            foreach (var insight in _insightEngine.Evaluate(ctx))
            {
                var key = $"{insight.Code}:{ctx.UnitCode}:{insight.Title}";
                if (!seen.Add(key)) continue;

                result.Add(new InsightDto(
                    insight.Code,
                    insight.Title,
                    insight.Message,
                    insight.Severity,
                    insight.GeneratedAt,
                    insight.RecommendedAction,
                    insight.DeepLink,
                    insight.Category));
            }
        }

        foreach (var unit in units)
        {
            AddFromContext(new InsightRuleContext
            {
                TenantId = tenantId,
                UnitCode = unit.UnitCode,
                UnitName = unit.UnitName,
                MetricKey = "delinquency_rate",
                CurrentValue = unit.DelinquencyRate,
                PreviousValue = unit.DelinquencyRate * 0.85m
            });
        }

        foreach (var kpi in kpis)
        {
            var metricKey = MapKpiMetricKey(kpi.Key);
            if (metricKey is null) continue;

            var previous = kpi.PreviousValue ?? (metricKey switch
            {
                "retention_rate" => kpi.Value * 0.97m,
                "revenue" => kpi.Value * 1.05m,
                "churn_rate" => kpi.Value * 0.9m,
                _ => kpi.Value
            });

            AddFromContext(new InsightRuleContext
            {
                TenantId = tenantId,
                MetricKey = metricKey,
                CurrentValue = kpi.Value,
                PreviousValue = previous
            });
        }

        return result
            .OrderByDescending(i => i.Severity == "critical")
            .ThenByDescending(i => i.Severity == "warning")
            .ThenBy(i => i.GeneratedAt)
            .ToList();
    }

    private static string? MapKpiMetricKey(string kpiKey)
    {
        var k = kpiKey.ToLowerInvariant();
        if (k.Contains("retenc")) return "retention_rate";
        if (k.Contains("receita") || k.Contains("revenue")) return "revenue";
        if (k.Contains("churn") || k.Contains("evas")) return "churn_rate";
        return null;
    }
}

/// <summary>
/// Porta de leitura do DW — implementada em Analytics com Dapper.
/// </summary>
public interface IAnalyticsReadRepository
{
    Task<IReadOnlyList<DashboardKpiDto>> GetKpisAsync(Guid tenantId, DashboardFilterDto filter, CancellationToken ct);
    Task<IReadOnlyList<DelinquencyByUnitDto>> GetDelinquencyByUnitAsync(Guid tenantId, DashboardFilterDto filter, CancellationToken ct);
    Task<IReadOnlyList<TimeSeriesPointDto>> GetRevenueTrendAsync(Guid tenantId, DashboardFilterDto filter, CancellationToken ct);
}
