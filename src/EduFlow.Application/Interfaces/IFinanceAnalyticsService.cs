using EduFlow.Application.DTOs;

namespace EduFlow.Application.Interfaces;

public interface IFinanceAnalyticsService
{
    Task<FinanceReceivablesDto> GetReceivablesAsync(
        Guid tenantId,
        DashboardFilterDto filter,
        CancellationToken ct = default);

    Task<FinanceCashFlowDto> GetCashFlowAsync(
        Guid tenantId,
        int year,
        int month,
        DashboardFilterDto filter,
        CancellationToken ct = default);

    Task<FinancePulseDto> GetPulseAsync(
        Guid tenantId,
        int year,
        int month,
        string dataBasis,
        DashboardFilterDto filter,
        CancellationToken ct = default);

    Task<IReadOnlyList<FinanceGoalDto>> GetGoalsAsync(
        Guid tenantId,
        int year,
        int month,
        Guid? unitId,
        CancellationToken ct = default);

    Task<FinanceGoalDto> UpsertGoalAsync(
        Guid tenantId,
        string key,
        UpsertFinanceGoalRequest request,
        CancellationToken ct = default);
}
