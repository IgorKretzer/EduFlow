namespace EduFlow.Application.DTOs;

public sealed record EnrollmentSummaryDto(
    string EnrollmentCode,
    string UnitCode,
    string UnitName,
    string StudentStatus,
    string PaymentStatus,
    decimal DebtAmount,
    decimal PaidAmount,
    string ChurnRisk,
    string RetentionLevel,
    string Trend,
    int? DaysSinceLastPayment,
    int OperationalScore);

public sealed record EnrollmentFinancialLineDto(
    string ExternalId,
    decimal DebtAmount,
    decimal PaidAmount,
    string PaymentStatus,
    DateOnly DueDate,
    DateTime SyncedAt);

public sealed record EnrollmentDetailDto(
    EnrollmentSummaryDto Summary,
    IReadOnlyList<EnrollmentFinancialLineDto> FinancialHistory);

public sealed record EnrollmentPageDto(
    IReadOnlyList<EnrollmentSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
