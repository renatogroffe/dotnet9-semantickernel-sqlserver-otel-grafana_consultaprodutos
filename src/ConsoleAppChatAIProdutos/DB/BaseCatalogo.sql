USE master
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BaseCatalogo')
BEGIN
    CREATE DATABASE BaseCatalogo;
END
GO

USE BaseCatalogo;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Produtos')
BEGIN
    CREATE TABLE Produtos (
        Id INT IDENTITY(1,1) NOT NULL,
        CodigoBarras VARCHAR(13) NOT NULL,
        Nome VARCHAR(100) NOT NULL,
        Preco NUMERIC(19, 4) NOT NULL,
        CONSTRAINT PK_Produtos PRIMARY KEY (Id)
    );
END
GO

USE master
GO