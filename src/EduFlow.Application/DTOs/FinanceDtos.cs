namespace EduFlow.Application.DTOs;

public sealed record FinanceAmountSliceDto(
    string Id,
    string Label,
    decimal InvoiceAmount,
    decimal OpenAmount);

public sealed record FinanceSituationDto(
    string Id,
    string Label,
    decimal Amount);

public sealed record FinanceReceivablesSummaryDto(
    decimal TotalReceivable,
    decimal Overdue,
    decimal ToReceive,
    decimal TotalPaid,
    decimal TotalInvoice,
    decimal TotalInterest,
    int PeopleCount,
    decimal OverdueShare,
    decimal ToReceiveShare);

public sealed record FinanceReceivablesDto(
    FinanceReceivablesSummaryDto Summary,
    IReadOnlyList<FinanceAmountSliceDto> OverdueSlices,
    IReadOnlyList<FinanceSituationDto> OverdueSituations,
    IReadOnlyList<FinanceAmountSliceDto> ToMatureSlices,
    IReadOnlyList<FinanceSituationDto> ToMatureSituations);

public sealed record FinanceBreakdownDto(string Label, decimal Amount);

public sealed record FinanceCashFlowDayDto(
    string Date,
    int Day,
    decimal OpeningBalance,
    decimal Inflow,
    decimal Outflow,
    decimal OperationalBalance,
    decimal ClosingBalance,
    decimal ScheduledReceivable);

public sealed record FinanceCashFlowSummaryDto(
    decimal OpeningBalance,
    decimal TotalInflow,
    decimal TotalOutflow,
    decimal ClosingBalance,
    decimal DelinquencyRatePct,
    decimal TotalInterest,
    decimal InflowShare,
    decimal OutflowShare,
    IReadOnlyList<FinanceBreakdownDto> InflowBreakdown,
    IReadOnlyList<FinanceBreakdownDto> OutflowBreakdown);

public sealed record FinanceCashFlowDto(
    FinanceCashFlowSummaryDto Summary,
    IReadOnlyList<FinanceCashFlowDayDto> Days);

public sealed record FinancePulseMetricDto(
    string Key,
    string Label,
    decimal Value,
    string Format,
    decimal? PreviousValue,
    decimal? ChangePercent,
    string Tone,
    string Hint);

public sealed record FinancePulseSignalDto(
    string Code,
    string Title,
    string Message,
    string Severity,
    string MetricKey,
    string RecommendedAction,
    string DeepLink);

public sealed record FinancePulseCalendarDayDto(
    string Date,
    int Day,
    decimal ExpectedInflow,
    decimal RealizedInflow,
    decimal ExpectedOutflow,
    decimal RealizedOutflow,
    decimal ProjectedBalance,
    decimal OverdueAmount,
    string Status);

public sealed record FinanceStatementLineDto(
    string Key,
    string Label,
    decimal Amount,
    string Kind,
    int Order);

public sealed record FinancePulseGoalDto(
    string Key,
    string Label,
    decimal? TargetAmount,
    decimal ActualAmount,
    decimal? ProgressPercent,
    string Source);

public sealed record FinanceGoalDto(
    Guid Id,
    Guid? UnitId,
    int Year,
    int Month,
    string Key,
    string Label,
    decimal TargetAmount,
    DateTime UpdatedAtUtc);

public sealed record UpsertFinanceGoalRequest(
    Guid? UnitId,
    int Year,
    int Month,
    string Label,
    decimal TargetAmount);

public sealed record FinancePulseDto(
    int Year,
    int Month,
    string DataBasis,
    string HealthStatus,
    string HealthMessage,
    IReadOnlyList<FinancePulseMetricDto> Metrics,
    IReadOnlyList<FinancePulseSignalDto> Signals,
    IReadOnlyList<FinancePulseCalendarDayDto> Calendar,
    IReadOnlyList<FinanceStatementLineDto> Statement,
    IReadOnlyList<FinancePulseGoalDto> Goals,
    IReadOnlyList<string> Gaps);
