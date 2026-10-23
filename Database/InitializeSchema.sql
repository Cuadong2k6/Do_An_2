:On Error exit

-- Run through sqlcmd. Database creation is separate because CREATE DATABASE
-- cannot run inside a transaction.
USE DoAn2;
GO
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.schema_init_complete', N'U') IS NOT NULL
    THROW 51000, N'Database schema đã được khởi tạo. Không chạy lại InitializeSchema.sql.', 1;

BEGIN TRANSACTION;
GO

:r $(ScriptDir)/01_CreateTables.sql
:r $(ScriptDir)/02_StoredProcedures.sql
:r $(ScriptDir)/03_SeedData.sql
:r $(ScriptDir)/05_MigrateMd5ToBcrypt.sql
:r $(ScriptDir)/06_AddNhatky.sql
:r $(ScriptDir)/07_AddIndexes.sql
:r $(ScriptDir)/08_SeedBulkData.sql
:r $(ScriptDir)/09_PasswordReset.sql
:r $(ScriptDir)/10_EmailOtp.sql
:r $(ScriptDir)/11_GoogleReaderAuth.sql
:r $(ScriptDir)/12_GoogleReaderAuthFlow.sql

CREATE TABLE dbo.schema_init_complete
(
    initialized_at DATETIME2(0) NOT NULL
        CONSTRAINT DF_schema_init_complete_initialized_at DEFAULT SYSUTCDATETIME()
);

COMMIT TRANSACTION;
GO
