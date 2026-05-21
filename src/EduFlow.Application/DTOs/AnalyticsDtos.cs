namespace EduFlow.Application.DTOs;

public sealed record DashboardKpiDto(
    string Key,
    string Label,
    decimal Value,
    decimal? PreviousValue,
    decimal? ChangePercent,
    string Format);

public sealed record DelinquencyByUnitDto(
    string UnitCode,
    string UnitName,
    decimal DelinquencyRate,
    decimal DebtAmount,
    int OverdueCount);

public sealed record InsightDto(
    string Code,
    string Title,
    string Message,
    string Severity,
    DateTime GeneratedAt,
    string? RecommendedAction = null,
    string? DeepLink = null,
    string? Category = null);

public sealed record DashboardFilterDto(
    Guid? UnitId,
    DateOnly? From,
    DateOnly? To);

public sealed record TimeSeriesPointDto(
    DateOnly Date,
    decimal Value);

public sealed record DashboardSummaryDto(
    IReadOnlyList<DashboardKpiDto> Kpis,
    IReadOnlyList<DelinquencyByUnitDto> DelinquencyByUnit,
    IReadOnlyList<InsightDto> Insights,
    IReadOnlyList<TimeSeriesPointDto> RevenueTrend);
