-- Execute no banco EduFlow_DW (após tabelas dw.* existirem)
USE EduFlow_DW;
GO

CREATE OR ALTER VIEW dw.vw_DelinquencyByUnit
AS
SELECT
    fi.TenantId,
    u.UnitId,
    u.UnitCode,
    u.UnitName,
    fi.DelinquencyRate,
    fi.DebtAmount,
    fi.OverdueCount
FROM dw.FactInadimplencia fi
INNER JOIN dw.DimUnidade u ON u.TenantId = fi.TenantId AND u.UnitKey = fi.UnitKey
WHERE fi.DateKey = (
    SELECT MAX(f2.DateKey) FROM dw.FactInadimplencia f2 WHERE f2.TenantId = fi.TenantId
);
GO

CREATE OR ALTER VIEW dw.vw_DashboardKpis
AS
SELECT TenantId, [Key], [Label], [Value], [PreviousValue], [ChangePercent], [Format]
FROM (
    SELECT
        f.TenantId,
        CAST('revenue_total' AS VARCHAR(50)) AS [Key],
        CAST(N'Receita total' AS NVARCHAR(100)) AS [Label],
        CAST(ISNULL(SUM(f.PaidAmount), 0) AS DECIMAL(18,2)) AS [Value],
        CAST(NULL AS DECIMAL(18,2)) AS [PreviousValue],
        CAST(NULL AS DECIMAL(9,4)) AS [ChangePercent],
        CAST('currency' AS VARCHAR(20)) AS [Format]
    FROM dw.FactFinanceiro f
    GROUP BY f.TenantId

    UNION ALL

    SELECT
        f.TenantId,
        'debt_open',
        N'Em aberto',
        ISNULL(SUM(f.DebtAmount), 0),
        NULL, NULL, 'currency'
    FROM dw.FactFinanceiro f
    GROUP BY f.TenantId

    UNION ALL

    SELECT
        f.TenantId,
        'interest_total',
        N'Juros atribuídos',
        ISNULL(SUM(f.InterestAmount), 0),
        NULL, NULL, 'currency'
    FROM dw.FactFinanceiro f
    GROUP BY f.TenantId

    UNION ALL

    SELECT
        a.TenantId,
        'active_students',
        N'Alunos ativos',
        COUNT(*),
        NULL, NULL, 'integer'
    FROM dw.DimAluno a
    WHERE a.Status IN ('active', 'delinquent')
    GROUP BY a.TenantId

    UNION ALL

    SELECT
        e.TenantId,
        'churn_rate',
        N'Churn',
        MAX(e.ChurnRate),
        NULL, NULL, 'percent'
    FROM dw.FactEvasao e
    WHERE e.DateKey = (SELECT MAX(e2.DateKey) FROM dw.FactEvasao e2 WHERE e2.TenantId = e.TenantId)
    GROUP BY e.TenantId
) x;
GO
