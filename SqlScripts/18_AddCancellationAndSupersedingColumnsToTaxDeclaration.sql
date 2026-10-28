-- =====================================================================================
-- Migration: Add Cancellation & Superseding Explicit Columns to TaxDeclarations
-- Date: 2026-10-28
-- Description: Adds IsCancelledAndSuperseded, CancellationReason, DateCancelled,
--              SupersededByTdns, and CancelledByRequestId columns to [TaxDeclarations].
-- =====================================================================================

IF COL_LENGTH('TaxDeclarations', 'IsCancelledAndSuperseded') IS NULL
BEGIN
    ALTER TABLE [TaxDeclarations] 
    ADD [IsCancelledAndSuperseded] BIT NOT NULL CONSTRAINT DF_TaxDeclarations_IsCancelledAndSuperseded DEFAULT (0);
    PRINT 'Added [IsCancelledAndSuperseded] column to [TaxDeclarations].';
END
GO

IF COL_LENGTH('TaxDeclarations', 'CancellationReason') IS NULL
BEGIN
    ALTER TABLE [TaxDeclarations] 
    ADD [CancellationReason] NVARCHAR(500) NULL;
    PRINT 'Added [CancellationReason] column to [TaxDeclarations].';
END
GO

IF COL_LENGTH('TaxDeclarations', 'DateCancelled') IS NULL
BEGIN
    ALTER TABLE [TaxDeclarations] 
    ADD [DateCancelled] DATETIME2 NULL;
    PRINT 'Added [DateCancelled] column to [TaxDeclarations].';
END
GO

IF COL_LENGTH('TaxDeclarations', 'SupersededByTdns') IS NULL
BEGIN
    ALTER TABLE [TaxDeclarations] 
    ADD [SupersededByTdns] NVARCHAR(MAX) NULL;
    PRINT 'Added [SupersededByTdns] column to [TaxDeclarations].';
END
GO

IF COL_LENGTH('TaxDeclarations', 'CancelledByRequestId') IS NULL
BEGIN
    ALTER TABLE [TaxDeclarations] 
    ADD [CancelledByRequestId] UNIQUEIDENTIFIER NULL;
    PRINT 'Added [CancelledByRequestId] column to [TaxDeclarations].';
END
GO

-- Back-fill any existing Cancelled records based on Status = 3 or Memoranda
UPDATE [TaxDeclarations]
SET [IsCancelledAndSuperseded] = 1,
    [CancellationReason] = 'Subdivision (SD) · Segregation of Land'
WHERE [Status] = 3 
  AND [IsCancelledAndSuperseded] = 0;
GO

PRINT 'Cancellation & Superseding columns verification completed successfully.';
