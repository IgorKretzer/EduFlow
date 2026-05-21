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
}
