-- Schema mínimo do Data Warehouse para piloto (EduFlow_DW).
USE EduFlow_DW;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'dw')
    EXEC(N'CREATE SCHEMA dw');
GO

IF OBJECT_ID(N'dw.DimTempo', N'U') IS NULL
CREATE TABLE dw.DimTempo (
    DateKey INT NOT NULL PRIMARY KEY,
    [Date] DATE NOT NULL,
    [Year] INT NOT NULL,
    [Month] INT NOT NULL,
    [Quarter] INT NOT NULL
);
GO

IF OBJECT_ID(N'dw.DimUnidade', N'U') IS NULL
CREATE TABLE dw.DimUnidade (
    UnitKey INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    UnitId UNIQUEIDENTIFIER NOT NULL,
    UnitCode NVARCHAR(64) NOT NULL,
    UnitName NVARCHAR(256) NOT NULL
);
GO

IF OBJECT_ID(N'dw.DimAluno', N'U') IS NULL
CREATE TABLE dw.DimAluno (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    EnrollmentCode NVARCHAR(64) NOT NULL,
    UnitKey INT NOT NULL,
    CourseKey INT NULL,
    Status NVARCHAR(32) NOT NULL,
    CONSTRAINT PK_DimAluno PRIMARY KEY (TenantId, EnrollmentCode)
);
GO

IF OBJECT_ID(N'dw.FactFinanceiro', N'U') IS NULL
CREATE TABLE dw.FactFinanceiro (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    DateKey INT NOT NULL,
    UnitKey INT NOT NULL,
    EnrollmentCode NVARCHAR(64) NOT NULL,
    DebtAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) NOT NULL,
    PaymentStatus NVARCHAR(32) NOT NULL,
    IsOverdue BIT NOT NULL
);
GO

IF OBJECT_ID(N'dw.FactInadimplencia', N'U') IS NULL
CREATE TABLE dw.FactInadimplencia (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    DateKey INT NOT NULL,
    UnitKey INT NOT NULL,
    DelinquencyRate DECIMAL(9,4) NOT NULL,
    DebtAmount DECIMAL(18,2) NOT NULL,
    OverdueCount INT NOT NULL
);
GO

IF OBJECT_ID(N'dw.FactEvasao', N'U') IS NULL
CREATE TABLE dw.FactEvasao (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    DateKey INT NOT NULL,
    UnitKey INT NOT NULL,
    ChurnRate DECIMAL(9,4) NOT NULL,
    ChurnedStudents INT NOT NULL,
    ActiveStudents INT NOT NULL
);
GO
