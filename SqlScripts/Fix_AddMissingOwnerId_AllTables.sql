-- ==========================================================================================
-- SCRIPT: Fix_AddMissingOwnerId_AllTables.sql
-- PURPOSE: Fixes 'Invalid column name OwnerId' across all affected tables in the database.
-- SAFE TO RUN: Idempotent (checks IF COL_LENGTH IS NULL before adding columns & constraints).
-- ==========================================================================================
SET NOCOUNT ON;
GO

PRINT '----------------------------------------------------------------------';
PRINT 'CHECKING AND ADDING OwnerId TO ALL OPERATIONAL TABLES...';
PRINT '----------------------------------------------------------------------';
GO

-- 1. Table: TaxDeclarations
IF COL_LENGTH(N'[dbo].[TaxDeclarations]', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[TaxDeclarations] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
    PRINT 'Added [OwnerId] to [dbo].[TaxDeclarations].';
END
ELSE
BEGIN
    PRINT '[OwnerId] already exists in [dbo].[TaxDeclarations].';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaxDeclarations_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[TaxDeclarations]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TaxDeclarations_OwnerId] ON [dbo].[TaxDeclarations] ([OwnerId]);
    PRINT 'Created index IX_TaxDeclarations_OwnerId.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaxDeclarations_TaxpayerOwners_OwnerId' AND parent_object_id = OBJECT_ID(N'[dbo].[TaxDeclarations]'))
BEGIN
    ALTER TABLE [dbo].[TaxDeclarations] WITH CHECK ADD CONSTRAINT [FK_TaxDeclarations_TaxpayerOwners_OwnerId]
        FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TaxpayerOwners] ([Id]) ON DELETE SET NULL;
    PRINT 'Created foreign key FK_TaxDeclarations_TaxpayerOwners_OwnerId.';
END
GO

-- 2. Table: PropertyAssessmentRequests
IF COL_LENGTH(N'[dbo].[PropertyAssessmentRequests]', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[PropertyAssessmentRequests] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
    PRINT 'Added [OwnerId] to [dbo].[PropertyAssessmentRequests].';
END
ELSE
BEGIN
    PRINT '[OwnerId] already exists in [dbo].[PropertyAssessmentRequests].';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PropertyAssessmentRequests_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[PropertyAssessmentRequests]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PropertyAssessmentRequests_OwnerId] ON [dbo].[PropertyAssessmentRequests] ([OwnerId]);
    PRINT 'Created index IX_PropertyAssessmentRequests_OwnerId.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PropertyAssessmentRequests_TaxpayerOwners_OwnerId' AND parent_object_id = OBJECT_ID(N'[dbo].[PropertyAssessmentRequests]'))
BEGIN
    ALTER TABLE [dbo].[PropertyAssessmentRequests] WITH CHECK ADD CONSTRAINT [FK_PropertyAssessmentRequests_TaxpayerOwners_OwnerId]
        FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TaxpayerOwners] ([Id]) ON DELETE SET NULL;
    PRINT 'Created foreign key FK_PropertyAssessmentRequests_TaxpayerOwners_OwnerId.';
END
GO

-- 3. Table: RequestForInspections
IF COL_LENGTH(N'[dbo].[RequestForInspections]', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[RequestForInspections] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
    PRINT 'Added [OwnerId] to [dbo].[RequestForInspections].';
END
ELSE
BEGIN
    PRINT '[OwnerId] already exists in [dbo].[RequestForInspections].';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RequestForInspections_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[RequestForInspections]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RequestForInspections_OwnerId] ON [dbo].[RequestForInspections] ([OwnerId]);
    PRINT 'Created index IX_RequestForInspections_OwnerId.';
END
GO

-- 4. Table: TaxMappingRequests
IF COL_LENGTH(N'[dbo].[TaxMappingRequests]', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[TaxMappingRequests] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
    PRINT 'Added [OwnerId] to [dbo].[TaxMappingRequests].';
END
ELSE
BEGIN
    PRINT '[OwnerId] already exists in [dbo].[TaxMappingRequests].';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaxMappingRequests_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[TaxMappingRequests]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TaxMappingRequests_OwnerId] ON [dbo].[TaxMappingRequests] ([OwnerId]);
    PRINT 'Created index IX_TaxMappingRequests_OwnerId.';
END
GO

PRINT '----------------------------------------------------------------------';
PRINT 'ALL OwnerId COLUMNS AND CONSTRAINTS ARE SUCCESSFULLY APPLIED!';
PRINT '----------------------------------------------------------------------';
GO
