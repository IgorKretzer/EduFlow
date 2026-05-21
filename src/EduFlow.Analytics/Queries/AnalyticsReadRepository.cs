using Dapper;
using EduFlow.Application.DTOs;
using EduFlow.Application.Services;
using Microsoft.Data.SqlClient;

namespace EduFlow.Analytics.Queries;

public sealed class AnalyticsReadRepository : IAnalyticsReadRepository
{
    private readonly IDwConnectionFactory _dw;

    public AnalyticsReadRepository(IDwConnectionFactory dw)
    {
        DapperConfiguration.EnsureDateOnlyHandlers();
        _dw = dw;
    }

    public async Task<IReadOnlyList<DashboardKpiDto>> GetKpisAsync(
        Guid tenantId, DashboardFilterDto filter, CancellationToken ct)
    {
        const string sql = """
            SELECT [Key], [Label], [Value], [PreviousValue], [ChangePercent], [Format]
            FROM dw.vw_DashboardKpis
            WHERE TenantId = @TenantId
            """;

        const string fallback = """
            SELECT CAST('revenue_total' AS VARCHAR(50)) AS [Key],
                   CAST(N'Receita total' AS NVARCHAR(100)) AS [Label],
                   CAST(ISNULL(SUM(PaidAmount), 0) AS DECIMAL(18,2)) AS [Value],
                   CAST(NULL AS DECIMAL(18,2)) AS [PreviousValue],
                   CAST(NULL AS DECIMAL(9,4)) AS [ChangePercent],
                   CAST('currency' AS VARCHAR(20)) AS [Format]
            FROM dw.FactFinanceiro WHERE TenantId = @TenantId
            UNION ALL
            SELECT 'active_students', N'Alunos ativos', CAST(COUNT(*) AS DECIMAL(18,2)), NULL, NULL, 'integer'
            FROM dw.DimAluno WHERE TenantId = @TenantId AND Status IN ('active', 'delinquent')
            """;

        return await QueryWithViewFallbackAsync<DashboardKpiDto>(
            tenantId, sql, fallback, new { TenantId = tenantId }, ct);
    }

    public async Task<IReadOnlyList<DelinquencyByUnitDto>> GetDelinquencyByUnitAsync(
        Guid tenantId, DashboardFilterDto filter, CancellationToken ct)
    {
        const string sql = """
            SELECT UnitCode, UnitName, DelinquencyRate, DebtAmount, OverdueCount
            FROM dw.vw_DelinquencyByUnit
            WHERE TenantId = @TenantId
              AND (@UnitId IS NULL OR UnitId = @UnitId)
            ORDER BY DelinquencyRate DESC
            """;

        const string fallback = """
            SELECT u.UnitCode, u.UnitName, fi.DelinquencyRate, fi.DebtAmount, fi.OverdueCount
            FROM dw.FactInadimplencia fi
            INNER JOIN dw.DimUnidade u ON u.TenantId = fi.TenantId AND u.UnitKey = fi.UnitKey
            WHERE fi.TenantId = @TenantId
              AND (@UnitId IS NULL OR u.UnitId = @UnitId)
              AND fi.DateKey = (
                  SELECT MAX(f2.DateKey) FROM dw.FactInadimplencia f2 WHERE f2.TenantId = @TenantId)
            ORDER BY fi.DelinquencyRate DESC
            """;

        return await QueryWithViewFallbackAsync<DelinquencyByUnitDto>(
            tenantId, sql, fallback, new { TenantId = tenantId, filter.UnitId }, ct);
    }

    public async Task<IReadOnlyList<TimeSeriesPointDto>> GetRevenueTrendAsync(
        Guid tenantId, DashboardFilterDto filter, CancellationToken ct)
    {
        using var conn = _dw.Create();
        try
        {
            var rows = await conn.QueryAsync<TimeSeriesPointDto>(new CommandDefinition(
                """
                SELECT CAST(dt.[Date] AS DATE) AS [Date], SUM(ff.PaidAmount) AS Value
                FROM dw.FactFinanceiro ff
                INNER JOIN dw.DimTempo dt ON ff.DateKey = dt.DateKey
                WHERE ff.TenantId = @TenantId
                  AND (@From IS NULL OR dt.[Date] >= @From)
                  AND (@To IS NULL OR dt.[Date] <= @To)
                GROUP BY dt.[Date]
                ORDER BY dt.[Date]
                """,
                new
                {
                    TenantId = tenantId,
                    From = ToDbDate(filter.From),
                    To = ToDbDate(filter.To)
                },
                cancellationToken: ct));

            return rows.ToList();
        }
        catch (SqlException ex) when (IsMissingObject(ex))
        {
            return [];
        }
    }

    private static DateTime? ToDbDate(DateOnly? date) =>
        date?.ToDateTime(TimeOnly.MinValue);

    private async Task<IReadOnlyList<T>> QueryWithViewFallbackAsync<T>(
        Guid tenantId,
        string viewSql,
        string fallbackSql,
        object parameters,
        CancellationToken ct)
    {
        using var conn = _dw.Create();
        try
        {
            var rows = await conn.QueryAsync<T>(new CommandDefinition(
                viewSql, parameters, cancellationToken: ct));
            return rows.ToList();
        }
        catch (SqlException ex) when (IsMissingObject(ex))
        {
            try
            {
                var rows = await conn.QueryAsync<T>(new CommandDefinition(
                    fallbackSql, parameters, cancellationToken: ct));
                return rows.ToList();
            }
            catch (SqlException ex2) when (IsMissingObject(ex2))
            {
                return [];
            }
        }
    }

    private static bool IsMissingObject(SqlException ex) =>
        ex.Number is 208 or 207 or 4060; // objeto inválido / banco inacessível
}
