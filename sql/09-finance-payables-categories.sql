-- Colunas financeiras (receber/pagar, categoria, fluxo de caixa)
IF COL_LENGTH('CanonicalFinancials', 'FlowDirection') IS NULL
    ALTER TABLE CanonicalFinancials ADD FlowDirection NVARCHAR(16) NOT NULL DEFAULT 'receivable';

IF COL_LENGTH('CanonicalFinancials', 'CategoryId') IS NULL
    ALTER TABLE CanonicalFinancials ADD CategoryId NVARCHAR(64) NOT NULL DEFAULT '';

IF COL_LENGTH('CanonicalFinancials', 'CategoryName') IS NULL
    ALTER TABLE CanonicalFinancials ADD CategoryName NVARCHAR(256) NOT NULL DEFAULT '';

IF COL_LENGTH('CanonicalFinancials', 'PaymentMethodLabel') IS NULL
    ALTER TABLE CanonicalFinancials ADD PaymentMethodLabel NVARCHAR(128) NOT NULL DEFAULT '';

IF COL_LENGTH('CanonicalFinancials', 'CounterpartyName') IS NULL
    ALTER TABLE CanonicalFinancials ADD CounterpartyName NVARCHAR(256) NOT NULL DEFAULT '';

IF COL_LENGTH('CanonicalFinancials', 'CashFlowDate') IS NULL
    ALTER TABLE CanonicalFinancials ADD CashFlowDate DATE NOT NULL DEFAULT '2000-01-01';

UPDATE CanonicalFinancials SET CashFlowDate = DueDate WHERE CashFlowDate = '2000-01-01';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CanonicalCategories')
BEGIN
    CREATE TABLE CanonicalCategories (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        ExternalId NVARCHAR(64) NOT NULL,
        Name NVARCHAR(256) NOT NULL,
        SyncedAt DATETIME2 NOT NULL,
        CONSTRAINT UQ_CanonicalCategories_Tenant_External UNIQUE (TenantId, ExternalId)
    );
END
