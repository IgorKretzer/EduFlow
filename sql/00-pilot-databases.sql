-- Cria bancos do piloto (executar como sa antes do schema EF/DW).
IF DB_ID(N'EduFlow_Staging') IS NULL
    CREATE DATABASE EduFlow_Staging;
GO

IF DB_ID(N'EduFlow_DW') IS NULL
    CREATE DATABASE EduFlow_DW;
GO
