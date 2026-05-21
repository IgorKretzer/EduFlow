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
    decimal InflowShare,
    decimal OutflowShare,
    IReadOnlyList<FinanceBreakdownDto> InflowBreakdown,
    IReadOnlyList<FinanceBreakdownDto> OutflowBreakdown);

public sealed record FinanceCashFlowDto(
    FinanceCashFlowSummaryDto Summary,
    IReadOnlyList<FinanceCashFlowDayDto> Days);
