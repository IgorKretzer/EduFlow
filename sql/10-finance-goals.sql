-- Metas financeiras cadastradas no EduFlow.
-- Os lançamentos financeiros continuam vindo do ERP; esta tabela guarda apenas metas do gestor.

IF OBJECT_ID(N'FinanceGoals', N'U') IS NULL
BEGIN
    CREATE TABLE FinanceGoals (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        UnitId UNIQUEIDENTIFIER NULL,
        [Year] INT NOT NULL,
        [Month] INT NOT NULL,
        [Key] NVARCHAR(64) NOT NULL,
        [Label] NVARCHAR(160) NOT NULL,
        TargetAmount DECIMAL(18,2) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        UpdatedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT UQ_FinanceGoals_Tenant_Unit_Period_Key
            UNIQUE (TenantId, UnitId, [Year], [Month], [Key])
    );
END;
