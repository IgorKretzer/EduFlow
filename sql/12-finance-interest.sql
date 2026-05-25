-- Juros/multa capturados do ERP quando disponíveis.
-- Descontos continuam fora do modelo analítico inicial por variarem muito por operação.

IF COL_LENGTH('CanonicalFinancials', 'InterestAmount') IS NULL
    ALTER TABLE CanonicalFinancials ADD InterestAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

IF COL_LENGTH('dw.FactFinanceiro', 'InterestAmount') IS NULL
    ALTER TABLE dw.FactFinanceiro ADD InterestAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CanonicalFinancials_Tenant_Interest'
      AND object_id = OBJECT_ID('CanonicalFinancials')
)
CREATE INDEX IX_CanonicalFinancials_Tenant_Interest
ON CanonicalFinancials (TenantId, InterestAmount)
INCLUDE (DueDate, CashFlowDate, FlowDirection, UnitId);
