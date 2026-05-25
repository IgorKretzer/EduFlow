-- Índices de apoio para o Pulso Financeiro no staging.
-- Mantém a EduFlow como camada analítica: dados financeiros vêm do ERP, metas vêm do EduFlow.

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CanonicalFinancials_Tenant_Due_Flow'
      AND object_id = OBJECT_ID('CanonicalFinancials')
)
CREATE INDEX IX_CanonicalFinancials_Tenant_Due_Flow
ON CanonicalFinancials (TenantId, DueDate, FlowDirection)
INCLUDE (UnitId, DebtAmount, PaidAmount, PaymentStatus, CashFlowDate, CategoryName, PaymentMethodLabel);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CanonicalFinancials_Tenant_Cash_Flow'
      AND object_id = OBJECT_ID('CanonicalFinancials')
)
CREATE INDEX IX_CanonicalFinancials_Tenant_Cash_Flow
ON CanonicalFinancials (TenantId, CashFlowDate, FlowDirection)
INCLUDE (UnitId, DebtAmount, PaidAmount, PaymentStatus, DueDate, CategoryName, PaymentMethodLabel);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_FinanceGoals_Tenant_Period_Key'
      AND object_id = OBJECT_ID('FinanceGoals')
)
CREATE INDEX IX_FinanceGoals_Tenant_Period_Key
ON FinanceGoals (TenantId, [Year], [Month], [Key])
INCLUDE (UnitId, TargetAmount, [Label], UpdatedAtUtc);
