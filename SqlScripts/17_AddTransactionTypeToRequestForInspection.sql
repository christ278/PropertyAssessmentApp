-- =========================================================================================
-- Script 17: Add TransactionTypeName to RequestForInspections table
-- =========================================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[RequestForInspections]') 
    AND name = 'TransactionTypeName'
)
BEGIN
    ALTER TABLE [dbo].[RequestForInspections]
    ADD [TransactionTypeName] NVARCHAR(255) NULL;
    PRINT 'Added TransactionTypeName column to [dbo].[RequestForInspections].';
END
ELSE
BEGIN
    PRINT 'Column TransactionTypeName already exists on [dbo].[RequestForInspections].';
END
GO
