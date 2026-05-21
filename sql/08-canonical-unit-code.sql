-- EduFlow_Staging: coluna CodigoUnidade no financeiro canônico
USE EduFlow_Staging;
GO

IF COL_LENGTH('dbo.CanonicalFinancials', 'UnitCode') IS NULL
BEGIN
    ALTER TABLE dbo.CanonicalFinancials ADD UnitCode NVARCHAR(64) NOT NULL
        CONSTRAINT DF_CanonicalFinancials_UnitCode DEFAULT ('');
END
GO
