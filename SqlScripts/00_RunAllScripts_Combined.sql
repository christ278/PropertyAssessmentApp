-- ==========================================================================================
-- SCRIPT: 00_RunAllScripts_Combined.sql
-- DESCRIPTION: All 21 SQL scripts combined into a single standalone executable script.
--              Does NOT require SQLCMD Mode or file path references.
--              Directly executable in SSMS, Azure Query Editor, DBeaver, or sqlcmd.
-- ==========================================================================================
SET NOCOUNT ON;
GO


-- ==========================================================================================
-- SECTION: 01_CreateTables.sql
-- ==========================================================================================
PRINT 'Executing section: 01_CreateTables.sql...';
GO
-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 01: DATABASE SCHEMA & TABLE CREATION
-- Target Database: Microsoft SQL Server (T-SQL) / FairTrackDB
-- =============================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

-- 1. USER ACCOUNTS TABLE
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserAccounts')
BEGIN
    CREATE TABLE [dbo].[UserAccounts] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Username] NVARCHAR(50) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [FullName] NVARCHAR(150) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [EmployeeNo] NVARCHAR(50) NOT NULL,
        [Role] INT NOT NULL DEFAULT 5, -- 0: SystemAdmin, 1: CityAssessor, 2: AssessmentOfficer, 3: Appraiser, 4: TaxMapper, 5: AssessmentClerk
        [DivisionId] INT NULL,
        [WorkflowRoleId] INT NULL,
        [WorkflowRoleName] NVARCHAR(150) NULL,
        [Department] NVARCHAR(150) NOT NULL DEFAULT 'Real Property Assessment Division',
        [ContactNo] NVARCHAR(50) NOT NULL DEFAULT '',
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastLoginDate] DATETIME2 NULL
    );

    CREATE INDEX [IX_UserAccounts_Username] ON [dbo].[UserAccounts]([Username]);
    PRINT 'Created table [dbo].[UserAccounts].';
END
GO

-- 2. TAXPAYER OWNERS TABLE
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TaxpayerOwners')
BEGIN
    CREATE TABLE [dbo].[TaxpayerOwners] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [OwnerType] INT NOT NULL DEFAULT 0, -- 0: Individual, 1: Corporation, 2: Government
        [FirstName] NVARCHAR(100) NULL,
        [MiddleName] NVARCHAR(100) NULL,
        [LastName] NVARCHAR(100) NULL,
        [CorporateName] NVARCHAR(200) NULL,
        [TaxIdentNo] NVARCHAR(50) NOT NULL,
        [RptinNumber] NVARCHAR(50) NULL,
        [ContactNo] NVARCHAR(50) NOT NULL DEFAULT '',
        [Email] NVARCHAR(150) NOT NULL DEFAULT '',
        [ProvinceName] NVARCHAR(100) NOT NULL DEFAULT '',
        [CityMunicipalityName] NVARCHAR(100) NOT NULL DEFAULT '',
        [BarangayName] NVARCHAR(100) NOT NULL DEFAULT '',
        [Address] NVARCHAR(MAX) NOT NULL DEFAULT '',
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    PRINT 'Created table [dbo].[TaxpayerOwners].';
END
GO

-- 3. TAX DECLARATIONS TABLE (NORMALIZED CORE MASTER)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TaxDeclarations')
BEGIN
    CREATE TABLE [dbo].[TaxDeclarations] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Kind] INT NOT NULL DEFAULT 0, -- 0: Land, 1: Building, 2: Machinery
        [TaxDeclarationNumber] NVARCHAR(50) NOT NULL UNIQUE,
        [TransactionCode] NVARCHAR(50) NOT NULL,
        [PropertyIndexNumber] NVARCHAR(50) NOT NULL,
        [OwnerId] UNIQUEIDENTIFIER NULL,
        [OwnerName] NVARCHAR(200) NOT NULL,
        [OwnerTin] NVARCHAR(50) NOT NULL,
        [OwnerPhone] NVARCHAR(50) NOT NULL,
        [OwnerEmail] NVARCHAR(100) NOT NULL,
        [OwnerAddress] NVARCHAR(300) NOT NULL,
        [OwnerSex] NVARCHAR(20) NOT NULL,
        [AdminPossessorName] NVARCHAR(200) NULL,
        [AdminPossessorSex] NVARCHAR(20) NULL,
        [AdminPossessorAddress] NVARCHAR(300) NULL,
        [AdminPossessorTin] NVARCHAR(50) NULL,
        [AdminPossessorPhone] NVARCHAR(50) NULL,
        [AdminPossessorEmail] NVARCHAR(100) NULL,
        [AppraisedByName] NVARCHAR(150) NOT NULL DEFAULT '',
        [DateAppraised] DATETIME2 NULL,
        [AssessedByName] NVARCHAR(150) NOT NULL DEFAULT '',
        [DateAssessed] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [RecommendingApprovalName] NVARCHAR(150) NOT NULL DEFAULT '',
        [DateRecommended] DATETIME2 NULL,
        [Memoranda] NVARCHAR(MAX) NOT NULL DEFAULT '',
        [ApprovedByName] NVARCHAR(150) NOT NULL DEFAULT '',
        [DateApproved] DATETIME2 NULL,
        [EntryInRollName] NVARCHAR(150) NOT NULL DEFAULT '',
        [EntryInRollPosition] NVARCHAR(100) NOT NULL DEFAULT '',
        [EntryInRollDate] DATETIME2 NULL,
        [EncodedByName] NVARCHAR(150) NOT NULL DEFAULT '',
        [DateEncoded] DATETIME2 NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [FloorPlanSketchPath] NVARCHAR(MAX) NULL,
        [OwnerCivilStatus] NVARCHAR(50) NULL
    );

    CREATE INDEX [IX_TaxDeclarations_TDN] ON [dbo].[TaxDeclarations]([TaxDeclarationNumber]);
    CREATE INDEX [IX_TaxDeclarations_PIN] ON [dbo].[TaxDeclarations]([PropertyIndexNumber]);
    CREATE INDEX [IX_TaxDeclarations_OwnerId] ON [dbo].[TaxDeclarations]([OwnerId]);
    PRINT 'Created table [dbo].[TaxDeclarations].';
END
GO

-- 4. ORDER OF PAYMENT RATES TABLE
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrderPayments')
BEGIN
    CREATE TABLE [dbo].[OrderPayments] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Code] NVARCHAR(50) NOT NULL UNIQUE,
        [Particular] NVARCHAR(MAX) NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    PRINT 'Created table [dbo].[OrderPayments].';
END
GO

-- 5. TAX MAPPING REQUESTS TABLE
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TaxMappingRequests')
BEGIN
    CREATE TABLE [dbo].[TaxMappingRequests] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [RequestNumber] NVARCHAR(50) NOT NULL UNIQUE,
        [RequestDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [TaxDeclarationId] UNIQUEIDENTIFIER NULL,
        [OwnerId] UNIQUEIDENTIFIER NULL,
        [TaxDeclarationNumber] NVARCHAR(50) NULL,
        [PropertyIndexNumber] NVARCHAR(50) NULL,
        [ApplicantName] NVARCHAR(200) NOT NULL,
        [OwnerContactNo] NVARCHAR(50) NULL,
        [RequesterName] NVARCHAR(200) NULL,
        [ORNumber] NVARCHAR(50) NULL,
        [ORDateIssued] DATETIME2 NULL,
        [Purposes] NVARCHAR(MAX) NULL,
        [Purpose] INT NOT NULL DEFAULT 0,
        [ReqIdOfOwner] BIT NOT NULL DEFAULT 0,
        [ReqAuthorizationLetter] BIT NOT NULL DEFAULT 0,
        [ReqCertIndigency] BIT NOT NULL DEFAULT 0,
        [ReqIdOfRepresentative] BIT NOT NULL DEFAULT 0,
        [ReqDeedOfConveyance] BIT NOT NULL DEFAULT 0,
        [CertTaxDeclaration] BIT NOT NULL DEFAULT 0,
        [CertRecordsVerification] BIT NOT NULL DEFAULT 0,
        [CertTaxDecTimeOfDeath] BIT NOT NULL DEFAULT 0,
        [CertAnnotationBailBond] BIT NOT NULL DEFAULT 0,
        [CertPropertyHistory] BIT NOT NULL DEFAULT 0,
        [CertAnnotationMortgages] BIT NOT NULL DEFAULT 0,
        [CertNoImprovement] BIT NOT NULL DEFAULT 0,
        [CertCancellationAnnotation] BIT NOT NULL DEFAULT 0,
        [CertSupportingDocuments] BIT NOT NULL DEFAULT 0,
        [CertRoadRightOfWay] BIT NOT NULL DEFAULT 0,
        [CertPropertyIdVerification] BIT NOT NULL DEFAULT 0,
        [CertNoRoadRightOfWay] BIT NOT NULL DEFAULT 0,
        [CertFieldInspection] BIT NOT NULL DEFAULT 0,
        [CertTaxMap] BIT NOT NULL DEFAULT 0,
        [CertOthers] BIT NOT NULL DEFAULT 0,
        [CertImageMap] BIT NOT NULL DEFAULT 0,
        [CertOthersSpecification] NVARCHAR(MAX) NULL,
        [ProvinceId] UNIQUEIDENTIFIER NULL,
        [CityMunicipalityId] UNIQUEIDENTIFIER NULL,
        [BarangayId] UNIQUEIDENTIFIER NULL,
        [StreetAddress] NVARCHAR(MAX) NULL,
        [ParcelAreaSqm] DECIMAL(18,2) NULL,
        [AttachmentFileName] NVARCHAR(255) NULL,
        [AttachmentFileSize] NVARCHAR(50) NULL,
        [Remarks] NVARCHAR(MAX) NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [AssignedStaff] NVARCHAR(150) NULL,
        [RequestedDocumentsJson] NVARCHAR(MAX) NULL,
        [TotalFeesAmount] DECIMAL(18,2) NOT NULL DEFAULT 0.00
    );

    CREATE INDEX [IX_TaxMappingRequests_ReqNo] ON [dbo].[TaxMappingRequests]([RequestNumber]);
    CREATE INDEX [IX_TaxMappingRequests_OwnerId] ON [dbo].[TaxMappingRequests]([OwnerId]);
    PRINT 'Created table [dbo].[TaxMappingRequests].';
END
GO

-- 6. FILE STORAGE LOCATION SETUP TABLE (CAST REFERENCE)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileLocation')
BEGIN
    CREATE TABLE [dbo].[FileLocation] (
        [FileLocationId] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(150) NOT NULL,
        [Location] NVARCHAR(500) NOT NULL
    );
    PRINT 'Created table [dbo].[FileLocation].';
END
GO

GO


-- ==========================================================================================
-- SECTION: 04_SeedFileLocationDefaultSetup.sql
-- ==========================================================================================
PRINT 'Executing section: 04_SeedFileLocationDefaultSetup.sql...';
GO
-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 04: FILE LOCATION SETUP & SEED SCRIPT (CAST REFERENCE SYSTEM)
-- Reference Source: CAST_v1 / SqlScript / seed_filelocation.sql
-- =============================================================================

-- 1. ENSURE FILE LOCATION TABLE EXISTS
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FileLocation')
BEGIN
    CREATE TABLE [dbo].[FileLocation] (
        [FileLocationId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(150) NOT NULL,
        [Location] NVARCHAR(500) NOT NULL
    );
END
GO

-- 2. ENABLE IDENTITY INSERT TO PRESERVE CAST DEFAULT LOCATION IDS
SET IDENTITY_INSERT [dbo].[FileLocation] ON;
GO

-- ID 1: Profile (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 1)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (1, N'Profile', N'\\192.168.0.182\mapping_system\Attachments\UserProfile');
END

-- ID 2: Attachment (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 2)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (2, N'Attachment', N'\\192.168.0.182\mapping_system\Attachments\Attachment _for_Testing');
END

-- ID 3: Sketch (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 3)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (3, N'Sketch', N'\\192.168.0.182\mapping_system\Attachments\Skech');
END

-- ID 4: InspectionAttachment (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 4)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (4, N'InspectionAttachment', N'\\192.168.0.182\mapping_system\Attachments\Inspection');
END

-- ID 5: SketchLand (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 5)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (5, N'SketchLand', N'\\192.168.0.182\mapping_system\Attachments\Inspection');
END

-- ID 6: PropertyAssessment (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 6)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (6, N'PropertyAssessment', N'\\192.168.0.182\mapping_system\Attachments\PropertyAssessment');
END

-- ID 11: Profile (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 11)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (11, N'Profile', N'~/UploadFile/UserProfile');
END

-- ID 12: Attachment (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 12)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (12, N'Attachment', N'~/UploadFile/Attachment');
END

-- ID 13: Sketch (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 13)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (13, N'Sketch', N'~/UploadFile/Sketch');
END

-- ID 14: InspectionAttachmentNetwork (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 14)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (14, N'InspectionAttachmentNetwork', N'~/UploadFile/Inspection');
END

-- ID 15: SketchLandBase (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 15)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (15, N'SketchLandBase', N'~/UploadFile/Land');
END

-- ID 16: PropertyAssessmentBase (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 16)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (16, N'PropertyAssessmentBase', N'~/UploadFile/PropertyAssessment');
END
GO

-- 3. DISABLE IDENTITY INSERT
SET IDENTITY_INSERT [dbo].[FileLocation] OFF;
GO

-- 4. VERIFICATION QUERY
SELECT [FileLocationId], [Name], [Location] 
FROM [dbo].[FileLocation] 
ORDER BY [FileLocationId];
GO

GO


-- ==========================================================================================
-- SECTION: 05_CreateInspectionAndAssessmentRequestTables.sql
-- ==========================================================================================
PRINT 'Executing section: 05_CreateInspectionAndAssessmentRequestTables.sql...';
GO
-- =============================================================================
-- SQL SCRIPT STEP 05: CREATE REQUEST FOR INSPECTION & PROPERTY ASSESSMENT TABLES
-- Target Database: (localdb)\mssqllocaldb / PropertyAssessmentAppDb
-- Reference: CAST System (RequestForInspection & PropertyAssessment workflows)
-- =============================================================================
-- USE []; -- Run against active connected database
GO

-- 1. REQUEST FOR INSPECTION TABLE (10-STAGE INSPECTION WORKFLOW)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RequestForInspections')
BEGIN
    CREATE TABLE RequestForInspections (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TrackingNumber NVARCHAR(100) NOT NULL,
        TransactionCode NVARCHAR(50) NULL,
        TransactionNo NVARCHAR(100) NULL,

        -- Inspection Purpose / Reasons
        IsDepreciation BIT NOT NULL DEFAULT 0,
        IsReassessment BIT NOT NULL DEFAULT 0,
        IsCancelled BIT NOT NULL DEFAULT 0,
        IsOther BIT NOT NULL DEFAULT 0,
        OtherPurposeDetails NVARCHAR(500) NULL,

        -- Reference Property
        TaxDeclarationId UNIQUEIDENTIFIER NULL,
        TaxDeclarationNumber NVARCHAR(100) NOT NULL DEFAULT '',
        PropertyIndexNumber NVARCHAR(100) NOT NULL DEFAULT '',
        OwnerName NVARCHAR(200) NOT NULL DEFAULT '',
        Kind NVARCHAR(50) NOT NULL DEFAULT 'Land',
        LotNo NVARCHAR(100) NOT NULL DEFAULT 'N/A',
        PropertyLocation NVARCHAR(300) NOT NULL DEFAULT 'N/A',
        OwnerId UNIQUEIDENTIFIER NULL,

        -- Contact & Representative
        ContactPerson NVARCHAR(200) NULL,
        ContactNumber NVARCHAR(100) NULL,
        EmailAddress NVARCHAR(150) NULL,
        Representative NVARCHAR(200) NULL,

        -- Billing & Payment
        RequestedDocumentsJson NVARCHAR(MAX) NULL,
        TotalFeesAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        ORNumber NVARCHAR(100) NULL,
        ORDateIssued DATETIME2 NULL,

        -- File Attachments
        AttachmentFileName NVARCHAR(MAX) NULL,
        AttachmentFileSize NVARCHAR(MAX) NULL,
        PhysicalStoragePath NVARCHAR(MAX) NULL,
        WebRelativePath NVARCHAR(MAX) NULL,

        -- Workflow Status & Tracking
        Status INT NOT NULL DEFAULT 1, -- 0: Deferred, 1: Receiving, 2: Replotting, 3: Inspection, 4: CheckAndReview, 5: Repinning, 6: RecommendingApproval, 7: ValuationAndExamination, 8: Approval, 9: Printing, 10: Releasing
        AssignedStaff NVARCHAR(150) NULL,
        Remarks NVARCHAR(MAX) NULL,
        RequestDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_RequestForInspections_Tracking ON RequestForInspections(TrackingNumber);
    CREATE INDEX IX_RequestForInspections_Status ON RequestForInspections(Status);
END
GO

-- 2. PROPERTY ASSESSMENT REQUESTS TABLE (RPA & FAAS PARTITIONING)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PropertyAssessmentRequests')
BEGIN
    CREATE TABLE PropertyAssessmentRequests (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TrackingNumber NVARCHAR(100) NOT NULL,
        TransactionTypeCode NVARCHAR(50) NOT NULL DEFAULT 'CS',
        TransactionTypeName NVARCHAR(150) NOT NULL DEFAULT 'Transfer of Ownership',
        Kind NVARCHAR(50) NOT NULL DEFAULT 'Land',
        Partitions INT NOT NULL DEFAULT 1,

        -- Reference Property & Previous Owner
        RefTaxDeclarationId UNIQUEIDENTIFIER NULL,
        PrevTDN NVARCHAR(100) NOT NULL DEFAULT '',
        PrevPIN NVARCHAR(100) NOT NULL DEFAULT '',
        PrevOwner NVARCHAR(200) NOT NULL DEFAULT '',
        LotNo NVARCHAR(100) NOT NULL DEFAULT 'N/A',
        PropertyLocation NVARCHAR(300) NOT NULL DEFAULT 'N/A',
        OwnerId UNIQUEIDENTIFIER NULL,

        -- New Owner / Declarant Information
        NewOwnerName NVARCHAR(200) NOT NULL,
        Address NVARCHAR(300) NOT NULL DEFAULT '',
        OwnerTIN NVARCHAR(50) NULL,
        ContactNo NVARCHAR(100) NULL,
        EmailAddress NVARCHAR(150) NULL,
        Requestor NVARCHAR(200) NULL,

        -- Documents Submitted Checklist
        DocTitleTctCloa BIT NOT NULL DEFAULT 0,
        DocDeedOfSaleConveyance BIT NOT NULL DEFAULT 0,
        DocCarBir BIT NOT NULL DEFAULT 0,
        DocTaxClearance BIT NOT NULL DEFAULT 0,
        DocTransferTaxReceipt BIT NOT NULL DEFAULT 0,
        DocApprovedSurveyPlan BIT NOT NULL DEFAULT 0,
        DocCadastralMap BIT NOT NULL DEFAULT 0,
        DocDenrCert BIT NOT NULL DEFAULT 0,
        DocBuildingPermit BIT NOT NULL DEFAULT 0,
        DocCertCompletion BIT NOT NULL DEFAULT 0,
        DocCertOccupancy BIT NOT NULL DEFAULT 0,
        DocInvoiceReceipts BIT NOT NULL DEFAULT 0,
        DocInstallationCostBreakdown BIT NOT NULL DEFAULT 0,
        DocSpecsManual BIT NOT NULL DEFAULT 0,
        DocSpaAuthLetter BIT NOT NULL DEFAULT 0,
        DocValidIdOwnerRep BIT NOT NULL DEFAULT 0,
        DocOthers BIT NOT NULL DEFAULT 0,
        DocOthersSpecify NVARCHAR(500) NULL,

        -- Billing & Payment
        RequestedDocumentsJson NVARCHAR(MAX) NULL,
        TotalFeesAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        ORNumber NVARCHAR(100) NULL,
        ORDateIssued DATETIME2 NULL,

        -- File Attachments
        AttachmentFileName NVARCHAR(MAX) NULL,
        AttachmentFileSize NVARCHAR(MAX) NULL,
        PhysicalStoragePath NVARCHAR(MAX) NULL,
        WebRelativePath NVARCHAR(MAX) NULL,

        -- Workflow Status & Tracking
        Status INT NOT NULL DEFAULT 1, -- 0: Pending, 1: Receiving, 2: Replotting, 3: Valuation, 4: Review, 5: Approval, 6: Approved, 7: Deferred
        AssignedAppraiser NVARCHAR(150) NULL,
        Remarks NVARCHAR(MAX) NULL,
        RequestDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_PropertyAssessmentRequests_Tracking ON PropertyAssessmentRequests(TrackingNumber);
    CREATE INDEX IX_PropertyAssessmentRequests_Status ON PropertyAssessmentRequests(Status);
    CREATE INDEX IX_PropertyAssessmentRequests_OwnerId ON PropertyAssessmentRequests(OwnerId);
END
GO

GO


-- ==========================================================================================
-- SECTION: 17_AddTransactionTypeToRequestForInspection.sql
-- ==========================================================================================
PRINT 'Executing section: 17_AddTransactionTypeToRequestForInspection.sql...';
GO
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

GO


-- ==========================================================================================
-- SECTION: 18_AddCancellationAndSupersedingColumnsToTaxDeclaration.sql
-- ==========================================================================================
PRINT 'Executing section: 18_AddCancellationAndSupersedingColumnsToTaxDeclaration.sql...';
GO
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

GO


-- ==========================================================================================
-- SECTION: 02_SeedDefaultAdmin.sql
-- ==========================================================================================
PRINT 'Executing section: 02_SeedDefaultAdmin.sql...';
GO
-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 02: DEFAULT ADMIN ACCOUNT SEEDING (T-SQL / SQL SERVER)
-- Target Account: Username: 'admin' | Password: 'P@ssword2026!' | Role: 0 ('SystemAdmin')
-- =============================================================================

SET NOCOUNT ON;

-- INSERT OR UPDATE DEFAULT SYSTEM ADMINISTRATOR ACCOUNT
-- Password is 'P@ssword2026!' (ASP.NET Core Identity PasswordHasher PBKDF2 hash)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'Password')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] DROP COLUMN [Password];
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[UserAccounts] (
        [Id],
        [Username],
        [PasswordHash],
        [FullName],
        [Email],
        [EmployeeNo],
        [Role],
        [Department],
        [ContactNo],
        [IsActive],
        [CreatedDate],
        [LastLoginDate]
    )
    VALUES (
        '70000000-0000-0000-0000-000000000000',
        'admin',
        'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==', -- Hash for password 'P@ssword2026!'
        'System Administrator',
        'admin@gensan.gov.ph',
        'EMP-ADMIN-001',
        0, -- 0: SystemAdmin Role
        'City Assessor Executive Office',
        '0917-000-0000',
        1, -- 1: Bit TRUE in T-SQL
        GETDATE(),
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [dbo].[UserAccounts]
    SET [PasswordHash] = 'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==',
        [FullName] = 'System Administrator',
        [Role] = 0,
        [IsActive] = 1,
        [LastLoginDate] = GETDATE()
    WHERE [Username] = 'admin';
END;

-- VERIFICATION QUERY FOR DEFAULT ADMIN
SELECT [Id], [Username], [FullName], [Email], [EmployeeNo], [Role], [IsActive] 
FROM [dbo].[UserAccounts] 
WHERE [Username] = 'admin';

GO


-- ==========================================================================================
-- SECTION: 12_SeedDefaultModules.sql
-- ==========================================================================================
PRINT 'Executing section: 12_SeedDefaultModules.sql...';
GO
-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 12_SeedDefaultModules.sql
-- DESCRIPTION: Seeds default system modules into [dbo].[SystemModule].
--              Ensures table existence, applies idempotent upsert (MERGE) 
--              with deterministic ModuleId values, and preserves sorting/routing.
-- DATABASE: FairTrackDB
-- =============================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Default System Modules Seeding...';
PRINT '=========================================================================';

-- 1. Ensure SystemModule table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SystemModule')
BEGIN
    CREATE TABLE [dbo].[SystemModule] (
        [ModuleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ModuleCode] NVARCHAR(50) NOT NULL UNIQUE,
        [ModuleName] NVARCHAR(100) NOT NULL,
        [Category] NVARCHAR(50) NULL,
        [ControllerName] NVARCHAR(50) NULL,
        [ActionName] NVARCHAR(50) NULL,
        [Icon] NVARCHAR(50) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created table [dbo].[SystemModule].';
END
GO

-- 2. Upsert default system modules using MERGE
BEGIN TRANSACTION;

BEGIN TRY
    -- Prepare source staging data
    DECLARE @SourceModules TABLE (
        [ModuleId] INT,
        [ModuleCode] NVARCHAR(50),
        [ModuleName] NVARCHAR(100),
        [Category] NVARCHAR(50),
        [ControllerName] NVARCHAR(50),
        [ActionName] NVARCHAR(50),
        [Icon] NVARCHAR(50),
        [SortOrder] INT,
        [IsActive] BIT
    );

    INSERT INTO @SourceModules ([ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES
        (1,  'DASHBOARD',           'Executive Dashboard',                      'Main Menu',                   'Home',                     'Index',          'layout-dashboard', 1,  0),
        (17, 'POOL_TRANSACTIONS',   'Pool Transactions (Shared Queue)',         'Transaction Workflows',       'Workflow',                 'Pool',           'inbox',            1,  1),
        (2,  'ANALYTICS',           'Executive Analytics & Reports',            'Main Menu',                   'Analytics',                'Index',          'trending-up',      2,  1),
        (18, 'MY_TRANSACTIONS',     'My Transactions',                          'Transaction Workflows',       'Workflow',                 'MyTransactions', 'user-check',       2,  1),
        (3,  'INSPECTION',          'Field Inspection Requests',                'FAAS & Transaction Requests', 'RequestForInspection',     'Index',          'clipboard-check',  10, 1),
        (4,  'PROPERTY_ASSESSMENT', 'Property Assessment Requests',             'FAAS & Transaction Requests', 'PropertyAssessmentRequest', 'Index',          'building',         11, 1),
        (5,  'ASSESSMENT_FAAS',     'Real Property Appraisal & Tax Dec (FAAS)', 'FAAS & Transaction Requests', 'Assessment',                'Index',          'file-spreadsheet', 12, 1),
        (6,  'TAX_MAPPING',         'GIS Spatial & Tax Mapping',                'GIS & Spatial Mapping',       'TaxMapping',               'Index',          'map-pin',          13, 1),
        (7,  'TAX_DECLARATION',     'Tax Declaration Certification Requests',   'FAAS & Transaction Requests', 'TaxDeclarationRequest',     'Index',          'file-text',        14, 1),
        (8,  'LAND_HOLDINGS',       'Certificate of Land Holdings',             'FAAS & Transaction Requests', 'LandHoldingRequest',       'Index',          'map',              15, 1),
        (9,  'ORDER_PAYMENT',       'Order of Payment Billing (OP)',            'Billing & Fee Collections',   'OrderPayment',             'Index',          'receipt',          20, 1),
        (10, 'OWNER_REGISTRATION',  'Taxpayer Directory & Master Records',      'Master Records',              'Owner',                    'Index',          'users',            21, 1),
        (11, 'FILE_LOCATION',       'File Storage & Network Setup',             'System Administration',       'FileLocation',             'Index',          'hard-drive',       30, 1),
        (12, 'WORKFLOW_SETTINGS',   'Workflow Pipeline & Approvals',            'System Administration',       'Workflow',                 'Settings',       'git-pull-request', 31, 1),
        (13, 'USER_MANAGEMENT',     'User Staff Accounts',                      'System Administration',       'UserAccount',              'Index',          'shield-check',     40, 1),
        (14, 'USER_ROLES',          'User Roles Directory',                     'System Administration',       'UserRole',                 'Index',          'user-cog',         41, 1),
        (15, 'ROLE_PERMISSIONS',    'Dynamic Form Permissions',                 'System Administration',       'UserRole',                 'Permissions',    'shield-alert',     42, 1),
        (16, 'DATA_TRANSFER',       'TD Data Transfer Utility',                 'File Maintenance',            'DataTransferUtility',      'Index',          'arrow-right-left', 50, 1),
        (19, 'DASHBOARD_USER',      'Staff Dashboard (Ordinary Users)',         'Dashboards',                  'Dashboard',                'Staff',          'layout-dashboard', 1,  1),
        (20, 'DASHBOARD_HEAD',      'Assessor Head Dashboard',                  'Dashboards',                  'Dashboard',                'AssessorHead',   'gauge',            2,  1),
        (21, 'DASHBOARD_ADMIN',     'System Owner Dashboard (Super Admin)',     'Dashboards',                  'Dashboard',                'SystemOwner',    'server-cog',       3,  1),
        (22, 'ASSESSMENT_PRINT',    'Print Tax Declaration (Records Printout)', 'FAAS & Transaction Requests', 'Assessment',               'PrintPdf',       'printer',          13, 1);

    -- Enable IDENTITY_INSERT so predetermined ModuleId values are retained
    SET IDENTITY_INSERT [dbo].[SystemModule] ON;

    MERGE [dbo].[SystemModule] AS Target
    USING @SourceModules AS Source
    ON Target.[ModuleCode] = Source.[ModuleCode]
    WHEN MATCHED THEN
        UPDATE SET 
            Target.[ModuleName]     = Source.[ModuleName],
            Target.[Category]       = Source.[Category],
            Target.[ControllerName] = Source.[ControllerName],
            Target.[ActionName]     = Source.[ActionName],
            Target.[Icon]           = Source.[Icon],
            Target.[SortOrder]      = Source.[SortOrder],
            Target.[IsActive]       = Source.[IsActive]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
        VALUES (Source.[ModuleId], Source.[ModuleCode], Source.[ModuleName], Source.[Category], Source.[ControllerName], Source.[ActionName], Source.[Icon], Source.[SortOrder], Source.[IsActive]);

    SET IDENTITY_INSERT [dbo].[SystemModule] OFF;

    COMMIT TRANSACTION;
    PRINT 'System modules seeded successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    IF OBJECTPROPERTY(OBJECT_ID('[dbo].[SystemModule]'), 'TableHasIdentity') = 1
        SET IDENTITY_INSERT [dbo].[SystemModule] OFF;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
GO

-- 3. Verification output
PRINT '-------------------------------------------------------------------------';
PRINT 'Verification: Current System Modules:';
PRINT '-------------------------------------------------------------------------';
SELECT [ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive]
FROM [dbo].[SystemModule]
ORDER BY [SortOrder], [ModuleId];
GO

GO


-- ==========================================================================================
-- SECTION: 13_SeedDefaultUserRoles.sql
-- ==========================================================================================
PRINT 'Executing section: 13_SeedDefaultUserRoles.sql...';
GO
-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 13_SeedDefaultUserRoles.sql
-- DESCRIPTION: Seeds default user roles into [dbo].[UserRoleSettings].
--              Ensures table schema compatibility, preserves deterministic GUIDs,
--              maps Division and Workflow TransactionStatus IDs, and executes
--              safe idempotent upserts (MERGE).
-- DATABASE: FairTrackDB
-- =============================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Default User Roles Seeding...';
PRINT '=========================================================================';

-- 1. Ensure UserRoleSettings table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'UserRoleSettings')
BEGIN
    CREATE TABLE [dbo].[UserRoleSettings] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [RoleName] NVARCHAR(150) NOT NULL,
        [TransactionStatusId] INT NULL,
        [Division] INT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Created table [dbo].[UserRoleSettings].';
END
ELSE
BEGIN
    -- Ensure required columns exist
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserRoleSettings' AND COLUMN_NAME = 'TransactionStatusId')
    BEGIN
        ALTER TABLE [dbo].[UserRoleSettings] ADD [TransactionStatusId] INT NULL;
        PRINT 'Added column [TransactionStatusId] to [dbo].[UserRoleSettings].';
    END

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserRoleSettings' AND COLUMN_NAME = 'Division')
    BEGIN
        ALTER TABLE [dbo].[UserRoleSettings] ADD [Division] INT NULL;
        PRINT 'Added column [Division] to [dbo].[UserRoleSettings].';
    END
END
GO

-- 2. Upsert default user roles using MERGE
BEGIN TRANSACTION;

BEGIN TRY
    -- Prepare source staging table with default roles fetched from FairTrackDB
    DECLARE @SourceRoles TABLE (
        [Id] UNIQUEIDENTIFIER,
        [RoleName] NVARCHAR(150),
        [TransactionStatusId] INT,
        [Division] INT,
        [Description] NVARCHAR(MAX),
        [IsActive] BIT
    );

    INSERT INTO @SourceRoles ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive])
    VALUES
        -- System Administrator
        ('50147D2F-24FE-4FA9-B270-9F217F58D333', 'System Administrator',     NULL, NULL, 'Full administrative access, user accounts, and system configuration.', 1),

        -- Division 1: Appraisal and Assessment
        ('5266D996-43CD-490F-95FE-CED504C44AB1', 'Receiving',                1,    1,    'Document intake, requirements checking, and transaction tracking initiation.', 1),
        ('AB9B06C0-881A-4985-8B51-1415A8700A3F', 'Replotting',               2,    1,    'Cadastral map replotting, boundary verification, and parcel plotting.', 1),
        ('6690FBF5-824A-44C3-9714-94F74ADC0AB9', 'Inspection',               3,    1,    'Ocular field inspection, building measurements, and photo verification.', 1),
        ('03C2D297-7D98-426F-B4C2-83074D0913B1', 'Check and Review',         4,    1,    'Field inspection report evaluation, boundary accuracy, and checklist review.', 1),
        ('56643222-0159-460B-A14A-85A6E3EB773D', 'Repinning',                5,    1,    'Property Identification Number assignment and tax mapping index validation.', 1),
        ('24860ADC-D6AC-451E-912B-FA605443BBD0', 'Recommending Approval',    6,    1,    'Appraiser recommendation, valuation endorsement, and review sign-off.', 1),
        ('97A5E41F-BB7B-408E-974A-564F58342635', 'Valuation & Examination', 7,    1,    'Real property appraisal, schedule of market values application, and FAAS computation.', 1),
        ('695E4789-217B-4454-B080-172145F5EF19', 'Approval',                 8,    1,    'City Assessor executive approval, digital signature, and assessment roll enrollment.', 1),
        ('2FFE9A18-7BD1-491B-AFA4-F65DC12F27B7', 'Printing',                 9,    1,    'Notice of Assessment, Tax Declaration, and property record printing.', 1),
        ('D4B13F45-012F-4856-8687-9FE9FAD9F68B', 'Releasing',                10,   1,    'Official document releasing, owner copy issuance, and transaction completion.', 1),

        -- Division 2: Tax Mapping (GIS)
        ('F8122558-287D-4309-8BB8-ED5563E52CEE', 'Receiving (GIS)',          15,   2,    'GIS mapping request intake, survey plan verification, and section indexing.', 1),
        ('17011240-7E73-45AB-8020-C42455AA9A3D', 'Replotting (GIS)',         16,   2,    'GIS digital parcel boundary plotting and base map layer updating.', 1),
        ('96FAC5F0-0C8B-4EB1-9411-4D93889140DD', 'Preparation (GIS)',        17,   2,    'Tax mapping sheet drafting, technical description verification, and GIS cartography.', 1),
        ('B37E9FF8-5D2F-496A-B9EC-2C20A774078D', 'Approval (GIS)',           18,   2,    'GIS Tax Mapping Section supervisory approval and certification endorsement.', 1),
        ('8A8588FD-D2ED-4CB0-856E-390A707E948A', 'Releasing (GIS)',          19,   2,    'Official release of certified tax map prints, blueprints, and RROW clearances.', 1),

        -- Division 3: Records Management
        ('E91ECBD7-EDB5-4C31-BB91-8A8830A5B12C', 'Receiving (Records)',      25,   3,    'Intake for Land Holding and Tax Declaration certificates and requests.', 1),
        ('D7CDC2E1-2C95-42B9-B5E8-902E42695DB3', 'Replotting (Records)',     26,   3,    'Historical title chain verification and property holdings ledger check.', 1),
        ('D39EBAC7-23C1-46C0-AD34-B17BC5533C32', 'Preparation (Records)',    27,   3,    'Certificate of Property Holdings or Certificate of No Property drafting.', 1),
        ('9AE240ED-ACC6-4E3F-A066-FB4BFBA5DC4A', 'Verification (Records)',   28,   3,    'Vault records reconciliation, ledger cross-referencing, and record verification.', 1),
        ('881379E8-1F4C-4AFA-8BFD-BABBD74BD64D', 'Approval (Records)',       29,   3,    'Final supervisory approval and official seal stamping for records issuance.', 1),
        ('0E21FFDB-82E3-4DB0-B6A2-03E8901C5EBC', 'Releasing (Records)',      30,   3,    'Certified True Copy and Certificate releasing to requesting taxpayer.', 1);

    -- Merge by primary key Id or by unique workflow step
    MERGE [dbo].[UserRoleSettings] AS Target
    USING @SourceRoles AS Source
    ON Target.[Id] = Source.[Id] 
       OR (Target.[TransactionStatusId] IS NOT NULL AND Target.[TransactionStatusId] = Source.[TransactionStatusId])
       OR (Target.[RoleName] = Source.[RoleName])
    WHEN MATCHED THEN
        UPDATE SET
            Target.[RoleName]            = Source.[RoleName],
            Target.[TransactionStatusId] = Source.[TransactionStatusId],
            Target.[Division]            = Source.[Division],
            Target.[Description]         = Source.[Description],
            Target.[IsActive]            = Source.[IsActive],
            Target.[UpdatedDate]         = GETDATE()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
        VALUES (Source.[Id], Source.[RoleName], Source.[TransactionStatusId], Source.[Division], Source.[Description], Source.[IsActive], GETDATE(), GETDATE());

    COMMIT TRANSACTION;
    PRINT 'User roles seeded successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
GO

-- 3. Verification output
PRINT '-------------------------------------------------------------------------';
PRINT 'Verification: Current User Role Settings:';
PRINT '-------------------------------------------------------------------------';
SELECT 
    [Id],
    [RoleName],
    [TransactionStatusId] AS [StepId],
    CASE [Division]
        WHEN 1 THEN 'Div 1: Appraisal & Assessment'
        WHEN 2 THEN 'Div 2: Tax Mapping (GIS)'
        WHEN 3 THEN 'Div 3: Records Management'
        ELSE 'Administration / General'
    END AS [DivisionName],
    [Description],
    [IsActive]
FROM [dbo].[UserRoleSettings]
ORDER BY ISNULL([Division], 0), [TransactionStatusId];
GO

GO


-- ==========================================================================================
-- SECTION: 06_SyncUserRolesWithWorkflowSteps.sql
-- ==========================================================================================
PRINT 'Executing section: 06_SyncUserRolesWithWorkflowSteps.sql...';
GO
-- =========================================================================
-- SCRIPT: 06_SyncUserRolesWithWorkflowSteps.sql
-- DESCRIPTION: Syncs [dbo].[UserRoleSettings] with [dbo].[TransactionStatus]
--              so that User Roles represent the Workflow Steps / Allowed Target Steps
-- DATABASE: FairTrackDB
-- =========================================================================
-- USE []; -- Run against active connected database
GO

-- 1. Ensure TransactionStatusId column exists on UserRoleSettings
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserRoleSettings' AND COLUMN_NAME = 'TransactionStatusId')
BEGIN
    ALTER TABLE [dbo].[UserRoleSettings] ADD [TransactionStatusId] INT NULL;
    PRINT 'Added TransactionStatusId column to UserRoleSettings table.';
END
GO

-- 2. Ensure System Administrator role exists
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [Id] = '50147D2F-24FE-4FA9-B270-9F217F58D333')
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('50147D2F-24FE-4FA9-B270-9F217F58D333', 'System Administrator', NULL, NULL, 'Full administrative access, user accounts, and system configuration.', 1, GETDATE(), GETDATE());
    PRINT 'Created System Administrator role.';
END
ELSE
BEGIN
    UPDATE [dbo].[UserRoleSettings]
    SET [RoleName] = 'System Administrator', [TransactionStatusId] = NULL, [Division] = NULL
    WHERE [Id] = '50147D2F-24FE-4FA9-B270-9F217F58D333';
    PRINT 'Updated System Administrator role.';
END
GO

-- 3. Upsert each workflow status as an official User Role
DECLARE @StatusId INT, @Name NVARCHAR(150), @Div INT;
DECLARE cur CURSOR FOR
    SELECT [TransactionStatusId], [Name], [DivisionId]
    FROM [dbo].[TransactionStatus]
    WHERE [TransactionStatusId] > 0
    ORDER BY [TransactionStatusId];

OPEN cur;
FETCH NEXT FROM cur INTO @StatusId, @Name, @Div;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @Suffix NVARCHAR(50) = CASE WHEN @Div = 2 THEN ' (GIS)' WHEN @Div = 3 THEN ' (Records)' ELSE '' END;
    DECLARE @RoleName NVARCHAR(150) = @Name + @Suffix;
    DECLARE @Desc NVARCHAR(300) = CASE @StatusId
        WHEN 1 THEN 'Document intake, requirements checking, and transaction tracking initiation.'
        WHEN 2 THEN 'Cadastral map replotting, boundary verification, and parcel plotting.'
        WHEN 3 THEN 'Ocular field inspection, building measurements, and photo verification.'
        WHEN 4 THEN 'Field inspection report evaluation, boundary accuracy, and checklist review.'
        WHEN 5 THEN 'Property Identification Number assignment and tax mapping index validation.'
        WHEN 6 THEN 'Appraiser recommendation, valuation endorsement, and review sign-off.'
        WHEN 7 THEN 'Real property appraisal, schedule of market values application, and FAAS computation.'
        WHEN 8 THEN 'City Assessor executive approval, digital signature, and assessment roll enrollment.'
        WHEN 9 THEN 'Notice of Assessment, Tax Declaration, and property record printing.'
        WHEN 10 THEN 'Official document releasing, owner copy issuance, and transaction completion.'
        WHEN 15 THEN 'GIS mapping request intake, survey plan verification, and section indexing.'
        WHEN 16 THEN 'GIS digital parcel boundary plotting and base map layer updating.'
        WHEN 17 THEN 'Tax mapping sheet drafting, technical description verification, and GIS cartography.'
        WHEN 18 THEN 'GIS Tax Mapping Section supervisory approval and certification endorsement.'
        WHEN 19 THEN 'Official release of certified tax map prints, blueprints, and RROW clearances.'
        WHEN 25 THEN 'Intake for Land Holding and Tax Declaration certificates and requests.'
        WHEN 26 THEN 'Historical title chain verification and property holdings ledger check.'
        WHEN 27 THEN 'Certificate of Property Holdings or Certificate of No Property drafting.'
        WHEN 28 THEN 'Vault records reconciliation, ledger cross-referencing, and record verification.'
        WHEN 29 THEN 'Final supervisory approval and official seal stamping for records issuance.'
        WHEN 30 THEN 'Certified True Copy and Certificate releasing to requesting taxpayer.'
        ELSE 'Workflow execution for ' + @Name
    END;

    IF EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [TransactionStatusId] = @StatusId)
    BEGIN
        UPDATE [dbo].[UserRoleSettings]
        SET [RoleName] = @RoleName, [Division] = @Div, [Description] = @Desc, [UpdatedDate] = GETDATE()
        WHERE [TransactionStatusId] = @StatusId;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
        VALUES (NEWID(), @RoleName, @StatusId, @Div, @Desc, 1, GETDATE(), GETDATE());
    END

    FETCH NEXT FROM cur INTO @StatusId, @Name, @Div;
END;

CLOSE cur;
DEALLOCATE cur;
GO

-- 4. Clean up legacy role names that were positions and have no TransactionStatusId
DELETE FROM [dbo].[RoleModulePermission] 
WHERE [RoleId] IN (
    SELECT [Id] FROM [dbo].[UserRoleSettings] 
    WHERE [TransactionStatusId] IS NULL AND [RoleName] <> 'System Administrator'
);

DELETE FROM [dbo].[UserRoleSettings] 
WHERE [TransactionStatusId] IS NULL AND [RoleName] <> 'System Administrator';
GO

PRINT 'UserRoleSettings successfully synced with TransactionStatus!';
GO

GO


-- ==========================================================================================
-- SECTION: 08_SeedDefaultRolePermissions.sql
-- ==========================================================================================
PRINT 'Executing section: 08_SeedDefaultRolePermissions.sql...';
GO
-- =========================================================================
-- SCRIPT: 08_SeedDefaultRolePermissions.sql
-- DESCRIPTION: Sets up default module permissions in [dbo].[RoleModulePermission]:
--              1. Automatically ensures [dbo].[SystemModule] and [dbo].[UserRoleSettings]
--                 contain all required baseline modules and roles.
--              2. By default, EVERY active role has access to:
--                 - Pool Transactions (POOL_TRANSACTIONS)
--                 - My Transactions (MY_TRANSACTIONS)
--              3. Division 1 Receiving role gets:
--                 - RPA: Property Assessment Requests (PROPERTY_ASSESSMENT)
--                 - RFI: Field Inspection Requests (INSPECTION)
--              4. Division 2 Receiving role gets:
--                 - Tax Mapping (TAX_MAPPING)
--              5. Division 3 Receiving role gets:
--                 - Tax Declaration (TAX_DECLARATION)
--                 - Land Holding (LAND_HOLDINGS)
--              6. System Administrator gets all active system modules.
-- DATABASE: FairTrackDB
-- =========================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Default Role & Module Permissions Seeding';
PRINT '=========================================================================';

-- -----------------------------------------------------------------------------
-- Pre-step A: Ensure SystemModule Table & Core Modules Exist
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SystemModule')
BEGIN
    CREATE TABLE [dbo].[SystemModule] (
        [ModuleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ModuleCode] NVARCHAR(50) NOT NULL UNIQUE,
        [ModuleName] NVARCHAR(100) NOT NULL,
        [Category] NVARCHAR(50) NULL,
        [ControllerName] NVARCHAR(50) NULL,
        [ActionName] NVARCHAR(50) NULL,
        [Icon] NVARCHAR(50) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created table [dbo].[SystemModule].';
END
GO

-- Upsert baseline modules if not yet populated
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModule] WHERE [ModuleCode] IN ('POOL_TRANSACTIONS', 'MY_TRANSACTIONS'))
BEGIN
    PRINT 'System modules missing; auto-seeding baseline modules into SystemModule...';
    
    SET IDENTITY_INSERT [dbo].[SystemModule] ON;

    DECLARE @InitialModules TABLE (
        [ModuleId] INT, [ModuleCode] NVARCHAR(50), [ModuleName] NVARCHAR(100),
        [Category] NVARCHAR(50), [ControllerName] NVARCHAR(50), [ActionName] NVARCHAR(50),
        [Icon] NVARCHAR(50), [SortOrder] INT, [IsActive] BIT
    );

    INSERT INTO @InitialModules ([ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES
        (1,  'DASHBOARD',           'Executive Dashboard',                      'Main Menu',                   'Home',                     'Index',          'layout-dashboard', 1,  1),
        (17, 'POOL_TRANSACTIONS',   'Pool Transactions (Shared Queue)',         'Transaction Workflows',       'Workflow',                 'Pool',           'inbox',            1,  1),
        (2,  'ANALYTICS',           'Executive Analytics & Reports',            'Main Menu',                   'Analytics',                'Index',          'trending-up',      2,  1),
        (18, 'MY_TRANSACTIONS',     'My Transactions',                          'Transaction Workflows',       'Workflow',                 'MyTransactions', 'user-check',       2,  1),
        (3,  'INSPECTION',          'Field Inspection Requests',                'FAAS & Transaction Requests', 'RequestForInspection',     'Index',          'clipboard-check',  10, 1),
        (4,  'PROPERTY_ASSESSMENT', 'Property Assessment Requests',             'FAAS & Transaction Requests', 'PropertyAssessmentRequest', 'Index',          'building',         11, 1),
        (5,  'ASSESSMENT_FAAS',     'Real Property Appraisal & Tax Dec (FAAS)', 'FAAS & Transaction Requests', 'Assessment',                'Index',          'file-spreadsheet', 12, 1),
        (6,  'TAX_MAPPING',         'GIS Spatial & Tax Mapping',                'GIS & Spatial Mapping',       'TaxMapping',               'Index',          'map-pin',          13, 1),
        (7,  'TAX_DECLARATION',     'Tax Declaration Certification Requests',   'FAAS & Transaction Requests', 'TaxDeclarationRequest',     'Index',          'file-text',        14, 1),
        (8,  'LAND_HOLDINGS',       'Certificate of Land Holdings',             'FAAS & Transaction Requests', 'LandHoldingRequest',       'Index',          'map',              15, 1),
        (9,  'ORDER_PAYMENT',       'Order of Payment Billing (OP)',            'Billing & Fee Collections',   'OrderPayment',             'Index',          'receipt',          20, 1),
        (10, 'OWNER_REGISTRATION',  'Taxpayer Directory & Master Records',      'Master Records',              'Owner',                    'Index',          'users',            21, 1),
        (11, 'FILE_LOCATION',       'File Storage & Network Setup',             'System Administration',       'FileLocation',             'Index',          'hard-drive',       30, 1),
        (12, 'WORKFLOW_SETTINGS',   'Workflow Pipeline & Approvals',            'System Administration',       'Workflow',                 'Settings',       'git-pull-request', 31, 1),
        (13, 'USER_MANAGEMENT',     'User Staff Accounts',                      'System Administration',       'UserAccount',              'Index',          'shield-check',     40, 1),
        (14, 'USER_ROLES',          'User Roles Directory',                     'System Administration',       'UserRole',                 'Index',          'user-cog',         41, 1),
        (15, 'ROLE_PERMISSIONS',    'Dynamic Form Permissions',                 'System Administration',       'UserRole',                 'Permissions',    'shield-alert',     42, 1),
        (16, 'DATA_TRANSFER',       'TD Data Transfer Utility',                 'File Maintenance',            'DataTransferUtility',      'Index',          'arrow-right-left', 50, 1);

    MERGE [dbo].[SystemModule] AS Target
    USING @InitialModules AS Source
    ON Target.[ModuleCode] = Source.[ModuleCode]
    WHEN MATCHED THEN
        UPDATE SET Target.[ModuleName] = Source.[ModuleName], Target.[ControllerName] = Source.[ControllerName], Target.[ActionName] = Source.[ActionName]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
        VALUES (Source.[ModuleId], Source.[ModuleCode], Source.[ModuleName], Source.[Category], Source.[ControllerName], Source.[ActionName], Source.[Icon], Source.[SortOrder], Source.[IsActive]);

    SET IDENTITY_INSERT [dbo].[SystemModule] OFF;
    PRINT 'Baseline SystemModule records inserted successfully.';
END
GO

-- -----------------------------------------------------------------------------
-- Pre-step B: Ensure UserRoleSettings Table & Baseline Roles Exist
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'UserRoleSettings')
BEGIN
    CREATE TABLE [dbo].[UserRoleSettings] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [RoleName] NVARCHAR(150) NOT NULL,
        [TransactionStatusId] INT NULL,
        [Division] INT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Created table [dbo].[UserRoleSettings].';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [RoleName] = 'System Administrator')
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('50147D2F-24FE-4FA9-B270-9F217F58D333', 'System Administrator', NULL, NULL, 'Full administrative access, user accounts, and system configuration.', 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [Division] = 1 AND [TransactionStatusId] = 1)
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('5266D996-43CD-490F-95FE-CED504C44AB1', 'Receiving', 1, 1, 'Document intake, requirements checking, and transaction tracking initiation.', 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [Division] = 2 AND [TransactionStatusId] = 15)
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('F8122558-287D-4309-8BB8-ED5563E52CEE', 'Receiving (GIS)', 15, 2, 'GIS mapping request intake, survey plan verification, and section indexing.', 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [Division] = 3 AND [TransactionStatusId] = 25)
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('E91ECBD7-EDB5-4C31-BB91-8A8830A5B12C', 'Receiving (Records)', 25, 3, 'Intake for Land Holding and Tax Declaration certificates and requests.', 1, GETDATE(), GETDATE());
END
GO

-- -----------------------------------------------------------------------------
-- Core Step: Apply Role Module Permissions
-- -----------------------------------------------------------------------------
BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Resolve Module IDs dynamically from [dbo].[SystemModule]
    DECLARE @PoolModuleId INT,
            @MyTransModuleId INT,
            @InspectionModuleId INT,
            @PropAssessmentModuleId INT,
            @TaxMappingModuleId INT,
            @TaxDecModuleId INT,
            @LandHoldingModuleId INT;

    SELECT @PoolModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'POOL_TRANSACTIONS';
    SELECT @MyTransModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'MY_TRANSACTIONS';
    SELECT @InspectionModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'INSPECTION';
    SELECT @PropAssessmentModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'PROPERTY_ASSESSMENT';
    SELECT @TaxMappingModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'TAX_MAPPING';
    SELECT @TaxDecModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'TAX_DECLARATION';
    SELECT @LandHoldingModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'LAND_HOLDINGS';

    PRINT 'Resolved target System Modules successfully.';

    -- 2. Resolve Receiving Role IDs dynamically from [dbo].[UserRoleSettings]
    DECLARE @Div1ReceivingRoleId UNIQUEIDENTIFIER,
            @Div2ReceivingRoleId UNIQUEIDENTIFIER,
            @Div3ReceivingRoleId UNIQUEIDENTIFIER,
            @AdminRoleId UNIQUEIDENTIFIER;

    -- Division 1 Receiving (TransactionStatusId = 1 / Receiving)
    SELECT TOP 1 @Div1ReceivingRoleId = Id 
    FROM [dbo].[UserRoleSettings] 
    WHERE Division = 1 AND (TransactionStatusId = 1 OR RoleName = 'Receiving');

    -- Division 2 Receiving (TransactionStatusId = 15 / Receiving (GIS))
    SELECT TOP 1 @Div2ReceivingRoleId = Id 
    FROM [dbo].[UserRoleSettings] 
    WHERE Division = 2 AND (TransactionStatusId = 15 OR RoleName LIKE '%Receiving%');

    -- Division 3 Receiving (TransactionStatusId = 25 / Receiving (Records))
    SELECT TOP 1 @Div3ReceivingRoleId = Id 
    FROM [dbo].[UserRoleSettings] 
    WHERE Division = 3 AND (TransactionStatusId = 25 OR RoleName LIKE '%Receiving%');

    -- System Administrator
    SELECT TOP 1 @AdminRoleId = Id 
    FROM [dbo].[UserRoleSettings] 
    WHERE RoleName = 'System Administrator' OR Id = '50147D2F-24FE-4FA9-B270-9F217F58D333';

    PRINT 'Resolved Receiving and Admin role IDs successfully.';

    -- 3. Reset non-admin permissions to clean baseline
    PRINT 'Cleaning existing permissions for non-admin roles...';
    
    DELETE FROM [dbo].[RoleModulePermission]
    WHERE RoleId <> @AdminRoleId;

    -- 4. Grant EVERY Role default access to POOL_TRANSACTIONS & MY_TRANSACTIONS
    PRINT 'Granting Pool Transactions & My Transactions to ALL active roles...';

    IF @PoolModuleId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
        SELECT Id, @PoolModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE()
        FROM [dbo].[UserRoleSettings]
        WHERE IsActive = 1 AND Id <> @AdminRoleId;
    END

    IF @MyTransModuleId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
        SELECT Id, @MyTransModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE()
        FROM [dbo].[UserRoleSettings]
        WHERE IsActive = 1 AND Id <> @AdminRoleId;
    END

    PRINT 'Default Pool & My Transactions access granted to all roles.';

    -- 5. Division 1 Receiving: Additional access to RPA (PROPERTY_ASSESSMENT) and RFI (INSPECTION)
    IF @Div1ReceivingRoleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 1 Receiving additional permissions (RPA & RFI)...';

        IF @PropAssessmentModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div1ReceivingRoleId, @PropAssessmentModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        IF @InspectionModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div1ReceivingRoleId, @InspectionModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        PRINT 'Division 1 Receiving permissions configured.';
    END

    -- 6. Division 2 Receiving: Additional access to Tax Mapping (TAX_MAPPING)
    IF @Div2ReceivingRoleId IS NOT NULL AND @TaxMappingModuleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 2 Receiving additional permissions (Tax Mapping)...';

        INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
        VALUES (@Div2ReceivingRoleId, @TaxMappingModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());

        PRINT 'Division 2 Receiving permissions configured.';
    END

    -- 7. Division 3 Receiving: Additional access to Tax Dec (TAX_DECLARATION) and Land Holding (LAND_HOLDINGS)
    IF @Div3ReceivingRoleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 3 Receiving additional permissions (Tax Dec & Land Holding)...';

        IF @TaxDecModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div3ReceivingRoleId, @TaxDecModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        IF @LandHoldingModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div3ReceivingRoleId, @LandHoldingModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        PRINT 'Division 3 Receiving permissions configured.';
    END

    -- 8. Ensure System Administrator has access to all active system modules
    IF @AdminRoleId IS NOT NULL
    BEGIN
        PRINT 'Configuring System Administrator full access to all active modules...';

        MERGE [dbo].[RoleModulePermission] AS target
        USING (
            SELECT @AdminRoleId AS RoleId, ModuleId
            FROM [dbo].[SystemModule]
            WHERE IsActive = 1
        ) AS source
        ON target.RoleId = source.RoleId AND target.ModuleId = source.ModuleId
        WHEN MATCHED THEN
            UPDATE SET CanView = 1, CanCreate = 1, CanEdit = 1, CanDelete = 1, CanApprove = 1,
                       UpdatedBy = 'DefaultRoleAccessScript', UpdatedDate = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (source.RoleId, source.ModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());

        PRINT 'System Administrator permissions configured.';
    END

    COMMIT TRANSACTION;
    PRINT '=========================================================================';
    PRINT 'Default role permissions applied successfully.';
    PRINT '=========================================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    PRINT 'ERROR during script execution: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

-- -----------------------------------------------------------------------------
-- Verification Query
-- -----------------------------------------------------------------------------
SELECT 
    ISNULL(CAST(r.Division AS NVARCHAR(10)), 'ADMIN') AS [Div],
    r.RoleName AS [Role Name],
    m.ModuleCode AS [Module Code],
    m.ModuleName AS [Module Title],
    p.CanView,
    p.CanCreate,
    p.CanEdit,
    p.CanDelete,
    p.CanApprove
FROM [dbo].[RoleModulePermission] p
INNER JOIN [dbo].[UserRoleSettings] r ON p.RoleId = r.Id
INNER JOIN [dbo].[SystemModule] m ON p.ModuleId = m.ModuleId
WHERE r.RoleName LIKE '%Receiv%' OR r.RoleName = 'System Administrator'
ORDER BY r.Division, r.RoleName, m.SortOrder;
GO

GO


-- ==========================================================================================
-- SECTION: 19_SeedTransactionStatusesAndWorkflowSequences.sql
-- ==========================================================================================
PRINT 'Executing section: 19_SeedTransactionStatusesAndWorkflowSequences.sql...';
GO
-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 19_SeedTransactionStatusesAndWorkflowSequences.sql
-- DESCRIPTION: Seeds official Transaction Statuses and Workflow Sequences 
--              fetched from CAST reference system into [dbo].[TransactionStatus]
--              and [dbo].[WorkflowSequence].
--              Ensures table compatibility, safe idempotent upserts (MERGE),
--              and wires up routing across Divisions 1, 2, and 3.
-- =============================================================================
SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Transaction Statuses & Workflow Sequences Seeding from CAST...';
PRINT '=========================================================================';

-- 1. Ensure TransactionStatus table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TransactionStatus')
BEGIN
    CREATE TABLE [dbo].[TransactionStatus] (
        [TransactionStatusId] INT NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(150) NOT NULL,
        [IsComplete] BIT NOT NULL DEFAULT 0,
        [DivisionId] INT NOT NULL DEFAULT 1
    );
    PRINT 'Created [dbo].[TransactionStatus] table.';
END
GO

-- 2. Seed / Upsert TransactionStatus records fetched from CAST
PRINT 'Seeding [dbo].[TransactionStatus] records...';

MERGE INTO [dbo].[TransactionStatus] AS target
USING (
    VALUES
        -- Division 1: Real Property Appraisal & Tax Assessment Division
        (0,  N'Deferred',                 0, 1),
        (1,  N'Receiving',                0, 1),
        (2,  N'Replotting',               0, 1),
        (3,  N'Inspection',               0, 1),
        (4,  N'Check and Review',         0, 1),
        (5,  N'Repinning',                0, 1),
        (6,  N'Recommending Approval',    0, 1),
        (7,  N'Valuation & Examination',  0, 1),
        (8,  N'Approval',                 0, 1),
        (9,  N'Printing',                 0, 1),
        (10, N'Releasing',                1, 1),

        -- Division 2: GIS Spatial & Tax Mapping Division
        (15, N'Receiving (GIS)',          0, 2),
        (16, N'Replotting (GIS)',         0, 2),
        (17, N'Preparation (GIS)',        0, 2),
        (18, N'Approval (GIS)',           0, 2),
        (19, N'Releasing (GIS)',          1, 2),

        -- Division 3: Records & Tax Declaration Management Division
        (25, N'Receiving (Records)',      0, 3),
        (26, N'Replotting (Records)',     0, 3),
        (27, N'Preparation (Records)',    0, 3),
        (28, N'Verification (Records)',   0, 3),
        (29, N'Approval (Records)',       0, 3),
        (30, N'Releasing (Records)',      1, 3)
) AS source ([TransactionStatusId], [Name], [IsComplete], [DivisionId])
ON target.[TransactionStatusId] = source.[TransactionStatusId]
WHEN MATCHED THEN
    UPDATE SET
        target.[Name] = source.[Name],
        target.[IsComplete] = source.[IsComplete],
        target.[DivisionId] = source.[DivisionId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TransactionStatusId], [Name], [IsComplete], [DivisionId])
    VALUES (source.[TransactionStatusId], source.[Name], source.[IsComplete], source.[DivisionId]);

PRINT 'Seeded ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' records into [dbo].[TransactionStatus].';
GO

-- 3. Ensure WorkflowSequence table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'WorkflowSequence')
BEGIN
    CREATE TABLE [dbo].[WorkflowSequence] (
        [SequenceId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TransactionTypeId] INT NOT NULL,
        [CurrentStatusId] INT NOT NULL,
        [NextStatusId] INT NULL,
        [IsApproval] BIT NOT NULL DEFAULT 0
    );
    PRINT 'Created [dbo].[WorkflowSequence] table.';
END
GO

-- 4. Seed / Upsert WorkflowSequence routes fetched from CAST
PRINT 'Seeding [dbo].[WorkflowSequence] routes...';

-- Temporary table to hold defined sequences
DECLARE @CAST_Sequences TABLE (
    [TransactionTypeId] INT NOT NULL,
    [CurrentStatusId] INT NOT NULL,
    [NextStatusId] INT NULL,
    [IsApproval] BIT NOT NULL
);

INSERT INTO @CAST_Sequences ([TransactionTypeId], [CurrentStatusId], [NextStatusId], [IsApproval])
VALUES
    -- =========================================================================
    -- Transaction Type 1: Request for Inspection
    -- =========================================================================
    (1, 1, 2, 0),      -- Receiving -> Replotting
    (1, 2, 4, 0),      -- Replotting -> Check and Review
    (1, 3, 4, 0),      -- Inspection -> Check and Review
    (1, 4, 5, 0),      -- Check and Review -> Repinning
    (1, 5, 6, 0),      -- Repinning -> Recommending Approval
    (1, 6, 7, 0),      -- Recommending Approval -> Valuation & Examination
    (1, 7, 8, 0),      -- Valuation & Examination -> Approval
    (1, 8, 9, 1),      -- Approval -> Printing (Approval Step = 1)
    (1, 9, 10, 0),     -- Printing -> Releasing
    (1, 10, NULL, 0),  -- Releasing (Final Step)

    -- =========================================================================
    -- Transaction Type 2: Property Assessment
    -- =========================================================================
    (2, 1, 2, 0),      -- Receiving -> Replotting
    (2, 2, 4, 0),      -- Replotting -> Check and Review
    (2, 3, 4, 0),      -- Inspection -> Check and Review
    (2, 4, 5, 0),      -- Check and Review -> Repinning
    (2, 5, 6, 0),      -- Repinning -> Recommending Approval
    (2, 6, 7, 0),      -- Recommending Approval -> Valuation & Examination
    (2, 7, 8, 0),      -- Valuation & Examination -> Approval
    (2, 8, 9, 1),      -- Approval -> Printing (Approval Step = 1)
    (2, 9, 10, 0),     -- Printing -> Releasing
    (2, 10, NULL, 0),  -- Releasing (Final Step)

    -- =========================================================================
    -- Transaction Type 3: Tax Mapping (GIS)
    -- =========================================================================
    (3, 15, 16, 0),    -- Receiving (GIS) -> Replotting (GIS)
    (3, 15, 17, 0),    -- Receiving (GIS) -> Preparation (GIS)
    (3, 16, 18, 0),    -- Replotting (GIS) -> Approval (GIS)
    (3, 17, 18, 0),    -- Preparation (GIS) -> Approval (GIS)
    (3, 18, 19, 1),    -- Approval (GIS) -> Release (GIS) (Approval Step = 1)
    (3, 19, NULL, 0),  -- Release (GIS) (Final Step)

    -- =========================================================================
    -- Transaction Type 4: Tax Declaration (Records)
    -- =========================================================================
    (4, 25, 26, 0),    -- Receiving (Records) -> Replotting (Records)
    (4, 25, 27, 0),    -- Receiving (Records) -> Preparation (Records)
    (4, 26, 28, 0),    -- Replotting (Records) -> Verification (Records)
    (4, 27, 28, 0),    -- Preparation (Records) -> Verification (Records)
    (4, 28, 29, 0),    -- Verification (Records) -> Approval (Records)
    (4, 29, 10, 1),    -- Approval (Records) -> Releasing (Div 1)
    (4, 29, 19, 1),    -- Approval (Records) -> Releasing (GIS)
    (4, 29, 30, 1),    -- Approval (Records) -> Releasing (Records) (Approval Step = 1)
    (4, 30, NULL, 0),  -- Releasing (Records) (Final Step)

    -- =========================================================================
    -- Transaction Type 5: Land Holding (Certifications)
    -- =========================================================================
    (5, 25, 26, 0),    -- Receiving (Records) -> Replotting (Records)
    (5, 26, 27, 0),    -- Replotting (Records) -> Preparation (Records)
    (5, 27, 28, 0),    -- Preparation (Records) -> Verification (Records)
    (5, 28, 29, 0),    -- Verification (Records) -> Approval (Records)
    (5, 29, 10, 1),    -- Approval (Records) -> Releasing (Div 1)
    (5, 29, 19, 1),    -- Approval (Records) -> Releasing (GIS)
    (5, 29, 30, 1),    -- Approval (Records) -> Releasing (Records) (Approval Step = 1)
    (5, 30, NULL, 0);  -- Releasing (Records) (Final Step)

-- Safe insert only if missing
INSERT INTO [dbo].[WorkflowSequence] ([TransactionTypeId], [CurrentStatusId], [NextStatusId], [IsApproval])
SELECT s.[TransactionTypeId], s.[CurrentStatusId], s.[NextStatusId], s.[IsApproval]
FROM @CAST_Sequences s
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[WorkflowSequence] w
    WHERE w.[TransactionTypeId] = s.[TransactionTypeId]
      AND w.[CurrentStatusId] = s.[CurrentStatusId]
      AND ((w.[NextStatusId] IS NULL AND s.[NextStatusId] IS NULL) OR w.[NextStatusId] = s.[NextStatusId])
);

PRINT 'Seeded CAST workflow sequences into [dbo].[WorkflowSequence].';
GO

-- 5. Verification
SELECT 'TransactionStatus' AS [Table], COUNT(*) AS [RowCount] FROM [dbo].[TransactionStatus]
UNION ALL
SELECT 'WorkflowSequence' AS [Table], COUNT(*) AS [RowCount] FROM [dbo].[WorkflowSequence];
GO

PRINT '=========================================================================';
PRINT 'Transaction Statuses & Workflow Sequences Seeding Completed Successfully!';
PRINT '=========================================================================';

GO


-- ==========================================================================================
-- SECTION: 07_ImportUsersFromCast.sql
-- ==========================================================================================
PRINT 'Executing section: 07_ImportUsersFromCast.sql...';
GO
-- =========================================================================
-- SCRIPT: 07_ImportUsersFromCast.sql
-- DESCRIPTION: Standalone seed script for all staff user accounts exported
--              from legacy CAST into [dbo].[UserAccounts] with Division,
--              User Position, and Workflow Step Role mappings.
--              Self-contained INSERT/MERGE script (does NOT require DBCast).
-- DEFAULT PASSWORD FOR STAFF ACCOUNTS: P@ssword2026!
-- =========================================================================
SET NOCOUNT ON;
GO

PRINT '=======================================================';
PRINT 'Seeding Staff User Accounts into [dbo].[UserAccounts]';
PRINT '=======================================================';

-- 1. Ensure columns exist on target UserAccounts table and drop legacy Password column if present
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'Password')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] DROP COLUMN [Password];
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'DivisionId')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [DivisionId] INT NULL;
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'WorkflowRoleId')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [WorkflowRoleId] INT NULL;
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'WorkflowRoleName')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [WorkflowRoleName] NVARCHAR(150) NULL;
END
GO

-- Default ASP.NET Core Identity PasswordHasher PBKDF2 hash for password: 'P@ssword2026!'
DECLARE @StaffPasswordHash NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==';

-- 2. Insert / Update Staff User Accounts
MERGE INTO [dbo].[UserAccounts] AS Target
USING (
    VALUES
         ('2ec18a78-087e-e7fc-1828-1f8f38ccffcf', N'leonilie', @StaffPasswordHash, N'Leonilie T. Escara', N'leonilie@gensan.gov.ph', N'EMP-13037', 4, N'Records & Certification Section', N'09170013037', 1, 3, 29, N'Approval (Records)')
        ,('1f78ef08-01ac-1c10-bde1-314cc56747ad', N'ariane', @StaffPasswordHash, N'Ariane Jane V. Fernandez', N'ariane@gensan.gov.ph', N'EMP-13038', 5, N'Records & Certification Section', N'09170013038', 1, 3, 28, N'Verification (Records)')
        ,('9b90d7a3-f796-c8c0-ae00-751a67c2e100', N'ruben', @StaffPasswordHash, N'Ruben M. Frugalidad', N'ruben@gensan.gov.ph', N'EMP-13039', 2, N'Records & Certification Section', N'09170013039', 1, 3, 28, N'Verification (Records)')
        ,('652e039f-33ae-bde3-3a3a-358cb7c2b10e', N'aillen', @StaffPasswordHash, N'Aillen R. Defante', N'aillen@gensan.gov.ph', N'EMP-13040', 5, N'Records & Certification Section', N'09170013040', 1, 3, 25, N'Receiving (Records)')
        ,('4a4ad407-c69b-2007-59a4-3bf587763113', N'wilma', @StaffPasswordHash, N'Wilma D. Paciente', N'wilma@gensan.gov.ph', N'EMP-13041', 5, N'Records & Certification Section', N'09170013041', 1, 3, 25, N'Receiving (Records)')
        ,('94f2bd77-b5ea-4e38-f295-7e28411ec938', N'angelica', @StaffPasswordHash, N'Angelica O. Ranit', N'angelica@gensan.gov.ph', N'EMP-13042', 5, N'Records & Certification Section', N'09170013042', 1, 3, 27, N'Preparation (Records)')
        ,('bd58563b-7a47-6996-1698-867236e646f1', N'john', @StaffPasswordHash, N'John Vincent Bueza', N'john@gensan.gov.ph', N'EMP-13045', 4, N'Records & Certification Section', N'09170013045', 1, 3, 26, N'Replotting (Records)')
        ,('380740a8-11e7-d622-6ea0-9aea5b66d062', N'kaila', @StaffPasswordHash, N'Kaila Fate S. Fresco', N'kaila@gensan.gov.ph', N'EMP-13047', 5, N'Real Property Appraisal & Assessment Division', N'09170013047', 1, 1, 3, N'Inspection')
        ,('78f9a635-d63b-a8a8-6643-bdfcea957562', N'genia', @StaffPasswordHash, N'Genia G. Redo-do', N'genia@gensan.gov.ph', N'EMP-13049', 5, N'Real Property Appraisal & Assessment Division', N'09170013049', 1, 1, 1, N'Receiving')
        ,('f57a2001-8cb3-9688-5e4e-fdbdacebfef4', N'alexis', @StaffPasswordHash, N'Alexis Jane M. Barcelona', N'alexis@gensan.gov.ph', N'EMP-13050', 2, N'Real Property Appraisal & Assessment Division', N'09170013050', 1, 1, 1, N'Receiving')
        ,('89f8fc32-a4c5-1660-f9fa-5920175a3590', N'norlyn', @StaffPasswordHash, N'Norlyn Mae P. Buenavides', N'norlyn@gensan.gov.ph', N'EMP-13051', 5, N'Real Property Appraisal & Assessment Division', N'09170013051', 1, 1, 1, N'Receiving')
        ,('69f23cec-bccd-4c05-a595-1c77492d5e01', N'krish', @StaffPasswordHash, N'Krish Y. Estellina', N'krish@gensan.gov.ph', N'EMP-13052', 5, N'Real Property Appraisal & Assessment Division', N'09170013052', 1, 1, 3, N'Inspection')
        ,('c9bfb6c5-3f1e-bb80-3d9e-de090b2d816e', N'jim', @StaffPasswordHash, N'Jim P. Fernandez jr.', N'jim@gensan.gov.ph', N'EMP-13053', 5, N'Real Property Appraisal & Assessment Division', N'09170013053', 1, 1, 3, N'Inspection')
        ,('3460a53f-0139-423b-5429-4a0c56f3e4db', N'reymart', @StaffPasswordHash, N'Reymart A. Librado', N'reymart@gensan.gov.ph', N'EMP-13054', 4, N'Real Property Appraisal & Assessment Division', N'09170013054', 1, 1, 3, N'Inspection')
        ,('db88bf48-7a75-2fb6-da6a-bfbbd83ac75c', N'renan', @StaffPasswordHash, N'Renan V. Osoteo', N'renan@gensan.gov.ph', N'EMP-13055', 5, N'Real Property Appraisal & Assessment Division', N'09170013055', 1, 1, 3, N'Inspection')
        ,('88a13fe0-20a7-9a82-5493-d473d1c0b502', N'suzette', @StaffPasswordHash, N'Suzette M. Gascon', N'suzette@gensan.gov.ph', N'EMP-13056', 5, N'Real Property Appraisal & Assessment Division', N'09170013056', 1, 1, 3, N'Inspection')
        ,('bff20fe1-aca6-8c65-b569-f78531187775', N'ar jell', @StaffPasswordHash, N'Ar Jell John L. Estember', N'arjell@gensan.gov.ph', N'EMP-13060', 0, N'Real Property Appraisal & Assessment Division', N'09170013060', 1, 1, 3, N'Inspection')
        ,('7c3951c5-86b3-9c54-202e-37a5d71a8c19', N'fernan', @StaffPasswordHash, N'Fernan B. Fariaria', N'fernan@gensan.gov.ph', N'EMP-13061', 5, N'Real Property Appraisal & Assessment Division', N'09170013061', 1, 1, 3, N'Inspection')
        ,('f37e2bc1-c92b-40b2-1724-c5d71901fe16', N'john rey', @StaffPasswordHash, N'John Rey G. Peusca', N'johnrey@gensan.gov.ph', N'EMP-13063', 5, N'Real Property Appraisal & Assessment Division', N'09170013063', 1, 1, 3, N'Inspection')
        ,('05ca446a-89b8-09f5-a0bb-efd160fa2798', N'jonathan', @StaffPasswordHash, N'Jonathan John C. Parreño', N'jonathan@gensan.gov.ph', N'EMP-13064', 4, N'Real Property Appraisal & Assessment Division', N'09170013064', 1, 1, 3, N'Inspection')
        ,('33fce2b5-cad9-e02c-a3f1-36902a338a5e', N'gian', @StaffPasswordHash, N'Gian Nicolai B. Fabrigas', N'gian@gensan.gov.ph', N'EMP-13065', 4, N'Real Property Appraisal & Assessment Division', N'09170013065', 1, 1, 3, N'Inspection')
        ,('48f4445b-66ec-5dbb-7163-132ae74f3aef', N'pauline', @StaffPasswordHash, N'Pauline Anne Grace R. Cruz', N'pauline@gensan.gov.ph', N'EMP-13066', 0, N'Real Property Appraisal & Assessment Division', N'09170013066', 1, 1, 4, N'Check and Review')
        ,('c13e8ffe-5860-a4dd-30e1-2b58a4979135', N'mark john', @StaffPasswordHash, N'Mark John I. Bagaforo', N'markjohn@gensan.gov.ph', N'EMP-13067', 2, N'Real Property Appraisal & Assessment Division', N'09170013067', 1, 1, 10, N'Releasing')
        ,('5fb6edef-7b5f-c12f-6e36-43e908f060ed', N'jekiriz', @StaffPasswordHash, N'Jekiriz B. Cerna', N'jekiriz@gensan.gov.ph', N'EMP-13068', 2, N'Real Property Appraisal & Assessment Division', N'09170013068', 1, 1, 4, N'Check and Review')
        ,('7e55e3ce-85c4-47b4-2761-f9abb6291e61', N'marlon', @StaffPasswordHash, N'Marlon A. Cornejo', N'marlon@gensan.gov.ph', N'EMP-13069', 2, N'Real Property Appraisal & Assessment Division', N'09170013069', 1, 1, 5, N'Repinning')
        ,('6e8965f1-190e-fc38-7e35-02ad98a1d5ba', N'ramz', @StaffPasswordHash, N'Ramizel L. Perania', N'ramz@gensan.gov.ph', N'EMP-13070', 5, N'Real Property Appraisal & Assessment Division', N'09170013070', 1, 1, 6, N'Recommending Approval')
        ,('f87fb95d-b528-0a6f-22ea-d039eb6979a6', N'rhea', @StaffPasswordHash, N'Maria Rhea Connie R. Apostol', N'rhea@gensan.gov.ph', N'EMP-13071', 0, N'Real Property Appraisal & Assessment Division', N'09170013071', 1, 1, 7, N'Valuation & Examination')
        ,('92fd4825-b1ef-baf0-9b19-47c7b3686431', N'xyrene', @StaffPasswordHash, N'Xyrene O. Caseres', N'xyrene@gensan.gov.ph', N'EMP-13072', 4, N'GIS Spatial & Tax Mapping Section', N'09170013072', 1, 2, 15, N'Receiving (GIS)')
        ,('d80f4100-5bec-a17c-dc69-a91dd6ec1bc0', N'kim', @StaffPasswordHash, N'Kim Dived E. Jabagat', N'kim@gensan.gov.ph', N'EMP-13073', 4, N'GIS Spatial & Tax Mapping Section', N'09170013073', 1, 2, 16, N'Replotting (GIS)')
        ,('952261d0-8508-744d-cc00-470a2e56a80f', N'care', @StaffPasswordHash, N'Care May B. Divinagracia', N'care@gensan.gov.ph', N'EMP-13074', 5, N'GIS Spatial & Tax Mapping Section', N'09170013074', 1, 2, 17, N'Preparation (GIS)')
        ,('846dfac1-09bb-1d32-fb0d-6e589967e339', N'mhilla', @StaffPasswordHash, N'Mhilla A. Beltran', N'mhilla@gensan.gov.ph', N'EMP-13075', 5, N'GIS Spatial & Tax Mapping Section', N'09170013075', 1, 2, 16, N'Replotting (GIS)')
        ,('a76c77fb-dd2d-4c0c-a7a1-733b920c5d66', N'marlene', @StaffPasswordHash, N'Marlene Rendaje', N'marlene@gensan.gov.ph', N'EMP-13076', 4, N'GIS Spatial & Tax Mapping Section', N'09170013076', 1, 2, 16, N'Replotting (GIS)')
        ,('ee3e8aed-764d-04a7-83a4-9e1617d9301a', N'charisse', @StaffPasswordHash, N'Charisse B. Pagay', N'charisse@gensan.gov.ph', N'EMP-13077', 4, N'GIS Spatial & Tax Mapping Section', N'09170013077', 1, 2, 18, N'Approval (GIS)')
        ,('0855a872-44b2-24d5-ccf8-558b5026f3dd', N'xian', @StaffPasswordHash, N'Christian Jeed Bade', N'xian@gensan.gov.ph', N'EMP-13078', 4, N'GIS Spatial & Tax Mapping Section', N'09170013078', 1, 2, 16, N'Replotting (GIS)')
        ,('89200c75-90d2-e962-862e-40d8b9816029', N'jobert', @StaffPasswordHash, N'Jobert Dellava', N'jobert@gensan.gov.ph', N'EMP-13079', 0, N'GIS Spatial & Tax Mapping Section', N'09170013079', 1, 2, 15, N'Receiving (GIS)')
        ,('3f615196-8130-338e-c63a-8c7f67b49ec4', N'ellen', @StaffPasswordHash, N'Ellen', N'ellen@gensan.gov.ph', N'EMP-13080', 4, N'GIS Spatial & Tax Mapping Section', N'09170013080', 1, 2, 19, N'Releasing (GIS)')
        ,('f2f7df57-4360-e664-dcc1-c9c3db0f512f', N'rose', @StaffPasswordHash, N'Rose Marie Bade', N'rose@gensan.gov.ph', N'EMP-14078', 0, N'GIS Spatial & Tax Mapping Section', N'09170014078', 1, 2, 17, N'Preparation (GIS)')
        ,('04c0bfa6-733f-c6fb-2e11-e8c179952647', N'joy', @StaffPasswordHash, N'joy', N'joy@gensan.gov.ph', N'EMP-14079', 5, N'Records & Certification Section', N'09170014079', 1, 3, 28, N'Verification (Records)')
        ,('62c40be9-d923-f9b0-9a8a-6568bd975c81', N'kyle', @StaffPasswordHash, N'Kyle', N'kyle@gensan.gov.ph', N'EMP-14080', 4, N'Records & Certification Section', N'09170014080', 1, 3, 29, N'Approval (Records)')
        ,('9e34d359-19c0-36f4-8b28-abebcf0efd8e', N'judy', @StaffPasswordHash, N'judy', N'judy@gensan.gov.ph', N'EMP-14081', 5, N'Records & Certification Section', N'09170014081', 1, 3, 30, N'Releasing (Records)')
        ,('16dcb85f-a740-6108-cdf1-a4f57eded206', N'nene', @StaffPasswordHash, N'nene', N'nene@gensan.gov.ph', N'EMP-14082', 4, N'GIS Spatial & Tax Mapping Section', N'09170014082', 1, 2, 17, N'Preparation (GIS)')
        ,('2dc999d5-8020-2096-f118-0da9cadbe9cd', N'grace', @StaffPasswordHash, N'grace', N'grace@gensan.gov.ph', N'EMP-15078', 4, N'Records & Certification Section', N'09170015078', 1, 3, 30, N'Releasing (Records)')
        ,('5f863e45-530d-4618-8ade-e0fb092c8a74', N'ayan', @StaffPasswordHash, N'ayan', N'ayan@gensan.gov.ph', N'EMP-16078', 5, N'Real Property Appraisal & Assessment Division', N'09170016078', 1, 1, 2, N'Replotting')
        ,('339870c4-44b1-abaf-a3e8-5dee376d2186', N'rolenber', @StaffPasswordHash, N'rolenber', N'rolenber@gensan.gov.ph', N'EMP-16079', 5, N'Real Property Appraisal & Assessment Division', N'09170016079', 1, 1, 7, N'Valuation & Examination')
        ,('dc2a0350-e1f7-b54b-8ff4-0e5c9b64139a', N'dodong', @StaffPasswordHash, N'dodong', N'dodong@gensan.gov.ph', N'EMP-16080', 1, N'Real Property Appraisal & Assessment Division', N'09170016080', 1, 1, 8, N'Approval')
        ,('2ba9fde2-df2e-04f3-a929-7cd2642152b1', N'printing', @StaffPasswordHash, N'printing', N'printing@gensan.gov.ph', N'EMP-16081', 5, N'Real Property Appraisal & Assessment Division', N'09170016081', 1, 1, 9, N'Printing')
        ,('823060ec-7fd5-c19b-53d0-a086b509628c', N'rc1', @StaffPasswordHash, N'rc1', N'rc1@gensan.gov.ph', N'EMP-17078', 2, N'Real Property Appraisal & Assessment Division', N'09170017078', 1, 1, 1, N'Receiving')
        ,('e4dfb9fe-5d7d-f1aa-c71b-861e77865926', N'markq', @StaffPasswordHash, N'MarkQ', N'markq@gensan.gov.ph', N'EMP-17079', 4, N'Real Property Appraisal & Assessment Division', N'09170017079', 1, 1, 3, N'Inspection')
        ,('3da6fb91-7b6f-bae6-34f8-d9a15667c707', N'ella', @StaffPasswordHash, N'ella', N'ella@gensan.gov.ph', N'EMP-17080', 5, N'Real Property Appraisal & Assessment Division', N'09170017080', 1, 1, 2, N'Replotting')
        ,('b4aa412c-14ed-6785-a754-1f61a2c117d4', N'jerald', @StaffPasswordHash, N'jerald', N'jerald@gensan.gov.ph', N'EMP-17081', 4, N'Real Property Appraisal & Assessment Division', N'09170017081', 1, 1, 6, N'Recommending Approval')
        ,('8e353d75-beff-d058-3481-40dfa3b73e60', N'christine', @StaffPasswordHash, N'christine', N'christine@gensan.gov.ph', N'EMP-17082', 4, N'Real Property Appraisal & Assessment Division', N'09170017082', 1, 1, 7, N'Valuation & Examination')
        ,('c77e150d-d561-7795-b36b-f45a455bd589', N'judie', @StaffPasswordHash, N'judie', N'judie@gensan.gov.ph', N'EMP-17083', 1, N'Real Property Appraisal & Assessment Division', N'09170017083', 1, 1, 8, N'Approval')
        ,('ad1335bd-db38-afca-4ef3-52f9e4f6b815', N'anne', @StaffPasswordHash, N'Anne', N'anne@gensan.gov.ph', N'EMP-17084', 5, N'Real Property Appraisal & Assessment Division', N'09170017084', 1, 1, 9, N'Printing')
        ,('6050e828-a1da-dcd0-94e6-79a74b255901', N'rolen', @StaffPasswordHash, N'rolen', N'rolen@gensan.gov.ph', N'EMP-17085', 5, N'Real Property Appraisal & Assessment Division', N'09170017085', 1, 1, 4, N'Check and Review')
        ,('f06b3ddb-0697-2abc-3fad-a34631bcc463', N'rolen1', @StaffPasswordHash, N'rolen1', N'rolen1@gensan.gov.ph', N'EMP-17086', 5, N'GIS Spatial & Tax Mapping Section', N'09170017086', 1, 2, 18, N'Approval (GIS)')
) AS Source (
    [Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [DivisionId], [WorkflowRoleId], [WorkflowRoleName]
)
ON Target.[Username] = Source.[Username]
WHEN MATCHED THEN
    UPDATE SET
        Target.[FullName] = Source.[FullName],
        Target.[Email] = Source.[Email],
        Target.[Role] = Source.[Role],
        Target.[Department] = Source.[Department],
        Target.[IsActive] = Source.[IsActive],
        Target.[DivisionId] = Source.[DivisionId],
        Target.[WorkflowRoleId] = Source.[WorkflowRoleId],
        Target.[WorkflowRoleName] = Source.[WorkflowRoleName]
WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate], [LastLoginDate], [DivisionId], [WorkflowRoleId], [WorkflowRoleName]
    )
    VALUES (
        Source.[Id], Source.[Username], Source.[PasswordHash], Source.[FullName], Source.[Email], Source.[EmployeeNo], Source.[Role], Source.[Department], Source.[ContactNo], Source.[IsActive], GETDATE(), NULL, Source.[DivisionId], Source.[WorkflowRoleId], Source.[WorkflowRoleName]
    );

PRINT 'Successfully seeded 55 CAST staff user accounts into [dbo].[UserAccounts]!';
GO

-- 3. Verification & Summary Output
SELECT
    COUNT(*) AS [TotalUserAccounts],
    SUM(CASE WHEN DivisionId = 1 THEN 1 ELSE 0 END) AS [Division1_Appraisal],
    SUM(CASE WHEN DivisionId = 2 THEN 1 ELSE 0 END) AS [Division2_TaxMapping],
    SUM(CASE WHEN DivisionId = 3 THEN 1 ELSE 0 END) AS [Division3_Records],
    SUM(CASE WHEN DivisionId IS NULL THEN 1 ELSE 0 END) AS [SuperAdmins],
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS [ActiveUsers],
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS [InactiveUsers]
FROM [dbo].[UserAccounts];
GO

GO


-- ==========================================================================================
-- SECTION: 09_SeedBuildingClassifications.sql
-- ==========================================================================================
PRINT 'Executing section: 09_SeedBuildingClassifications.sql...';
GO
-- =========================================================================
-- SCRIPT: 09_SeedBuildingClassifications.sql
-- DESCRIPTION: Sets up and seeds the default reference Building Classifications
--              table ([dbo].[BuildingClassifications]) based on the CAST 
--              Property Appraisal System standards.
--              Includes Structural BUCC classes, Residential, Commercial,
--              Industrial, Institutional, Special, and Agricultural types.
-- DATABASE: FairTrackDB
-- DATE: 2026-10-26
-- =========================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Building Classifications Setup & Default Seeding';
PRINT '=========================================================================';

BEGIN TRANSACTION;

BEGIN TRY
    -- ---------------------------------------------------------------------
    -- 1. Ensure Table Exists
    -- ---------------------------------------------------------------------
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BuildingClassifications' AND schema_id = SCHEMA_ID('dbo'))
    BEGIN
        PRINT 'Creating table [dbo].[BuildingClassifications]...';
        CREATE TABLE [dbo].[BuildingClassifications] (
            [Id] INT IDENTITY(1,1) NOT NULL,
            [ClassificationCode] NVARCHAR(50) NOT NULL,
            [ClassificationName] NVARCHAR(250) NOT NULL,
            [Description] NVARCHAR(MAX) NULL,
            [StructuralTypeDefault] NVARCHAR(MAX) NULL,
            [DefaultUnitConstructionCost] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
            [IsActive] BIT NOT NULL DEFAULT 1,
            [SortOrder] INT NOT NULL DEFAULT 0,
            CONSTRAINT [PK_BuildingClassifications] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Table [dbo].[BuildingClassifications] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [dbo].[BuildingClassifications] already exists.';
    END

    -- ---------------------------------------------------------------------
    -- 2. Define Reference Building Classifications (CAST Standard)
    -- ---------------------------------------------------------------------
    DECLARE @ClassTable TABLE (
        [ClassificationCode] NVARCHAR(50),
        [ClassificationName] NVARCHAR(250),
        [Description] NVARCHAR(MAX),
        [StructuralTypeDefault] NVARCHAR(MAX),
        [DefaultUnitConstructionCost] DECIMAL(18,2),
        [IsActive] BIT,
        [SortOrder] INT
    );

    INSERT INTO @ClassTable ([ClassificationCode], [ClassificationName], [Description], [StructuralTypeDefault], [DefaultUnitConstructionCost], [IsActive], [SortOrder])
    VALUES
        (N'I-A',    N'Class A - Reinforced Concrete',                      N'Reinforced Concrete Frame (RC Columns, Beams, Slabs & Walls)',        N'Type I',   15000.00, 1, 1),
        (N'I-B',    N'Class B - Semi-Concrete / Hollow Blocks',            N'Reinforced Concrete Columns & Beams with CHB Exterior Walls',         N'Type II',  12500.00, 1, 2),
        (N'II-A',   N'Class C - Reinforced Concrete & Wood',               N'Concrete Ground Floor with Timber / Wood Upper Floors',                N'Type III',  9800.00, 1, 3),
        (N'III-A',  N'Class D - Wood / Timber Frame',                      N'Wood Columns, Girders, Timber Framing & Wood Flooring',                N'Type IV',   6500.00, 1, 4),
        (N'IV-A',   N'Class E - Light Materials / Mixed',                  N'Light G.I. Sheets, Bamboo, Sawali, Salvaged Materials',                N'Type V',    3500.00, 1, 5),
        (N'RES-01', N'Residential - One Family Dwelling',                  N'Single Family Detached Residential Building',                          N'Type I',   14000.00, 1, 6),
        (N'RES-02', N'Residential - Two Family Dwelling',                  N'Duplex Residential Unit',                                              N'Type I',   13500.00, 1, 7),
        (N'RES-03', N'Residential - Multi-Family / Apartment',              N'Apartment Building, Rowhouse, Accessoria',                             N'Type I',   14500.00, 1, 8),
        (N'RES-04', N'Residential - Condominium Unit',                     N'High-Rise / Mid-Rise Residential Condominium',                         N'Type I',   22000.00, 1, 9),
        (N'COM-01', N'Commercial - Retail / Shopping Center',              N'Retail Stores, Shopping Mall, Supermarket',                            N'Type I',   18000.00, 1, 10),
        (N'COM-02', N'Commercial - Office Building / Bank',                N'Office Tower, Financial Center, Bank Building',                        N'Type I',   20000.00, 1, 11),
        (N'COM-03', N'Commercial - Hotel / Motel / Inn',                   N'Hospitality Building, Hotel, Resort Accommodation',                    N'Type I',   24000.00, 1, 12),
        (N'IND-01', N'Industrial - Factory / Manufacturing Plant',         N'Industrial Production and Manufacturing Complex',                       N'Type III', 16000.00, 1, 13),
        (N'IND-02', N'Industrial - Warehouse / Storage Facility',          N'Storage Bodega, Logistics Center, Cold Storage',                       N'Type III', 11000.00, 1, 14),
        (N'INS-01', N'Institutional - School / University',                N'Educational Classrooms, Campus Laboratories',                          N'Type I',   15500.00, 1, 15),
        (N'INS-02', N'Institutional - Hospital / Medical Clinic',          N'Hospital, Clinical Laboratory, Health Facility',                       N'Type I',   25000.00, 1, 16),
        (N'SPE-01', N'Special - Gymnasium / Coliseum / Arena',             N'Sports Coliseum, Cultural Theater, Auditorium',                        N'Type I',   21000.00, 1, 17),
        (N'AGR-01', N'Agricultural - Farm Shed / Livestock Facility',      N'Poultry House, Piggery Pen, Farm Machinery Shed',                      N'Type IV',   4500.00, 1, 18);

    -- ---------------------------------------------------------------------
    -- 3. Idempotent Upsert / Merge into [dbo].[BuildingClassifications]
    -- ---------------------------------------------------------------------
    DECLARE @InsertedCount INT = 0;
    DECLARE @UpdatedCount INT = 0;

    MERGE INTO [dbo].[BuildingClassifications] AS target
    USING @ClassTable AS source
    ON (target.[ClassificationCode] = source.[ClassificationCode])
    WHEN MATCHED THEN
        UPDATE SET
            target.[ClassificationName]          = source.[ClassificationName],
            target.[Description]                 = source.[Description],
            target.[StructuralTypeDefault]       = source.[StructuralTypeDefault],
            target.[DefaultUnitConstructionCost] = source.[DefaultUnitConstructionCost],
            target.[IsActive]                    = source.[IsActive],
            target.[SortOrder]                   = source.[SortOrder]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ClassificationCode], [ClassificationName], [Description], [StructuralTypeDefault], [DefaultUnitConstructionCost], [IsActive], [SortOrder])
        VALUES (source.[ClassificationCode], source.[ClassificationName], source.[Description], source.[StructuralTypeDefault], source.[DefaultUnitConstructionCost], source.[IsActive], source.[SortOrder]);

    PRINT 'Building Classifications upsert completed successfully.';

    COMMIT TRANSACTION;

    PRINT '=========================================================================';
    PRINT 'Summary of [dbo].[BuildingClassifications]:';
    SELECT [Id], [ClassificationCode], [ClassificationName], [StructuralTypeDefault], [DefaultUnitConstructionCost], [SortOrder]
    FROM [dbo].[BuildingClassifications]
    ORDER BY [SortOrder] ASC;
    PRINT '=========================================================================';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT 'ERROR encountered: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

GO


-- ==========================================================================================
-- SECTION: 10_SeedBuildingAdditionalItems.sql
-- ==========================================================================================
PRINT 'Executing section: 10_SeedBuildingAdditionalItems.sql...';
GO
-- ==============================================================================
-- Seed Script: Standard Building Additional Items (CAST System Alignment)
-- Description: Inserts standard additional items / improvements reference data (52 items)
-- Target Table: dbo.BuildingAdditionalItems
-- ==============================================================================

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.BuildingAdditionalItems', N'U') IS NOT NULL
BEGIN
    PRINT 'Seeding reference data into dbo.BuildingAdditionalItems...';

    SET IDENTITY_INSERT [dbo].[BuildingAdditionalItems] ON;

    MERGE INTO [dbo].[BuildingAdditionalItems] AS target
    USING (VALUES
    (1, N'CARPORT', 30.00, 0.00, 0.00, 0.00, 2011, 1, 1),
    (2, N'MEZZANINE', 60.00, 0.00, 0.00, 0.00, 2011, 1, 2),
    (3, N'PORCH', 40.00, 0.00, 0.00, 0.00, 2011, 1, 3),
    (4, N'BALCONY', 45.00, 0.00, 0.00, 0.00, 2011, 1, 4),
    (5, N'GARAGE', 45.00, 0.00, 0.00, 0.00, 2011, 1, 5),
    (6, N'TERRACE : COVERED', 40.00, 0.00, 0.00, 0.00, 2011, 1, 6),
    (7, N'TERRACE : OPEN', 20.00, 0.00, 0.00, 0.00, 2011, 1, 7),
    (8, N'DECK ROOF : COVERED', 40.00, 0.00, 0.00, 0.00, 2011, 1, 8),
    (9, N'DECK ROOF : OPEN', 20.00, 0.00, 0.00, 0.00, 2011, 1, 9),
    (10, N'BASEMENT : RESIDENTIAL', 70.00, 0.00, 0.00, 0.00, 2011, 1, 10),
    (11, N'PAVEMENTS : TENNIS COURT', 0.00, 75.00, 0.00, 0.00, 2011, 1, 11),
    (12, N'PAVEMENTS : CONCRETE 5 CM. THICK', 0.00, 75.00, 0.00, 0.00, 2011, 1, 12),
    (13, N'PAVEMENTS : CONCRETE 10 CM. THICK', 0.00, 150.00, 0.00, 0.00, 2011, 1, 13),
    (14, N'PAVEMENTS :CONCRETE 15 CM. THICK', 0.00, 225.00, 0.00, 0.00, 2011, 1, 14),
    (15, N'PAVEMENTS : CONCRETE 20 CM. THICK', 0.00, 300.00, 0.00, 0.00, 2011, 1, 15),
    (16, N'PAVEMENTS : ASPHALT 1 COURSE', 0.00, 120.00, 0.00, 0.00, 2011, 1, 16),
    (17, N'PAVEMENTS : ASPHALT 2 COURSE', 0.00, 150.00, 0.00, 0.00, 2011, 1, 17),
    (18, N'PAVEMENTS : ASPHALT 3 COURSE', 0.00, 180.00, 0.00, 0.00, 2011, 1, 18),
    (19, N'FLOOR FINISHES : GRANITE TILES', 0.00, 1000.00, 0.00, 0.00, 2011, 1, 19),
    (20, N'FLOOR FINISHES : MARBLE SLABS', 0.00, 700.00, 0.00, 0.00, 2011, 1, 20),
    (21, N'FLOOR FINISHES : MARBLE TILES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 21),
    (22, N'FLOOR FINISHES : CERAMIC TILES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 22),
    (23, N'FLOOR FINISHES : CRAZY CUT MARBLES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 23),
    (24, N'FLOOR FINISHES : GRANOLITHIC', 0.00, 400.00, 0.00, 0.00, 2011, 1, 24),
    (25, N'FLOOR FINISHES : NARRA', 0.00, 600.00, 0.00, 0.00, 2011, 1, 25),
    (26, N'FLOOR FINISHES : YAKAL', 0.00, 500.00, 0.00, 0.00, 2011, 1, 26),
    (27, N'FLOOR FINISHES : NARRA/FANCY WOOD TILES', 0.00, 400.00, 0.00, 0.00, 2011, 1, 27),
    (28, N'FLOOR FINISHES : ORDINARY WOOD TILES', 0.00, 400.00, 0.00, 0.00, 2011, 1, 28),
    (29, N'FLOOR FINISHES : VINYL TILES', 0.00, 300.00, 0.00, 0.00, 2011, 1, 29),
    (30, N'FLOOR FINISHES : WASHOUT PEEBLES', 0.00, 100.00, 0.00, 0.00, 2011, 1, 30),
    (31, N'FLOOR FINISHES : UNGLAZED TILES', 0.00, 300.00, 0.00, 0.00, 2011, 1, 31),
    (33, N'WALLING : DOUBLE WALLING (ORDINARY PLYWOOD)', 0.00, 200.00, 0.00, 0.00, 2011, 1, 33),
    (34, N'WALLING : DOUBLE WALLING (NARRA PANELING)', 0.00, 300.00, 0.00, 0.00, 2011, 1, 34),
    (35, N'WALLING : GLAZED WHITE TILES', 0.00, 400.00, 0.00, 0.00, 2011, 1, 35),
    (36, N'WALLING : GLAZED COLORED TILES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 36),
    (37, N'WALLING : FANCY TILES', 0.00, 200.00, 0.00, 0.00, 2011, 1, 37),
    (38, N'WALLING : SYNTHETIC RUBLE', 0.00, 100.00, 0.00, 0.00, 2011, 1, 38),
    (39, N'WALLING : BRICKS', 0.00, 100.00, 0.00, 0.00, 2011, 1, 39),
    (40, N'SPECIAL PANELS : GLASS WITH WOODEN FRAMES', 0.00, 400.00, 0.00, 0.00, 2011, 1, 40),
    (41, N'SPECIAL PANELS : GLASS WITH ALUMINUM FRAMES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 41),
    (42, N'CEILINGS BELOW CONCRETE FLOOR : ORDINARY PLYWOOD', 0.00, 200.00, 0.00, 0.00, 2011, 1, 42),
    (43, N'CEILINGS BELOW CONCRETE FLOOR : LUMINOUS CEILING', 0.00, 500.00, 0.00, 0.00, 2011, 1, 43),
    (44, N'CEILINGS BELOW CONCRETE FLOOR : ACOUSTIC', 0.00, 400.00, 0.00, 0.00, 2011, 1, 44),
    (45, N'CEILINGS BELOW CONCRETE FLOOR : SPECIAL FINISH', 0.00, 300.00, 0.00, 0.00, 2011, 1, 45),
    (46, N'GATES, GRILLS AND WINDOW GRILLS : STEEL GATES W/GRILLS (SPECIAL DESIGNS)', 0.00, 1000.00, 0.00, 0.00, 2011, 1, 46),
    (47, N'GATES, GRILLS AND WINDOW GRILLS : STEEL GRILLS W/GRILLS (W/O DESIGNS ORDINARY)', 0.00, 600.00, 0.00, 0.00, 2011, 1, 47),
    (48, N'GATES, GRILLS AND WINDOW GRILLS : WINDOW GRILLS', 0.00, 400.00, 0.00, 0.00, 2011, 1, 48),
    (49, N'EXCESS HEIGHTS : RESIDENTIAL AND COMMERCIAL IN THE EXCESS OF 3 METERS', 10.00, 0.00, 0.00, 0.00, 2011, 1, 49),
    (50, N'EXCESS HEIGHTS : BODEGA AND FACTORY IN THE EXCESS OF 4.5 METERS', 10.00, 0.00, 0.00, 0.00, 2011, 1, 50),
    (51, N'EXTRA T % B - ORDINARY FINISH', 0.00, 10000.00, 0.00, 0.00, 2011, 1, 51),
    (52, N'PAINTING', 10.00, 0.00, 0.00, 0.00, 2011, 1, 52),
    (53, N'SECOND HAND MATERIALS', -10.00, 0.00, 0.00, 0.00, 2011, 1, 53)
    ) AS source (
        [Id], [AdditionalItemDescription], [PercentUnitValue], [CostPerSQM],
        [ExcessSQM], [ExcessPercentage], [RevisionYear], [IsActive], [SortOrder]
    )
    ON target.[Id] = source.[Id]
    WHEN MATCHED THEN
        UPDATE SET
            target.[AdditionalItemDescription] = source.[AdditionalItemDescription],
            target.[PercentUnitValue] = source.[PercentUnitValue],
            target.[CostPerSQM] = source.[CostPerSQM],
            target.[ExcessSQM] = source.[ExcessSQM],
            target.[ExcessPercentage] = source.[ExcessPercentage],
            target.[RevisionYear] = source.[RevisionYear],
            target.[IsActive] = source.[IsActive],
            target.[SortOrder] = source.[SortOrder]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [AdditionalItemDescription], [PercentUnitValue], [CostPerSQM], [ExcessSQM], [ExcessPercentage], [RevisionYear], [IsActive], [SortOrder])
        VALUES (source.[Id], source.[AdditionalItemDescription], source.[PercentUnitValue], source.[CostPerSQM], source.[ExcessSQM], source.[ExcessPercentage], source.[RevisionYear], source.[IsActive], source.[SortOrder]);

    SET IDENTITY_INSERT [dbo].[BuildingAdditionalItems] OFF;

    PRINT 'dbo.BuildingAdditionalItems seeding complete. Total reference items seeded: 52.';
END
ELSE
BEGIN
    PRINT 'Table dbo.BuildingAdditionalItems does not exist yet. Please run EF Core migration first.';
END
GO

GO


-- ==========================================================================================
-- SECTION: 11_SeedBuildingAssessmentDepreciationAndRates.sql
-- ==========================================================================================
PRINT 'Executing section: 11_SeedBuildingAssessmentDepreciationAndRates.sql...';
GO
-- ==========================================================================================
-- Script: 11_SeedBuildingAssessmentDepreciationAndRates.sql
-- Description: Seeds Building Valuation Reference Tables aligned with CAST Reference System
-- Database: FairTrackDB
-- ==========================================================================================

SET NOCOUNT ON;

-- 1. Building Structural Types (13 rows)
IF NOT EXISTS (SELECT 1 FROM [dbo].[BuildingStructuralTypes])
BEGIN
    SET IDENTITY_INSERT [dbo].[BuildingStructuralTypes] ON;
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (1, N'I-A', N'REINFORCED CONCRETE - A')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (2, N'I-B', N'REINFORCED CONCRETE - B')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (3, N'I-C', N'REINFORCED CONCRETE - C')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (4, N'II-A', N'MIXED CONCRETE-A')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (5, N'II-B', N'MIXED CONCRETE - B')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (6, N'II-C', N'MIXED CONCRETE-C')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (7, N'III-A', N'WOOD-A')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (8, N'III-B', N'WOOD-B')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (9, N'III-C', N'WOOD-C')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (10, N'III-D', N'NATIVE')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (11, N'IV-A', N'LIGHT MATERIALS')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (12, N'X', N'BLANK')
    INSERT INTO [dbo].[BuildingStructuralTypes] ([Id], [StructuralCode], [StructuralDescription]) VALUES (13, N'II-D', N'MIXED CONCRETE -D')
    SET IDENTITY_INSERT [dbo].[BuildingStructuralTypes] OFF;
    PRINT 'BuildingStructuralTypes seeded successfully.';
END
ELSE
BEGIN
    PRINT 'BuildingStructuralTypes already populated.';
END
GO

-- 2. Building Types / BUCC Classifications (25 rows)
IF NOT EXISTS (SELECT 1 FROM [dbo].[BuildingTypes])
BEGIN
    SET IDENTITY_INSERT [dbo].[BuildingTypes] ON;
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (1, N'(01)', N'ONE FAMILY DWELLING')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (2, N'(02)', N'TWO FAMILY DWELLING')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (3, N'(03)', N'MULTIPLE DWELLING')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (4, N'(04)', N'ROW HOUSE ACCESSORIES')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (5, N'(05)', N'APARTMENT')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (6, N'(06)', N'BOARDING HOUSE')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (7, N'(07)', N'LODGING HOUSE')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (8, N'(08)', N'TWO STOREY APARTMENT')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (9, N'(09)', N'TWO STOREY BOARDING HOUSE')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (10, N'(10)', N'TWO STOREY LODGING HOUSE')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (11, N'(11)', N'GARAGE QUARTER/GUARD HOUSE')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (12, N'(12)', N'CHURCH ASSEMBLY')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (13, N'(13)', N'SCHOOL BUILDING')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (14, N'(14)', N'HOSPITAL & HOTEL CONDOMINIUM')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (15, N'(15)', N'THEATER')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (16, N'(16)', N'FACTORY WAREHOUSE/BODEGA STORAGE/HANGARS')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (17, N'(17)', N'BANKS AND OFFICE BUILDING')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (18, N'(18)', N'MALLS AND SUPER MALLS')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (19, N'(19)', N'MARKET AND SHOPPING CENTER')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (20, N'(20)', N'GYMNASIUM COLISEUM')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (21, N'(21)', N'RECREATIONAL HALL/BOWLING LANE/CLUB HOUSE/PELOTA/COURT')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (22, N'(22)', N'SAW MILLS LUMBER SHEDS/PEGERY & POULTRY SHEDS')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (23, N'(23)', N'GASOLINE STATION')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (24, N'(24)', N'SWIMMING POOL')
    INSERT INTO [dbo].[BuildingTypes] ([Id], [BuildingTypeCode], [BuildingTypeDescription]) VALUES (25, N'(25)', N'FENCE FIRE AND WALL')
    SET IDENTITY_INSERT [dbo].[BuildingTypes] OFF;
    PRINT 'BuildingTypes seeded successfully.';
END
ELSE
BEGIN
    PRINT 'BuildingTypes already populated.';
END
GO

-- 3. Building Structure Unit Construction Cost Rates (204 rows)
IF NOT EXISTS (SELECT 1 FROM [dbo].[BuildingStructureRates])
BEGIN
    SET IDENTITY_INSERT [dbo].[BuildingStructureRates] ON;
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (1, 1, 1, CAST(5980 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (2, 1, 2, CAST(6630 AS Decimal(18, 0)), CAST(7020 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (3, 1, 4, CAST(6500 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (4, 1, 5, CAST(6500 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (5, 1, 3, CAST(6630 AS Decimal(18, 0)), CAST(7020 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (6, 1, 6, CAST(6500 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (7, 1, 7, CAST(6500 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (8, 1, 8, CAST(7150 AS Decimal(18, 0)), CAST(7670 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (9, 1, 9, CAST(7150 AS Decimal(18, 0)), CAST(7670 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (10, 1, 10, CAST(7150 AS Decimal(18, 0)), CAST(7670 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (11, 1, 11, CAST(2990 AS Decimal(18, 0)), CAST(3250 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (12, 1, 12, CAST(4810 AS Decimal(18, 0)), CAST(5200 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (13, 1, 13, CAST(4810 AS Decimal(18, 0)), CAST(5200 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (14, 1, 14, CAST(6630 AS Decimal(18, 0)), CAST(7020 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (15, 1, 15, CAST(4940 AS Decimal(18, 0)), CAST(5460 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (16, 1, 16, CAST(4810 AS Decimal(18, 0)), CAST(5200 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (17, 1, 17, CAST(6890 AS Decimal(18, 0)), CAST(7540 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (18, 1, 18, CAST(11050 AS Decimal(18, 0)), CAST(13000 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (19, 1, 19, CAST(6500 AS Decimal(18, 0)), CAST(7150 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (20, 1, 20, CAST(4550 AS Decimal(18, 0)), CAST(5070 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (21, 1, 21, CAST(4550 AS Decimal(18, 0)), CAST(5330 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (22, 1, 22, CAST(4290 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (23, 1, 23, CAST(2990 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (24, 1, 24, CAST(5330 AS Decimal(18, 0)), CAST(5980 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (25, 1, 25, CAST(1560 AS Decimal(18, 0)), CAST(2080 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (26, 2, 1, CAST(5460 AS Decimal(18, 0)), CAST(5980 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (27, 2, 2, CAST(6240 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (28, 2, 3, CAST(6240 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (29, 2, 4, CAST(2600 AS Decimal(18, 0)), CAST(2990 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (30, 2, 5, CAST(6240 AS Decimal(18, 0)), CAST(6500 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (31, 2, 6, CAST(6240 AS Decimal(18, 0)), CAST(6500 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (32, 2, 7, CAST(6240 AS Decimal(18, 0)), CAST(6500 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (33, 2, 8, CAST(6630 AS Decimal(18, 0)), CAST(7150 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (34, 2, 9, CAST(6630 AS Decimal(18, 0)), CAST(7150 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (35, 2, 10, CAST(6630 AS Decimal(18, 0)), CAST(7150 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (36, 2, 11, CAST(2600 AS Decimal(18, 0)), CAST(2990 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (37, 2, 12, CAST(4420 AS Decimal(18, 0)), CAST(4810 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (38, 2, 13, CAST(4420 AS Decimal(18, 0)), CAST(4810 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (39, 2, 14, CAST(6240 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (40, 2, 15, CAST(5070 AS Decimal(18, 0)), CAST(4940 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (41, 2, 16, CAST(4550 AS Decimal(18, 0)), CAST(4810 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (42, 2, 17, CAST(6500 AS Decimal(18, 0)), CAST(6890 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (43, 2, 18, CAST(9100 AS Decimal(18, 0)), CAST(11050 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (44, 2, 19, CAST(6110 AS Decimal(18, 0)), CAST(6500 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (45, 2, 20, CAST(4160 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (46, 2, 21, CAST(4160 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (47, 2, 22, CAST(4030 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (48, 2, 23, CAST(2730 AS Decimal(18, 0)), CAST(2990 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (49, 2, 24, CAST(4090 AS Decimal(18, 0)), CAST(5330 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (50, 2, 25, CAST(1040 AS Decimal(18, 0)), CAST(1560 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (51, 3, 1, CAST(4810 AS Decimal(18, 0)), CAST(5460 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (52, 3, 2, CAST(5980 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (53, 3, 3, CAST(5980 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (54, 3, 4, CAST(2210 AS Decimal(18, 0)), CAST(2600 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (55, 3, 5, CAST(5980 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (56, 3, 6, CAST(5980 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (57, 3, 7, CAST(5980 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (58, 3, 8, CAST(6240 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (59, 3, 9, CAST(6240 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (60, 3, 10, CAST(6249 AS Decimal(18, 0)), CAST(6630 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (61, 3, 11, CAST(2210 AS Decimal(18, 0)), CAST(2600 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (62, 3, 12, CAST(4030 AS Decimal(18, 0)), CAST(4420 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (63, 3, 13, CAST(4030 AS Decimal(18, 0)), CAST(4420 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (64, 3, 14, CAST(5980 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (65, 3, 15, CAST(4680 AS Decimal(18, 0)), CAST(5070 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (66, 3, 16, CAST(4160 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (67, 3, 17, CAST(6110 AS Decimal(18, 0)), CAST(6500 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (68, 3, 18, CAST(8450 AS Decimal(18, 0)), CAST(9100 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (69, 3, 19, CAST(5720 AS Decimal(18, 0)), CAST(6110 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (70, 3, 20, CAST(3770 AS Decimal(18, 0)), CAST(4160 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (71, 3, 21, CAST(3900 AS Decimal(18, 0)), CAST(4160 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (72, 3, 22, CAST(3770 AS Decimal(18, 0)), CAST(4030 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (73, 3, 23, CAST(2340 AS Decimal(18, 0)), CAST(2730 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (74, 3, 24, CAST(4550 AS Decimal(18, 0)), CAST(4940 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (75, 3, 25, CAST(520 AS Decimal(18, 0)), CAST(1040 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (76, 4, 1, CAST(4420 AS Decimal(18, 0)), CAST(4810 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (77, 4, 2, CAST(5330 AS Decimal(18, 0)), CAST(5980 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (78, 4, 3, CAST(5330 AS Decimal(18, 0)), CAST(5980 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (79, 4, 4, CAST(1820 AS Decimal(18, 0)), CAST(2210 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (80, 4, 5, CAST(5720 AS Decimal(18, 0)), CAST(5980 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (81, 4, 6, CAST(5720 AS Decimal(18, 0)), CAST(5980 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (82, 4, 7, CAST(5720 AS Decimal(18, 0)), CAST(5980 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (83, 4, 8, CAST(5720 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (84, 4, 9, CAST(5720 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (85, 4, 10, CAST(5720 AS Decimal(18, 0)), CAST(6240 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (86, 4, 11, CAST(1820 AS Decimal(18, 0)), CAST(2210 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (87, 4, 12, CAST(3770 AS Decimal(18, 0)), CAST(4030 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (88, 4, 13, CAST(3770 AS Decimal(18, 0)), CAST(4030 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (89, 4, 14, CAST(5330 AS Decimal(18, 0)), CAST(5980 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (90, 4, 15, CAST(3900 AS Decimal(18, 0)), CAST(4680 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (91, 4, 16, CAST(3900 AS Decimal(18, 0)), CAST(4160 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (92, 4, 17, CAST(5720 AS Decimal(18, 0)), CAST(6110 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (93, 4, 18, CAST(6500 AS Decimal(18, 0)), CAST(8580 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (94, 4, 19, CAST(5330 AS Decimal(18, 0)), CAST(5720 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (95, 4, 20, CAST(3510 AS Decimal(18, 0)), CAST(3770 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (96, 4, 21, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (97, 4, 22, CAST(3510 AS Decimal(18, 0)), CAST(3770 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (98, 4, 23, CAST(1950 AS Decimal(18, 0)), CAST(2340 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (99, 4, 24, CAST(4160 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (100, 5, 1, CAST(3900 AS Decimal(18, 0)), CAST(4420 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (101, 5, 2, CAST(4940 AS Decimal(18, 0)), CAST(5330 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (102, 5, 3, CAST(4940 AS Decimal(18, 0)), CAST(5330 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (103, 5, 4, CAST(1560 AS Decimal(18, 0)), CAST(1820 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (104, 5, 5, CAST(5460 AS Decimal(18, 0)), CAST(5720 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (105, 5, 6, CAST(5460 AS Decimal(18, 0)), CAST(5720 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (106, 5, 7, CAST(5460 AS Decimal(18, 0)), CAST(5720 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (107, 5, 8, CAST(5460 AS Decimal(18, 0)), CAST(5720 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (108, 5, 9, CAST(5460 AS Decimal(18, 0)), CAST(5720 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (109, 5, 10, CAST(5460 AS Decimal(18, 0)), CAST(5720 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (110, 5, 11, CAST(1560 AS Decimal(18, 0)), CAST(1820 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (111, 5, 12, CAST(3380 AS Decimal(18, 0)), CAST(3770 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (112, 5, 13, CAST(3380 AS Decimal(18, 0)), CAST(3770 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (113, 5, 14, CAST(4940 AS Decimal(18, 0)), CAST(5330 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (114, 5, 15, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (115, 5, 16, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (116, 5, 17, CAST(5590 AS Decimal(18, 0)), CAST(5720 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (117, 5, 18, CAST(5200 AS Decimal(18, 0)), CAST(7800 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (118, 5, 19, CAST(4940 AS Decimal(18, 0)), CAST(5330 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (119, 5, 20, CAST(3120 AS Decimal(18, 0)), CAST(3500 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (120, 5, 21, CAST(3250 AS Decimal(18, 0)), CAST(3510 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (121, 5, 22, CAST(3250 AS Decimal(18, 0)), CAST(3510 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (122, 5, 23, CAST(1690 AS Decimal(18, 0)), CAST(1950 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (123, 5, 24, CAST(3770 AS Decimal(18, 0)), CAST(4160 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (124, 6, 1, CAST(3610 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (125, 6, 2, CAST(4550 AS Decimal(18, 0)), CAST(4940 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (126, 6, 3, CAST(4550 AS Decimal(18, 0)), CAST(4940 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (127, 6, 4, CAST(1300 AS Decimal(18, 0)), CAST(1560 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (128, 6, 5, CAST(5070 AS Decimal(18, 0)), CAST(5460 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (129, 6, 6, CAST(5070 AS Decimal(18, 0)), CAST(5460 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (130, 6, 7, CAST(5070 AS Decimal(18, 0)), CAST(5460 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (131, 6, 8, CAST(5070 AS Decimal(18, 0)), CAST(5460 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (132, 6, 9, CAST(5070 AS Decimal(18, 0)), CAST(5460 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (133, 6, 10, CAST(5070 AS Decimal(18, 0)), CAST(5460 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (134, 6, 11, CAST(1300 AS Decimal(18, 0)), CAST(1560 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (135, 6, 12, CAST(2990 AS Decimal(18, 0)), CAST(3380 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (136, 6, 13, CAST(2990 AS Decimal(18, 0)), CAST(3380 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (137, 6, 14, CAST(4550 AS Decimal(18, 0)), CAST(4940 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (138, 6, 15, CAST(3120 AS Decimal(18, 0)), CAST(3510 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (139, 6, 16, CAST(3250 AS Decimal(18, 0)), CAST(3510 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (140, 6, 17, CAST(5330 AS Decimal(18, 0)), CAST(5590 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (141, 6, 19, CAST(4660 AS Decimal(18, 0)), CAST(4940 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (142, 6, 20, CAST(2860 AS Decimal(18, 0)), CAST(3120 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (143, 6, 21, CAST(2860 AS Decimal(18, 0)), CAST(3250 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (144, 6, 22, CAST(2990 AS Decimal(18, 0)), CAST(3250 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (145, 6, 23, CAST(1300 AS Decimal(18, 0)), CAST(1690 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (146, 6, 24, CAST(3250 AS Decimal(18, 0)), CAST(3770 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (147, 13, 16, CAST(2990 AS Decimal(18, 0)), CAST(3250 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (148, 13, 22, CAST(2730 AS Decimal(18, 0)), CAST(2990 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (149, 7, 1, CAST(3120 AS Decimal(18, 0)), CAST(3510 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (150, 7, 2, CAST(4160 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (151, 7, 3, CAST(4160 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (152, 7, 5, CAST(4680 AS Decimal(18, 0)), CAST(5070 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (153, 7, 6, CAST(4680 AS Decimal(18, 0)), CAST(5070 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (154, 7, 7, CAST(4680 AS Decimal(18, 0)), CAST(5070 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (155, 7, 8, CAST(4680 AS Decimal(18, 0)), CAST(5070 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (156, 7, 9, CAST(4680 AS Decimal(18, 0)), CAST(5070 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (157, 7, 10, CAST(4680 AS Decimal(18, 0)), CAST(5070 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (158, 7, 12, CAST(2600 AS Decimal(18, 0)), CAST(2990 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (159, 7, 13, CAST(2600 AS Decimal(18, 0)), CAST(2990 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (160, 7, 14, CAST(4290 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (161, 7, 15, CAST(2730 AS Decimal(18, 0)), CAST(3120 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (162, 7, 16, CAST(2600 AS Decimal(18, 0)), CAST(2990 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (163, 7, 17, CAST(4940 AS Decimal(18, 0)), CAST(5330 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (164, 7, 21, CAST(2470 AS Decimal(18, 0)), CAST(2850 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (165, 7, 22, CAST(2470 AS Decimal(18, 0)), CAST(2730 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (166, 8, 1, CAST(2860 AS Decimal(18, 0)), CAST(3120 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (167, 8, 2, CAST(3770 AS Decimal(18, 0)), CAST(4160 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (168, 8, 3, CAST(3770 AS Decimal(18, 0)), CAST(4160 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (169, 8, 5, CAST(4290 AS Decimal(18, 0)), CAST(4680 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (170, 8, 6, CAST(4290 AS Decimal(18, 0)), CAST(4680 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (171, 8, 7, CAST(4290 AS Decimal(18, 0)), CAST(4680 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (172, 8, 8, CAST(4290 AS Decimal(18, 0)), CAST(4680 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (173, 8, 9, CAST(4290 AS Decimal(18, 0)), CAST(4680 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (174, 8, 10, CAST(4290 AS Decimal(18, 0)), CAST(4680 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (175, 8, 14, CAST(3900 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (176, 8, 15, CAST(2080 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (177, 8, 16, CAST(2340 AS Decimal(18, 0)), CAST(2600 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (178, 8, 17, CAST(4550 AS Decimal(18, 0)), CAST(4940 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (179, 8, 21, CAST(2210 AS Decimal(18, 0)), CAST(2470 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (180, 8, 22, CAST(2210 AS Decimal(18, 0)), CAST(2470 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (181, 9, 1, CAST(2470 AS Decimal(18, 0)), CAST(2860 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (182, 9, 2, CAST(3510 AS Decimal(18, 0)), CAST(3770 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (183, 9, 5, CAST(3900 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (184, 9, 6, CAST(3900 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (185, 9, 7, CAST(3900 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (186, 9, 8, CAST(3900 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (187, 9, 9, CAST(3900 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (188, 9, 10, CAST(3900 AS Decimal(18, 0)), CAST(4290 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (189, 9, 14, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (190, 9, 15, CAST(1690 AS Decimal(18, 0)), CAST(2080 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (191, 9, 17, CAST(4160 AS Decimal(18, 0)), CAST(4550 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (192, 10, 1, CAST(2080 AS Decimal(18, 0)), CAST(2470 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (193, 10, 2, CAST(3250 AS Decimal(18, 0)), CAST(3510 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (194, 10, 5, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (195, 10, 6, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (196, 10, 7, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (197, 10, 8, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (198, 10, 9, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (199, 10, 10, CAST(3510 AS Decimal(18, 0)), CAST(3900 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (200, 10, 14, CAST(3120 AS Decimal(18, 0)), CAST(3510 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (201, 10, 15, CAST(1300 AS Decimal(18, 0)), CAST(1690 AS Decimal(18, 0)), 2011)
    INSERT INTO [dbo].[BuildingStructureRates] ([Id], [BuildingStructuralTypeId], [BuildingTypeId], [MinRate], [MaxRate], [RevisionYear]) VALUES (202, 10, 17, CAST(3770 AS Decimal(18, 0)), CAST(4160 AS Decimal(18, 0)), 2011)
    SET IDENTITY_INSERT [dbo].[BuildingStructureRates] OFF;
    PRINT 'BuildingStructureRates seeded successfully.';
END
ELSE
BEGIN
    PRINT 'BuildingStructureRates already populated.';
END
GO

-- 4. Building Depreciation Schedules (158 rows)
IF NOT EXISTS (SELECT 1 FROM [dbo].[BuildingDepreciations])
BEGIN
    SET IDENTITY_INSERT [dbo].[BuildingDepreciations] ON;
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (1, N'I-A', 1, N'EXCELLENT', 0, 2, CAST(2.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (2, N'I-A', 1, N'AVERAGE', 0, 2, CAST(2.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (3, N'I-A', 1, N'POOR', 0, 2, CAST(3.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (4, N'I-A', 1, N'EXCELLENT', 2, 5, CAST(4.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (5, N'I-A', 1, N'AVERAGE', 2, 5, CAST(4.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (6, N'I-A', 1, N'POOR', 2, 5, CAST(8.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (7, N'I-A', 1, N'EXCELLENT', 5, 8, CAST(7.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (8, N'I-A', 1, N'AVERAGE', 5, 8, CAST(8.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (9, N'I-A', 1, N'POOR', 5, 8, CAST(10.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (10, N'I-A', 1, N'EXCELLENT', 8, 12, CAST(10.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (11, N'I-A', 1, N'AVERAGE', 8, 12, CAST(13.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (12, N'I-A', 1, N'POOR', 8, 12, CAST(15.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (13, N'I-A', 1, N'EXCELLENT', 12, 16, CAST(14.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (14, N'I-A', 1, N'AVERAGE', 12, 16, CAST(17.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (15, N'I-A', 1, N'POOR', 12, 16, CAST(19.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (16, N'I-A', 1, N'EXCELLENT', 16, 20, CAST(17.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (17, N'I-A', 1, N'AVERAGE', 16, 20, CAST(21.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (18, N'I-A', 1, N'POOR', 16, 20, CAST(23.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (19, N'I-A', 1, N'EXCELLENT', 20, 25, CAST(20.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (20, N'I-A', 1, N'AVERAGE', 20, 25, CAST(24.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (21, N'I-A', 1, N'POOR', 20, 25, CAST(28.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (22, N'I-A', 1, N'EXCELLENT', 25, 30, CAST(24.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (23, N'I-A', 1, N'AVERAGE', 25, 30, CAST(27.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (24, N'I-A', 1, N'POOR', 25, 30, CAST(32.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (25, N'I-A', 1, N'EXCELLENT', 30, 35, CAST(27.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (26, N'I-A', 1, N'AVERAGE', 30, 35, CAST(30.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (27, N'I-A', 1, N'POOR', 30, 35, CAST(36.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (28, N'I-A', 1, N'EXCELLENT', 35, 40, CAST(30.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (29, N'I-A', 1, N'AVERAGE', 35, 40, CAST(33.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (30, N'I-A', 1, N'POOR', 35, 40, CAST(40.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (31, N'I-A', 1, N'EXCELLENT', 40, 45, CAST(33.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (32, N'I-A', 1, N'AVERAGE', 40, 45, CAST(36.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (33, N'I-A', 1, N'POOR', 40, 45, CAST(44.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (34, N'I-A', 1, N'EXCELLENT', 45, 50, CAST(35.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (35, N'I-A', 1, N'AVERAGE', 45, 50, CAST(39.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (36, N'I-A', 1, N'POOR', 45, 50, CAST(47.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (37, N'I-A', 1, N'EXCELLENT', 50, 55, CAST(38.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (38, N'I-A', 1, N'AVERAGE', 50, 55, CAST(42.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (39, N'I-A', 1, N'POOR', 50, 55, CAST(50.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (40, N'I-A', 1, N'EXCELLENT', 55, 60, CAST(41.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (41, N'I-A', 1, N'AVERAGE', 55, 60, CAST(45.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (42, N'I-A', 1, N'POOR', 55, 60, CAST(53.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (43, N'I-A', 1, N'EXCELLENT', 60, 65, CAST(44.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (44, N'I-A', 1, N'AVERAGE', 60, 65, CAST(48.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (45, N'I-A', 1, N'POOR', 60, 65, CAST(55.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (46, N'I-A', 1, N'EXCELLENT', 65, 70, CAST(47.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (47, N'I-A', 1, N'AVERAGE', 65, 70, CAST(51.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (48, N'I-A', 1, N'POOR', 65, 70, CAST(58.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (49, N'I-A', 1, N'EXCELLENT', 70, 75, CAST(50.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (50, N'I-A', 1, N'AVERAGE', 70, 75, CAST(54.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (51, N'I-A', 1, N'POOR', 70, 75, CAST(61.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (52, N'I-A', 1, N'EXCELLENT', 75, 80, CAST(53.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (53, N'I-A', 1, N'AVERAGE', 75, 80, CAST(57.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (54, N'I-A', 1, N'POOR', 75, 80, CAST(64.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (55, N'I-A', 1, N'EXCELLENT', 80, 85, CAST(56.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (56, N'I-A', 1, N'AVERAGE', 80, 85, CAST(60.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (57, N'I-A', 1, N'POOR', 80, 85, CAST(67.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (58, N'I-A', 1, N'EXCELLENT', 85, 90, CAST(59.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (59, N'I-A', 1, N'AVERAGE', 85, 90, CAST(63.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (60, N'I-A', 1, N'POOR', 85, 90, CAST(69.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (61, N'I-A', 1, N'EXCELLENT', 90, 95, CAST(62.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (62, N'I-A', 1, N'AVERAGE', 90, 95, CAST(66.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (63, N'I-A', 1, N'POOR', 90, 95, CAST(72.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (64, N'I-A', 1, N'EXCELLENT', 95, 100, CAST(65.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (65, N'I-A', 1, N'AVERAGE', 95, 100, CAST(69.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (66, N'I-A', 1, N'POOR', 95, 100, CAST(75.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (67, N'II-A', 1, N'EXCELLENT', 0, 2, CAST(3.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (68, N'II-A', 1, N'AVERAGE', 0, 2, CAST(4.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (69, N'II-A', 1, N'POOR', 0, 2, CAST(4.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (70, N'II-A', 1, N'EXCELLENT', 2, 5, CAST(7.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (71, N'II-A', 1, N'AVERAGE', 2, 5, CAST(10.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (72, N'II-A', 1, N'POOR', 2, 5, CAST(10.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (73, N'II-A', 1, N'EXCELLENT', 5, 8, CAST(12.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (74, N'II-A', 1, N'AVERAGE', 5, 8, CAST(17.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (75, N'II-A', 1, N'POOR', 5, 8, CAST(18.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (76, N'II-A', 1, N'EXCELLENT', 8, 12, CAST(16.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (77, N'II-A', 1, N'AVERAGE', 8, 12, CAST(24.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (78, N'II-A', 1, N'POOR', 8, 12, CAST(28.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (79, N'II-A', 1, N'EXCELLENT', 12, 16, CAST(21.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (80, N'II-A', 1, N'AVERAGE', 12, 16, CAST(28.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (81, N'II-A', 1, N'POOR', 12, 16, CAST(36.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (82, N'II-A', 1, N'EXCELLENT', 15, 20, CAST(26.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (83, N'II-A', 1, N'AVERAGE', 15, 20, CAST(32.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (84, N'II-A', 1, N'POOR', 15, 20, CAST(43.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (85, N'II-A', 1, N'EXCELLENT', 20, 25, CAST(29.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (86, N'II-A', 1, N'AVERAGE', 20, 25, CAST(36.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (87, N'II-A', 1, N'POOR', 20, 25, CAST(40.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (88, N'II-A', 1, N'EXCELLENT', 25, 30, CAST(34.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (89, N'II-A', 1, N'AVERAGE', 25, 30, CAST(40.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (90, N'II-A', 1, N'POOR', 25, 30, CAST(55.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (91, N'II-A', 1, N'EXCELLENT', 30, 35, CAST(38.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (92, N'II-A', 1, N'AVERAGE', 30, 35, CAST(45.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (93, N'II-A', 1, N'POOR', 30, 35, CAST(60.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (94, N'II-A', 1, N'EXCELLENT', 35, 40, CAST(43.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (95, N'II-A', 1, N'AVERAGE', 35, 40, CAST(50.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (96, N'II-A', 1, N'POOR', 35, 40, CAST(64.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (97, N'II-A', 1, N'EXCELLENT', 40, 45, CAST(47.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (98, N'II-A', 1, N'AVERAGE', 40, 45, CAST(55.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (99, N'II-A', 1, N'POOR', 40, 45, CAST(69.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (100, N'II-A', 1, N'EXCELLENT', 45, 50, CAST(51.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (101, N'II-A', 1, N'AVERAGE', 45, 50, CAST(60.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (102, N'II-A', 1, N'POOR', 45, 50, CAST(75.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (103, N'III-A', 1, N'EXCELLENT', 0, 2, CAST(4.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (104, N'III-A', 1, N'AVERAGE', 0, 2, CAST(5.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (105, N'III-A', 1, N'POOR', 0, 2, CAST(5.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (106, N'III-A', 1, N'EXCELLENT', 2, 5, CAST(10.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (107, N'III-A', 1, N'AVERAGE', 2, 5, CAST(14.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (108, N'III-A', 1, N'POOR', 2, 5, CAST(14.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (109, N'III-A', 1, N'EXCELLENT', 5, 8, CAST(18.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (110, N'III-A', 1, N'AVERAGE', 5, 8, CAST(22.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (111, N'III-A', 1, N'POOR', 5, 8, CAST(24.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (112, N'III-A', 1, N'EXCELLENT', 8, 12, CAST(25.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (113, N'III-A', 1, N'AVERAGE', 8, 12, CAST(30.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (114, N'III-A', 1, N'POOR', 8, 12, CAST(35.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (115, N'III-A', 1, N'EXCELLENT', 12, 16, CAST(33.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (116, N'III-A', 1, N'AVERAGE', 12, 16, CAST(37.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (117, N'III-A', 1, N'POOR', 12, 16, CAST(46.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (118, N'III-A', 1, N'EXCELLENT', 16, 20, CAST(40.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (119, N'III-A', 1, N'AVERAGE', 16, 20, CAST(44.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (120, N'III-A', 1, N'POOR', 16, 20, CAST(56.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (121, N'III-A', 1, N'EXCELLENT', 20, 25, CAST(47.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (122, N'III-A', 1, N'AVERAGE', 20, 25, CAST(52.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (123, N'III-A', 1, N'POOR', 20, 25, CAST(65.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (124, N'III-A', 1, N'EXCELLENT', 25, 30, CAST(55.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (125, N'III-A', 1, N'AVERAGE', 25, 30, CAST(60.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (126, N'III-A', 1, N'POOR', 25, 30, CAST(72.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (127, N'III-A', 1, N'EXCELLENT', 30, 35, CAST(63.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (128, N'III-A', 1, N'AVERAGE', 30, 35, CAST(69.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (129, N'III-A', 1, N'POOR', 30, 35, CAST(79.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (130, N'IV-A', 1, N'EXCELLENT', 0, 2, CAST(4.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (131, N'IV-A', 1, N'AVERAGE', 0, 2, CAST(5.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (132, N'IV-A', 1, N'POOR', 0, 2, CAST(5.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (133, N'IV-A', 1, N'EXCELLENT', 2, 5, CAST(10.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (134, N'IV-A', 1, N'AVERAGE', 2, 5, CAST(14.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (135, N'IV-A', 1, N'POOR', 2, 5, CAST(14.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (136, N'IV-A', 1, N'EXCELLENT', 5, 8, CAST(18.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (137, N'IV-A', 1, N'AVERAGE', 5, 8, CAST(22.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (138, N'IV-A', 1, N'POOR', 5, 8, CAST(24.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (139, N'IV-A', 1, N'EXCELLENT', 8, 12, CAST(25.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (140, N'IV-A', 1, N'AVERAGE', 8, 12, CAST(30.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (141, N'IV-A', 1, N'POOR', 8, 12, CAST(35.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (142, N'IV-A', 1, N'EXCELLENT', 12, 16, CAST(33.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (143, N'IV-A', 1, N'AVERAGE', 12, 16, CAST(37.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (144, N'IV-A', 1, N'POOR', 12, 16, CAST(46.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (145, N'IV-A', 1, N'EXCELLENT', 16, 20, CAST(40.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (146, N'IV-A', 1, N'AVERAGE', 16, 20, CAST(44.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (147, N'IV-A', 1, N'POOR', 16, 20, CAST(56.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (148, N'IV-A', 1, N'EXCELLENT', 20, 25, CAST(47.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (149, N'IV-A', 1, N'AVERAGE', 20, 25, CAST(52.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (150, N'IV-A', 1, N'POOR', 20, 25, CAST(65.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (151, N'IV-A', 1, N'EXCELLENT', 25, 30, CAST(55.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (152, N'IV-A', 1, N'AVERAGE', 25, 30, CAST(60.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (153, N'IV-A', 1, N'POOR', 25, 30, CAST(72.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (154, N'IV-A', 1, N'EXCELLENT', 30, 35, CAST(63.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (155, N'IV-A', 1, N'AVERAGE', 30, 35, CAST(69.00 AS Decimal(18, 2)))
    INSERT INTO [dbo].[BuildingDepreciations] ([Id], [TypeCode], [ActualUseId], [DegreeOfMaintenance], [AgeLowerBound], [AgeUpperBound], [PercentDepreciated]) VALUES (156, N'IV-A', 1, N'POOR', 30, 35, CAST(79.00 AS Decimal(18, 2)))
    SET IDENTITY_INSERT [dbo].[BuildingDepreciations] OFF;
    PRINT 'BuildingDepreciations seeded successfully.';
END
ELSE
BEGIN
    PRINT 'BuildingDepreciations already populated.';
END
GO

-- 5. Building Assessment Level Brackets (144 rows)
IF NOT EXISTS (SELECT 1 FROM [dbo].[BuildingAssessmentLevels])
BEGIN
    SET IDENTITY_INSERT [dbo].[BuildingAssessmentLevels] ON;
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (1, 1, CAST(174999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(0.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (2, 1, CAST(299999.99 AS Numeric(18, 2)), CAST(175000.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (3, 1, CAST(499999.99 AS Numeric(18, 2)), CAST(300000.00 AS Numeric(18, 2)), CAST(20.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (4, 1, CAST(749999.99 AS Numeric(18, 2)), CAST(500000.00 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (5, 1, CAST(999999.99 AS Numeric(18, 2)), CAST(750000.00 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (6, 1, CAST(1999999.99 AS Numeric(18, 2)), CAST(1000000.00 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (7, 1, CAST(4999999.99 AS Numeric(18, 2)), CAST(2000000.00 AS Numeric(18, 2)), CAST(40.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (8, 1, CAST(9999999.99 AS Numeric(18, 2)), CAST(5000000.00 AS Numeric(18, 2)), CAST(50.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (9, 1, CAST(999999999.99 AS Numeric(18, 2)), CAST(10000000.00 AS Numeric(18, 2)), CAST(60.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (10, 4, CAST(299999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (11, 4, CAST(499999.99 AS Numeric(18, 2)), CAST(300000.00 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (12, 4, CAST(749999.99 AS Numeric(18, 2)), CAST(500000.00 AS Numeric(18, 2)), CAST(40.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (13, 4, CAST(999999.99 AS Numeric(18, 2)), CAST(750000.00 AS Numeric(18, 2)), CAST(50.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (14, 4, CAST(1999999.99 AS Numeric(18, 2)), CAST(1000000.00 AS Numeric(18, 2)), CAST(60.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (15, 4, CAST(4999999.99 AS Numeric(18, 2)), CAST(2000000.00 AS Numeric(18, 2)), CAST(70.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (16, 4, CAST(9999999.99 AS Numeric(18, 2)), CAST(5000000.00 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (17, 4, CAST(999999999.99 AS Numeric(18, 2)), CAST(10000000.00 AS Numeric(18, 2)), CAST(80.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (18, 6, CAST(299999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (19, 6, CAST(499999.99 AS Numeric(18, 2)), CAST(300000.00 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (20, 6, CAST(749999.99 AS Numeric(18, 2)), CAST(500000.00 AS Numeric(18, 2)), CAST(40.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (21, 6, CAST(999999.99 AS Numeric(18, 2)), CAST(750000.00 AS Numeric(18, 2)), CAST(50.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (22, 6, CAST(1999999.99 AS Numeric(18, 2)), CAST(1000000.00 AS Numeric(18, 2)), CAST(60.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (23, 6, CAST(4999999.99 AS Numeric(18, 2)), CAST(2000000.00 AS Numeric(18, 2)), CAST(70.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (24, 6, CAST(9999999.99 AS Numeric(18, 2)), CAST(5000000.00 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (25, 6, CAST(99999999.99 AS Numeric(18, 2)), CAST(10000000.00 AS Numeric(18, 2)), CAST(80.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (26, 17, CAST(99999999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(15.00 AS Decimal(18, 2)), 1994)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (27, 1, CAST(175000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(0.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (28, 1, CAST(300000.00 AS Numeric(18, 2)), CAST(175000.01 AS Numeric(18, 2)), CAST(5.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (29, 1, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(15.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (30, 1, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(20.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (31, 1, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (32, 1, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (33, 1, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (34, 1, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (35, 1, CAST(99999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (36, 4, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (37, 4, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (38, 4, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (39, 4, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (40, 4, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (41, 4, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (42, 6, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (43, 6, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (44, 6, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (45, 6, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (46, 6, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (47, 6, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (48, 6, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(70.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (49, 6, CAST(99999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (50, 62, CAST(299999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(40.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (51, 62, CAST(499999.99 AS Numeric(18, 2)), CAST(300000.00 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (52, 62, CAST(749999.99 AS Numeric(18, 2)), CAST(500000.00 AS Numeric(18, 2)), CAST(50.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (53, 62, CAST(999999.99 AS Numeric(18, 2)), CAST(750000.00 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (54, 62, CAST(1999999.99 AS Numeric(18, 2)), CAST(1000000.00 AS Numeric(18, 2)), CAST(60.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (55, 62, CAST(999999999.99 AS Numeric(18, 2)), CAST(2000000.00 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2002)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (56, 4, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(70.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (57, 4, CAST(999999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (58, 48, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(20.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (59, 48, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (60, 48, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (61, 48, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (62, 48, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(40.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (63, 48, CAST(999999999.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (64, 19, CAST(999999999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(5.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (65, 6, CAST(999999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (66, 50, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(40.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (67, 50, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (68, 50, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(50.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (69, 50, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (70, 50, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(60.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (71, 50, CAST(99999999.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (72, 62, CAST(999999999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(0.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (73, 64, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (74, 64, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (75, 64, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (76, 64, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (77, 64, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (78, 64, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (79, 64, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(70.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (80, 64, CAST(999999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (81, 14, CAST(999999999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (82, 66, CAST(999999999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (83, 20, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (84, 20, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (85, 20, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (86, 20, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (87, 20, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (88, 20, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (89, 20, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(70.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (90, 20, CAST(999999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (91, 68, CAST(999999.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(0.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (92, 13, CAST(9999999.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(0.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (93, 11, CAST(99999999.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(0.00 AS Decimal(18, 2)), 2005)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (94, 1, CAST(175000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(0.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (95, 1, CAST(300000.00 AS Numeric(18, 2)), CAST(175000.01 AS Numeric(18, 2)), CAST(5.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (96, 1, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(15.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (97, 1, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(20.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (98, 1, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (99, 1, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (100, 1, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (101, 1, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (102, 1, CAST(1000000000.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (103, 48, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(20.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (104, 48, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (105, 48, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (106, 48, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (107, 48, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(40.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (108, 48, CAST(9999999999.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (109, 4, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (110, 4, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (111, 4, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (112, 4, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (113, 4, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (114, 4, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (115, 4, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(70.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (116, 4, CAST(9999999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (117, 6, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(25.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (118, 6, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(30.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (119, 6, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (120, 6, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (121, 6, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (122, 6, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (123, 6, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(70.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (124, 6, CAST(9999999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (125, 50, CAST(300000.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(40.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (126, 50, CAST(500000.00 AS Numeric(18, 2)), CAST(300000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (127, 50, CAST(750000.00 AS Numeric(18, 2)), CAST(500000.01 AS Numeric(18, 2)), CAST(50.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (128, 50, CAST(1000000.00 AS Numeric(18, 2)), CAST(750000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (129, 50, CAST(2000000.00 AS Numeric(18, 2)), CAST(1000000.01 AS Numeric(18, 2)), CAST(60.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (130, 50, CAST(9999999999.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(65.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (131, 10, CAST(9999999999.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (132, 12, CAST(9999999999.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (133, 11, CAST(9999999999.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (134, 73, CAST(9999999999.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (135, 62, CAST(999999999.00 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (136, 20, CAST(999999999.99 AS Numeric(18, 2)), CAST(0.00 AS Numeric(18, 2)), CAST(10.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (137, 13, CAST(2000000000.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (138, 19, CAST(9999999999.00 AS Numeric(18, 2)), CAST(10000000.01 AS Numeric(18, 2)), CAST(75.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (139, 13, CAST(10000000.00 AS Numeric(18, 2)), CAST(5000000.01 AS Numeric(18, 2)), CAST(45.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (140, 13, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (141, 62, CAST(5000000.00 AS Numeric(18, 2)), CAST(2000000.01 AS Numeric(18, 2)), CAST(35.00 AS Decimal(18, 2)), 2011)
    INSERT INTO [dbo].[BuildingAssessmentLevels] ([Id], [ActualUseId], [UpperBound], [LowerBound], [AssessmentLevel], [RevisionYear]) VALUES (142, 62, CAST(1000000000.00 AS Numeric(18, 2)), CAST(100000000.01 AS Numeric(18, 2)), CAST(55.00 AS Decimal(18, 2)), 2011)
    SET IDENTITY_INSERT [dbo].[BuildingAssessmentLevels] OFF;
    PRINT 'BuildingAssessmentLevels seeded successfully.';
END
ELSE
BEGIN
    PRINT 'BuildingAssessmentLevels already populated.';
END
GO

GO


-- ==========================================================================================
-- SECTION: 14_SeedLandPrerequisitesAndTables.sql
-- ==========================================================================================
PRINT 'Executing section: 14_SeedLandPrerequisitesAndTables.sql...';
GO
-- ==========================================================================================
-- SCRIPT: seed_land_prerequisites_and_tables.sql
-- DESCRIPTION: Creates prerequisite lookup tables and operational Land Appraisal line tables
--              for Local Government Unit (LGU) Assessor's Real Property Tax Assessment (CAST Standard),
--              and populates default seed data (General Santos City / CAST Standard Schedule).
-- ==========================================================================================
-- USE []; -- Run against active connected database
GO

-- 1. Table: LandRefClassification
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LandRefClassification')
BEGIN
    CREATE TABLE [dbo].[LandRefClassification] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ClassificationCode] NVARCHAR(50) NOT NULL,
        [ClassificationName] NVARCHAR(100) NOT NULL,
        [Ranking] INT NULL,
        [Prefix] NVARCHAR(20) NULL,
        [IsActive] BIT NOT NULL DEFAULT (1),
        [SortOrder] INT NOT NULL DEFAULT (1)
    );
END
GO

-- 2. Table: LandRefSubClassification
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LandRefSubClassification')
BEGIN
    CREATE TABLE [dbo].[LandRefSubClassification] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ClassificationId] INT NOT NULL,
        [SubClass] NVARCHAR(100) NOT NULL,
        [UnitValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 3. Table: LandRefActualUse
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LandRefActualUse')
BEGIN
    CREATE TABLE [dbo].[LandRefActualUse] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActualUseCode] NVARCHAR(50) NOT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [IsTaxExempt] BIT NOT NULL DEFAULT (0),
        [Rank] INT NULL,
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 4. Table: LandRefAssessmentLevel
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LandRefAssessmentLevel')
BEGIN
    CREATE TABLE [dbo].[LandRefAssessmentLevel] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActualUseId] INT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [AssessmentLevel] DECIMAL(18,2) NOT NULL DEFAULT (20),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 5. Table: LandRefAdjustment
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LandRefAdjustment')
BEGIN
    CREATE TABLE [dbo].[LandRefAdjustment] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Description] NVARCHAR(200) NOT NULL,
        [Distance] NVARCHAR(50) NULL,
        [AdjustmentPercent] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 6. Table: LandRefImprovementType
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LandRefImprovementType')
BEGIN
    CREATE TABLE [dbo].[LandRefImprovementType] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TypeCode] NVARCHAR(50) NOT NULL,
        [TypeDescription] NVARCHAR(150) NOT NULL,
        [Kind] NVARCHAR(50) NOT NULL DEFAULT ('FRUIT BEARING'),
        [BaseValue] DECIMAL(18,2) NOT NULL DEFAULT (50.00),
        [AssessmentLevel] DECIMAL(18,2) NOT NULL DEFAULT (40.00),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 7. Operational Table: TDNLandDetails (Classification Breakdown)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TDNLandDetails')
BEGIN
    CREATE TABLE [dbo].[TDNLandDetails] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TDNLandId] UNIQUEIDENTIFIER NOT NULL,
        [OrderNo] INT NOT NULL DEFAULT (1),
        [ClassificationId] INT NULL,
        [ClassificationName] NVARCHAR(100) NOT NULL,
        [SubClassId] INT NULL,
        [SubClassName] NVARCHAR(100) NOT NULL,
        [ActualUseId] INT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [AreaSQM] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [AreaHAS] DECIMAL(18,4) NOT NULL DEFAULT (0),
        [UnitValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [MarketValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [IsTaxable] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 8. Operational Table: TDNLandImprovements (Plants, Trees & Improvements)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TDNLandImprovements')
BEGIN
    CREATE TABLE [dbo].[TDNLandImprovements] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TDNLandId] UNIQUEIDENTIFIER NOT NULL,
        [OrderNo] INT NOT NULL DEFAULT (1),
        [TypeId] INT NULL,
        [ProductClass] NVARCHAR(100) NOT NULL,
        [AreaPlanted] DECIMAL(18,2) NULL,
        [IsFruitBearing] BIT NOT NULL DEFAULT (1),
        [Quantity] DECIMAL(18,2) NOT NULL DEFAULT (1),
        [AgeYears] INT NULL,
        [UnitValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [MarketValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [AssessmentLevel] DECIMAL(18,2) NOT NULL DEFAULT (40.00),
        [AssessedValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [IsTaxable] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 9. Operational Table: TDNLandBaseMarkets (Base Market Value Adjustments)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TDNLandBaseMarkets')
BEGIN
    CREATE TABLE [dbo].[TDNLandBaseMarkets] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TDNLandId] UNIQUEIDENTIFIER NOT NULL,
        [OrderNo] INT NOT NULL DEFAULT (1),
        [ClassificationId] INT NULL,
        [ClassificationName] NVARCHAR(100) NOT NULL,
        [BaseMarketValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [AdjustmentFactorId] INT NULL,
        [AdjustmentFactor] NVARCHAR(150) NOT NULL,
        [PercentAdjustment] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [ValueAdjustment] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [AdjustedMarketValue] DECIMAL(18,2) NOT NULL DEFAULT (0)
    );
END
GO

-- 10. Operational Table: TDNLandPropertyAssessments (Property Assessment Roll)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TDNLandPropertyAssessments')
BEGIN
    CREATE TABLE [dbo].[TDNLandPropertyAssessments] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TDNLandId] UNIQUEIDENTIFIER NOT NULL,
        [OrderNo] INT NOT NULL DEFAULT (1),
        [Kinds] NVARCHAR(50) NOT NULL,
        [ActualUseId] INT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [MarketValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [AssessmentLevelPercent] DECIMAL(18,2) NOT NULL DEFAULT (20.00),
        [AssessmentValue] DECIMAL(18,2) NOT NULL DEFAULT (0)
    );
END
GO

-- ==========================================================================================
-- DEFAULT INSERT DATA SEEDING (CAST SYSTEM REFERENCE)
-- ==========================================================================================

-- Seeding LandRefClassification
IF NOT EXISTS (SELECT 1 FROM [dbo].[LandRefClassification])
BEGIN
    SET IDENTITY_INSERT [dbo].[LandRefClassification] ON;
    INSERT INTO [dbo].[LandRefClassification] ([Id], [ClassificationCode], [ClassificationName], [Ranking], [Prefix], [IsActive], [SortOrder]) VALUES
    (1, N'RES', N'RESIDENTIAL', 1, N'RES', 1, 1),
    (4, N'COM', N'COMMERCIAL', 3, N'COM', 1, 2),
    (6, N'IND', N'INDUSTRIAL', 4, N'IND', 1, 3),
    (48, N'AGR', N'AGRICULTURAL', 2, N'AGR', 1, 4),
    (10, N'CUL', N'SPECIAL - CULTURAL', 7, N'CUL', 1, 5),
    (11, N'HOS', N'SPECIAL - HOSPITAL', 8, N'HOS', 1, 6),
    (12, N'SCI', N'SPECIAL - SCIENTIFIC', 9, N'SCI', 1, 7),
    (49, N'MIN', N'MINERAL', 5, N'MIN', 1, 8),
    (50, N'TIM', N'TIMBERLAND', 6, N'TIM', 1, 9);
    SET IDENTITY_INSERT [dbo].[LandRefClassification] OFF;
END
GO

-- Seeding LandRefSubClassification (Schedule of Unit Values)
IF NOT EXISTS (SELECT 1 FROM [dbo].[LandRefSubClassification])
BEGIN
    SET IDENTITY_INSERT [dbo].[LandRefSubClassification] ON;
    INSERT INTO [dbo].[LandRefSubClassification] ([Id], [ClassificationId], [SubClass], [UnitValue], [RevisionYear], [IsActive]) VALUES
    -- Residential (PHP/sq.m.)
    (1, 1, N'1st Class Residential', 520.00, 2011, 1),
    (2, 1, N'2nd Class Residential', 468.00, 2011, 1),
    (3, 1, N'3rd Class Residential', 410.80, 2011, 1),
    (4, 1, N'4th Class Residential', 351.00, 2011, 1),
    (5, 1, N'5th Class Residential', 286.00, 2011, 1),
    (6, 1, N'6th Class Residential', 234.00, 2011, 1),
    (7, 1, N'7th Class Residential', 163.80, 2011, 1),
    (8, 1, N'8th Class Residential', 117.00, 2011, 1),

    -- Commercial (PHP/sq.m.)
    (9, 4, N'1st Class Commercial', 3120.00, 2011, 1),
    (10, 4, N'2nd Class Commercial', 2522.00, 2011, 1),
    (11, 4, N'3rd Class Commercial', 1924.00, 2011, 1),
    (12, 4, N'4th Class Commercial', 1378.00, 2011, 1),
    (13, 4, N'5th Class Commercial', 1222.00, 2011, 1),
    (14, 4, N'6th Class Commercial', 988.00, 2011, 1),

    -- Industrial (PHP/sq.m.)
    (15, 6, N'1st Class Industrial', 910.00, 2011, 1),
    (16, 6, N'2nd Class Industrial', 806.00, 2011, 1),
    (17, 6, N'3rd Class Industrial', 676.00, 2011, 1),
    (18, 6, N'4th Class Industrial', 546.00, 2011, 1),
    (19, 6, N'5th Class Industrial', 416.00, 2011, 1),

    -- Agricultural (PHP/ha converted to base rate)
    (20, 48, N'Riceland Irrigated (1st Class)', 150000.00, 2011, 1),
    (21, 48, N'Riceland Unirrigated (2nd Class)', 130000.00, 2011, 1),
    (22, 48, N'Cornland (1st Class)', 110000.00, 2011, 1),
    (23, 48, N'Coconut Land', 100000.00, 2011, 1),
    (24, 48, N'Banana Plantation', 120000.00, 2011, 1),
    (25, 48, N'Fishpond / Aquaculture', 160000.00, 2011, 1);
    SET IDENTITY_INSERT [dbo].[LandRefSubClassification] OFF;
END
GO

-- Seeding LandRefActualUse
IF NOT EXISTS (SELECT 1 FROM [dbo].[LandRefActualUse])
BEGIN
    SET IDENTITY_INSERT [dbo].[LandRefActualUse] ON;
    INSERT INTO [dbo].[LandRefActualUse] ([Id], [ActualUseCode], [ActualUseName], [IsTaxExempt], [Rank], [IsActive]) VALUES
    (1, N'RES', N'RESIDENTIAL', 0, 1, 1),
    (2, N'AGR', N'AGRICULTURAL', 0, 2, 1),
    (3, N'COM', N'COMMERCIAL', 0, 3, 1),
    (4, N'IND', N'INDUSTRIAL', 0, 4, 1),
    (5, N'MIN', N'MINERAL', 0, 5, 1),
    (6, N'CUL', N'SPECIAL - CULTURAL', 0, 7, 1),
    (7, N'HOS', N'SPECIAL - HOSPITAL', 0, 8, 1),
    (8, N'SCI', N'SPECIAL - SCIENTIFIC', 0, 9, 1),
    (9, N'GOV', N'GOVERNMENT', 1, 10, 1),
    (10, N'REL', N'RELIGIOUS', 1, 11, 1),
    (11, N'CHAR', N'CHARITABLE', 1, 12, 1),
    (12, N'EDUC', N'EDUCATIONAL', 1, 13, 1);
    SET IDENTITY_INSERT [dbo].[LandRefActualUse] OFF;
END
GO

-- Seeding LandRefAssessmentLevel
IF NOT EXISTS (SELECT 1 FROM [dbo].[LandRefAssessmentLevel])
BEGIN
    SET IDENTITY_INSERT [dbo].[LandRefAssessmentLevel] ON;
    INSERT INTO [dbo].[LandRefAssessmentLevel] ([Id], [ActualUseId], [ActualUseName], [AssessmentLevel], [RevisionYear], [IsActive]) VALUES
    (1, 1, N'RESIDENTIAL', 20.00, 2011, 1),
    (2, 2, N'AGRICULTURAL', 40.00, 2011, 1),
    (3, 3, N'COMMERCIAL', 50.00, 2011, 1),
    (4, 4, N'INDUSTRIAL', 50.00, 2011, 1),
    (5, 5, N'MINERAL', 50.00, 2011, 1),
    (6, 6, N'SPECIAL - CULTURAL', 10.00, 2011, 1),
    (7, 7, N'SPECIAL - HOSPITAL', 10.00, 2011, 1),
    (8, 8, N'SPECIAL - SCIENTIFIC', 10.00, 2011, 1),
    (9, 9, N'GOVERNMENT', 0.00, 2011, 1);
    SET IDENTITY_INSERT [dbo].[LandRefAssessmentLevel] OFF;
END
GO

-- Seeding LandRefAdjustment
IF NOT EXISTS (SELECT 1 FROM [dbo].[LandRefAdjustment])
BEGIN
    SET IDENTITY_INSERT [dbo].[LandRefAdjustment] ON;
    INSERT INTO [dbo].[LandRefAdjustment] ([Id], [Description], [Distance], [AdjustmentPercent], [RevisionYear], [IsActive]) VALUES
    (1, N'Corner Influence (Residential)', NULL, 10.00, 2011, 1),
    (2, N'Corner Influence (Commercial)', NULL, 25.00, 2011, 1),
    (3, N'Provincial & National Highway Frontage', NULL, 0.00, 2011, 1),
    (4, N'All-Weather Gravel Road', N'0-1 km', -2.00, 2011, 1),
    (5, N'Dirt Road', N'Over 1-3 km', -6.00, 2011, 1),
    (6, N'Interior Lot / No Road Outlet', NULL, -10.00, 2011, 1),
    (7, N'Topography: 16-30 degrees slope', NULL, -10.00, 2011, 1),
    (8, N'Topography: 31-45 degrees slope', NULL, -20.00, 2011, 1),
    (9, N'Sunken / Lowland / Subject to Inundation', NULL, -30.00, 2011, 1),
    (10, N'Cost of Filling / Stripping (20m depth)', N'20m', -20.00, 2011, 1),
    (11, N'Slightly Eroded Land', NULL, -10.00, 2011, 1);
    SET IDENTITY_INSERT [dbo].[LandRefAdjustment] OFF;
END
GO

-- Seeding LandRefImprovementType
IF NOT EXISTS (SELECT 1 FROM [dbo].[LandRefImprovementType])
BEGIN
    SET IDENTITY_INSERT [dbo].[LandRefImprovementType] ON;
    INSERT INTO [dbo].[LandRefImprovementType] ([Id], [TypeCode], [TypeDescription], [Kind], [BaseValue], [AssessmentLevel], [RevisionYear], [IsActive]) VALUES
    (1, N'CCT', N'COCONUT TREE', N'FRUIT BEARING', 120.00, 40.00, 2011, 1),
    (2, N'MGO', N'MANGO TREE', N'FRUIT BEARING', 350.00, 40.00, 2011, 1),
    (3, N'BNN', N'BANANA CLUMPS', N'FRUIT BEARING', 35.00, 40.00, 2011, 1),
    (4, N'BMO', N'BAMBOO CLUMPS', N'PLANT', 45.00, 40.00, 2011, 1),
    (5, N'AVD', N'AVOCADO TREE', N'FRUIT BEARING', 150.00, 40.00, 2011, 1),
    (6, N'COFT', N'COFFEE TREE', N'FRUIT BEARING', 50.00, 40.00, 2011, 1),
    (7, N'CAT', N'CACAO TREE', N'FRUIT BEARING', 60.00, 40.00, 2011, 1),
    (8, N'DB', N'DURIAN (BEARING)', N'FRUIT BEARING', 400.00, 40.00, 2011, 1),
    (9, N'GF', N'GEMILINA TREE', N'FOREST TREE', 180.00, 40.00, 2011, 1),
    (10, N'MB', N'MAHOGANY (BIG)', N'FOREST TREE', 250.00, 40.00, 2011, 1),
    (11, N'NR', N'NARRA TREE', N'HARDWOOD', 500.00, 40.00, 2011, 1),
    (12, N'FENCE', N'CONCRETE FENCE / PERIMETER WALL', N'IMPROVEMENT', 650.00, 40.00, 2011, 1),
    (13, N'PAVE', N'CONCRETE PAVEMENT / DRIVEWAY', N'IMPROVEMENT', 250.00, 40.00, 2011, 1);
    SET IDENTITY_INSERT [dbo].[LandRefImprovementType] OFF;
END
GO

PRINT 'Land Prerequisites and Tables Seeded Successfully (CAST System Standard)!';

GO


-- ==========================================================================================
-- SECTION: 15_SeedDefaultProvincesCitiesBarangays.sql
-- ==========================================================================================
PRINT 'Executing section: 15_SeedDefaultProvincesCitiesBarangays.sql...';
GO
-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 15_SeedDefaultProvincesCitiesBarangays.sql
-- DESCRIPTION: Seeds default geographic lookup master data:
--              1. [dbo].[Provinces] (South Cotabato, Cavite, NCR, Cebu, etc.)
--              2. [dbo].[CityMunicipalities] (General Santos City, Koronadal, etc.)
--              3. [dbo].[Barangays] (Complete 26 official Barangays of GenSan + sample brgys)
--              Uses idempotent MERGE operations with deterministic GUIDs matching
--              GeographicService for seamless local / database operation.
-- DATABASE: FairTrackDB
-- =============================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Default Geographic Data Seeding (Provinces, Cities, Barangays)...';
PRINT '=========================================================================';

-- -----------------------------------------------------------------------------
-- 1. Ensure Tables Exist
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Provinces')
BEGIN
    CREATE TABLE [dbo].[Provinces] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Region] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created table [dbo].[Provinces].';
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CityMunicipalities')
BEGIN
    CREATE TABLE [dbo].[CityMunicipalities] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [ProvinceId] UNIQUEIDENTIFIER NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [ZipCode] NVARCHAR(20) NOT NULL,
        [IsCity] BIT NOT NULL DEFAULT 1,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_CityMunicipalities_Provinces] FOREIGN KEY ([ProvinceId]) REFERENCES [dbo].[Provinces]([Id])
    );
    PRINT 'Created table [dbo].[CityMunicipalities].';
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Barangays')
BEGIN
    CREATE TABLE [dbo].[Barangays] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [CityMunicipalityId] UNIQUEIDENTIFIER NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [District] NVARCHAR(50) NOT NULL DEFAULT 'District 1',
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_Barangays_CityMunicipalities] FOREIGN KEY ([CityMunicipalityId]) REFERENCES [dbo].[CityMunicipalities]([Id])
    );
    PRINT 'Created table [dbo].[Barangays].';
END
GO

-- -----------------------------------------------------------------------------
-- 2. Seed / Upsert Provinces
-- -----------------------------------------------------------------------------
BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @SourceProvinces TABLE (
        [Id] UNIQUEIDENTIFIER,
        [Code] NVARCHAR(50),
        [Name] NVARCHAR(100),
        [Region] NVARCHAR(100),
        [IsActive] BIT
    );

    INSERT INTO @SourceProvinces ([Id], [Code], [Name], [Region], [IsActive])
    VALUES
        ('10000000-0000-0000-0000-000000000001', 'PROV-1263', 'South Cotabato',       'Region XII (SOCCSKSARGEN)', 1),
        ('10000000-0000-0000-0000-000000000002', 'PROV-0421', 'Cavite',               'Region IV-A (CALABARZON)',   1),
        ('10000000-0000-0000-0000-000000000003', 'PROV-1300', 'Metro Manila (NCR)',   'National Capital Region',   1),
        ('10000000-0000-0000-0000-000000000004', 'PROV-0722', 'Cebu',                 'Region VII (Central Visayas)',1),
        ('10000000-0000-0000-0000-000000000005', 'PROV-1124', 'Davao del Sur',        'Region XI (Davao Region)',   1),
        ('10000000-0000-0000-0000-000000000006', 'PROV-1280', 'Sarangani',            'Region XII (SOCCSKSARGEN)', 1);

    MERGE [dbo].[Provinces] AS Target
    USING @SourceProvinces AS Source
    ON Target.[Id] = Source.[Id] OR Target.[Code] = Source.[Code]
    WHEN MATCHED THEN
        UPDATE SET
            Target.[Name]     = Source.[Name],
            Target.[Region]   = Source.[Region],
            Target.[IsActive] = Source.[IsActive]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [Code], [Name], [Region], [IsActive])
        VALUES (Source.[Id], Source.[Code], Source.[Name], Source.[Region], Source.[IsActive]);

    PRINT 'Provinces seeded successfully.';

    -- -------------------------------------------------------------------------
    -- 3. Seed / Upsert CityMunicipalities
    -- -------------------------------------------------------------------------
    DECLARE @SourceCities TABLE (
        [Id] UNIQUEIDENTIFIER,
        [ProvinceId] UNIQUEIDENTIFIER,
        [Code] NVARCHAR(50),
        [Name] NVARCHAR(100),
        [ZipCode] NVARCHAR(20),
        [IsCity] BIT,
        [IsActive] BIT
    );

    INSERT INTO @SourceCities ([Id], [ProvinceId], [Code], [Name], [ZipCode], [IsCity], [IsActive])
    VALUES
        ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'CITY-9500', 'General Santos City', '9500', 1, 1),
        ('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'CITY-9506', 'Koronadal City',     '9506', 1, 1),
        ('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'MUNI-9504', 'Polomolok',          '9504', 0, 1),
        ('20000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000002', 'CITY-4114', 'Dasmariñas City',     '4114', 1, 1),
        ('20000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000003', 'CITY-1100', 'Quezon City',         '1100', 1, 1),
        ('20000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000005', 'CITY-8000', 'Davao City',          '8000', 1, 1),
        ('20000000-0000-0000-0000-000000000007', '10000000-0000-0000-0000-000000000006', 'MUNI-9501', 'Alabel',              '9501', 0, 1);

    MERGE [dbo].[CityMunicipalities] AS Target
    USING @SourceCities AS Source
    ON Target.[Id] = Source.[Id] OR Target.[Code] = Source.[Code]
    WHEN MATCHED THEN
        UPDATE SET
            Target.[ProvinceId] = Source.[ProvinceId],
            Target.[Name]       = Source.[Name],
            Target.[ZipCode]    = Source.[ZipCode],
            Target.[IsCity]     = Source.[IsCity],
            Target.[IsActive]   = Source.[IsActive]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [ProvinceId], [Code], [Name], [ZipCode], [IsCity], [IsActive])
        VALUES (Source.[Id], Source.[ProvinceId], Source.[Code], Source.[Name], Source.[ZipCode], Source.[IsCity], Source.[IsActive]);

    PRINT 'CityMunicipalities seeded successfully.';

    -- -------------------------------------------------------------------------
    -- 4. Seed / Upsert Barangays (General Santos City 26 Official Barangays + Samples)
    -- -------------------------------------------------------------------------
    DECLARE @SourceBarangays TABLE (
        [Id] UNIQUEIDENTIFIER,
        [CityMunicipalityId] UNIQUEIDENTIFIER,
        [Code] NVARCHAR(50),
        [Name] NVARCHAR(100),
        [District] NVARCHAR(50),
        [IsActive] BIT
    );

    INSERT INTO @SourceBarangays ([Id], [CityMunicipalityId], [Code], [Name], [District], [IsActive])
    VALUES
        -- General Santos City (Complete 26 Barangays)
        ('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-01', 'Brgy. Dadiangas East (Poblacion)', 'District 1', 1),
        ('30000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-02', 'Brgy. Lagao',                   'District 2', 1),
        ('30000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-03', 'Brgy. Calumpang',               'District 1', 1),
        ('30000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-04', 'Brgy. San Isidro',               'District 3', 1),
        ('30000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-05', 'Brgy. City Heights',              'District 2', 1),
        ('30000000-0000-0000-0000-000000000010', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-10', 'Brgy. Apopong',                   'District 1', 1),
        ('30000000-0000-0000-0000-000000000011', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-11', 'Brgy. Baluan',                    'District 2', 1),
        ('30000000-0000-0000-0000-000000000012', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-12', 'Brgy. Batomelong',                'District 2', 1),
        ('30000000-0000-0000-0000-000000000013', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-13', 'Brgy. Buayan',                     'District 2', 1),
        ('30000000-0000-0000-0000-000000000014', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-14', 'Brgy. Bula',                       'District 1', 1),
        ('30000000-0000-0000-0000-000000000015', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-15', 'Brgy. Conel',                      'District 2', 1),
        ('30000000-0000-0000-0000-000000000016', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-16', 'Brgy. Dadiangas North',           'District 1', 1),
        ('30000000-0000-0000-0000-000000000017', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-17', 'Brgy. Dadiangas South',           'District 1', 1),
        ('30000000-0000-0000-0000-000000000018', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-18', 'Brgy. Dadiangas West',            'District 1', 1),
        ('30000000-0000-0000-0000-000000000019', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-19', 'Brgy. Fatima',                     'District 1', 1),
        ('30000000-0000-0000-0000-000000000020', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-20', 'Brgy. Katangawan',                 'District 2', 1),
        ('30000000-0000-0000-0000-000000000021', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-21', 'Brgy. Labangal',                   'District 1', 1),
        ('30000000-0000-0000-0000-000000000022', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-22', 'Brgy. Ligaya',                     'District 2', 1),
        ('30000000-0000-0000-0000-000000000023', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-23', 'Brgy. Mabuhay',                    'District 2', 1),
        ('30000000-0000-0000-0000-000000000024', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-24', 'Brgy. San Jose',                   'District 3', 1),
        ('30000000-0000-0000-0000-000000000025', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-25', 'Brgy. Sinawal',                    'District 1', 1),
        ('30000000-0000-0000-0000-000000000026', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-26', 'Brgy. Tambler',                    'District 1', 1),
        ('30000000-0000-0000-0000-000000000027', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-27', 'Brgy. Tinagacan',                  'District 2', 1),
        ('30000000-0000-0000-0000-000000000028', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-28', 'Brgy. Upper Labay',                 'District 2', 1),
        ('30000000-0000-0000-0000-000000000029', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-29', 'Brgy. Olympog',                    'District 2', 1),
        ('30000000-0000-0000-0000-000000000030', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-30', 'Brgy. Siguel',                     'District 1', 1),

        -- Koronadal City
        ('30000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000002', 'BRGY-9506-01', 'Brgy. Zone 1 (Poblacion)',         'District 1', 1),
        ('30000000-0000-0000-0000-000000000007', '20000000-0000-0000-0000-000000000002', 'BRGY-9506-02', 'Brgy. Morales',                    'District 2', 1),
        ('30000000-0000-0000-0000-000000000031', '20000000-0000-0000-0000-000000000002', 'BRGY-9506-03', 'Brgy. Assumption',                 'District 2', 1),
        ('30000000-0000-0000-0000-000000000032', '20000000-0000-0000-0000-000000000002', 'BRGY-9506-04', 'Brgy. Carpenter Hill',            'District 2', 1),

        -- Dasmariñas City
        ('30000000-0000-0000-0000-000000000008', '20000000-0000-0000-0000-000000000004', 'BRGY-4114-01', 'Brgy. Salitran I',                 'Zone 1',     1),
        ('30000000-0000-0000-0000-000000000009', '20000000-0000-0000-0000-000000000004', 'BRGY-4114-02', 'Brgy. Paliparan I',                'Zone 2',     1);

    MERGE [dbo].[Barangays] AS Target
    USING @SourceBarangays AS Source
    ON Target.[Id] = Source.[Id] OR Target.[Code] = Source.[Code]
    WHEN MATCHED THEN
        UPDATE SET
            Target.[CityMunicipalityId] = Source.[CityMunicipalityId],
            Target.[Name]               = Source.[Name],
            Target.[District]           = Source.[District],
            Target.[IsActive]           = Source.[IsActive]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [CityMunicipalityId], [Code], [Name], [District], [IsActive])
        VALUES (Source.[Id], Source.[CityMunicipalityId], Source.[Code], Source.[Name], Source.[District], Source.[IsActive]);

    PRINT 'Barangays seeded successfully.';

    COMMIT TRANSACTION;
    PRINT '=========================================================================';
    PRINT 'Geographic data seeding completed successfully.';
    PRINT '=========================================================================';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
GO

-- -----------------------------------------------------------------------------
-- 5. Verification Output
-- -----------------------------------------------------------------------------
PRINT 'Verification Counts:';
SELECT 
    (SELECT COUNT(*) FROM [dbo].[Provinces]) AS [TotalProvinces],
    (SELECT COUNT(*) FROM [dbo].[CityMunicipalities]) AS [TotalCitiesMunicipalities],
    (SELECT COUNT(*) FROM [dbo].[Barangays]) AS [TotalBarangays];
GO

GO


-- ==========================================================================================
-- SECTION: 16_SeedMachineryPrerequisitesAndTables.sql
-- ==========================================================================================
PRINT 'Executing section: 16_SeedMachineryPrerequisitesAndTables.sql...';
GO
-- ==========================================================================================
-- SCRIPT: 16_SeedMachineryPrerequisitesAndTables.sql
-- DESCRIPTION: Creates prerequisite lookup tables (MachineryRefActualUse, MachineryRefAssessmentLevel)
--              and operational Machinery Appraisal line tables (TDNMachineryDetails, TDNMachineryPropertyAssessments)
--              for Local Government Unit (LGU) Assessor's Real Property Tax Assessment (CAST System Standard),
--              and populates exact seed data from the CAST reference database.
-- ==========================================================================================
-- USE [NEWCAST]; -- Run against active connected database
GO

-- 1. Reference Table: MachineryRefActualUse (CAST dbo.refActualUse)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MachineryRefActualUse')
BEGIN
    CREATE TABLE [dbo].[MachineryRefActualUse] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActualUseCode] NVARCHAR(50) NOT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [IsTaxExempt] BIT NOT NULL DEFAULT (0),
        [Rank] INT NULL,
        [Prefix] NVARCHAR(50) NULL,
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 2. Reference Table: MachineryRefAssessmentLevel (CAST dbo.refMachineAssessmentLevel)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MachineryRefAssessmentLevel')
BEGIN
    CREATE TABLE [dbo].[MachineryRefAssessmentLevel] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActualUseId] INT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [AssessmentLevel] DECIMAL(18,2) NOT NULL DEFAULT (75.00),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 3. Operational Table: TDNMachineryDetails (CAST dbo.TDNMachineDetail - 16 Columns)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TDNMachineryDetails')
BEGIN
    CREATE TABLE [dbo].[TDNMachineryDetails] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TDNMachineryId] UNIQUEIDENTIFIER NOT NULL,
        [OrderNo] INT NOT NULL DEFAULT (1),
        [KindOfMachinery] NVARCHAR(200) NULL,
        [BrandModel] NVARCHAR(200) NULL,
        [Capacity] NVARCHAR(100) NULL,
        [AcquiredDate] DATETIME2 NULL,
        [ConditionWhenAcquired] NVARCHAR(100) NULL DEFAULT (N'NEW'),
        [EconomicLifeEstimated] INT NULL,
        [RemainingEconomicLife] INT NULL,
        [DateOfInstallation] DATETIME2 NULL,
        [DateOfOperation] DATETIME2 NULL,
        [OriginalCost] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [RCN] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [NoOfYearsUsed] INT NULL,
        [MarketValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [DepreciationTotalPercentage] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [DepreciationTotalValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [DepreciatedValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        CONSTRAINT [FK_TDNMachineryDetails_TDNMachineries_TDNMachineryId]
            FOREIGN KEY ([TDNMachineryId]) REFERENCES [dbo].[TDNMachineries]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_TDNMachineryDetails_TDNMachineryId] ON [dbo].[TDNMachineryDetails]([TDNMachineryId]);
END
GO

-- 4. Operational Table: TDNMachineryPropertyAssessments (Property Assessment Roll for Machinery)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TDNMachineryPropertyAssessments')
BEGIN
    CREATE TABLE [dbo].[TDNMachineryPropertyAssessments] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TDNMachineryId] UNIQUEIDENTIFIER NOT NULL,
        [OrderNo] INT NOT NULL DEFAULT (1),
        [ActualUseId] INT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [MarketValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [AssessmentLevelPercent] DECIMAL(18,2) NOT NULL DEFAULT (75.00),
        [AssessmentValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        CONSTRAINT [FK_TDNMachineryPropertyAssessments_TDNMachineries_TDNMachineryId]
            FOREIGN KEY ([TDNMachineryId]) REFERENCES [dbo].[TDNMachineries]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_TDNMachineryPropertyAssessments_TDNMachineryId] ON [dbo].[TDNMachineryPropertyAssessments]([TDNMachineryId]);
END
GO

-- 5. Add CAST Columns to TDNMachineries if not yet present
IF COL_LENGTH('dbo.TDNMachineries', 'ARPNo') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [ARPNo] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'SectionNo') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [SectionNo] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'ParcelNo') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [ParcelNo] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'LotNo') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [LotNo] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'IsAdministrator') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [IsAdministrator] BIT NOT NULL DEFAULT (0);
IF COL_LENGTH('dbo.TDNMachineries', 'IsBeneficialUser') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [IsBeneficialUser] BIT NOT NULL DEFAULT (0);
IF COL_LENGTH('dbo.TDNMachineries', 'IsPossessor') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [IsPossessor] BIT NOT NULL DEFAULT (0);
IF COL_LENGTH('dbo.TDNMachineries', 'AdministratorName') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [AdministratorName] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'AdministratorSex') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [AdministratorSex] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'AdministratorAddress') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [AdministratorAddress] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'AdministratorTin') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [AdministratorTin] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'AdministratorPhone') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [AdministratorPhone] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'AdministratorEmail') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [AdministratorEmail] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'MachineryAppraisalDetailsJson') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [MachineryAppraisalDetailsJson] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'MachineryPropertyAssessmentsJson') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [MachineryPropertyAssessmentsJson] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'MachineryDateRegistered') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [MachineryDateRegistered] DATETIME2 NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'OrNo') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [OrNo] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'OrDate') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [OrDate] DATETIME2 NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'LocalDateIssued') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [LocalDateIssued] DATETIME2 NULL;
IF COL_LENGTH('dbo.TDNMachineries', 'IsLandOwnerAlsoBuildingOwner') IS NULL
    ALTER TABLE [dbo].[TDNMachineries] ADD [IsLandOwnerAlsoBuildingOwner] BIT NOT NULL DEFAULT (0);
IF COL_LENGTH('dbo.TDNMachineryDetails', 'Classification') IS NULL
    ALTER TABLE [dbo].[TDNMachineryDetails] ADD [Classification] NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.TDNMachineryDetails', 'YearInstalled') IS NULL
    ALTER TABLE [dbo].[TDNMachineryDetails] ADD [YearInstalled] INT NULL;
IF COL_LENGTH('dbo.TDNMachineryDetails', 'YearOfOperation') IS NULL
    ALTER TABLE [dbo].[TDNMachineryDetails] ADD [YearOfOperation] INT NULL;
IF COL_LENGTH('dbo.TDNMachineryDetails', 'InstallationCost') IS NULL
    ALTER TABLE [dbo].[TDNMachineryDetails] ADD [InstallationCost] DECIMAL(18,2) NOT NULL DEFAULT (0);
IF COL_LENGTH('dbo.TDNMachineryDetails', 'TotalCost') IS NULL
    ALTER TABLE [dbo].[TDNMachineryDetails] ADD [TotalCost] DECIMAL(18,2) NOT NULL DEFAULT (0);
IF COL_LENGTH('dbo.RequestForInspections', 'Partitions') IS NULL
    ALTER TABLE [dbo].[RequestForInspections] ADD [Partitions] INT NOT NULL DEFAULT (1);
IF COL_LENGTH('dbo.RequestForInspections', 'GeneratedPartitionsJson') IS NULL
    ALTER TABLE [dbo].[RequestForInspections] ADD [GeneratedPartitionsJson] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.RequestForInspections', 'SelectedRefPropertyIdsJson') IS NULL
    ALTER TABLE [dbo].[RequestForInspections] ADD [SelectedRefPropertyIdsJson] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.RequestForInspections', 'OwnerId') IS NULL
    ALTER TABLE [dbo].[RequestForInspections] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
GO

-- ==========================================================================================
-- DEFAULT INSERT DATA SEEDING (EXACT CAST SYSTEM REFERENCE TABLES)
-- ==========================================================================================

-- Seeding MachineryRefActualUse (Exact rows from CAST.dbo.refActualUse)
IF NOT EXISTS (SELECT 1 FROM [dbo].[MachineryRefActualUse])
BEGIN
    SET IDENTITY_INSERT [dbo].[MachineryRefActualUse] ON;
    INSERT INTO [dbo].[MachineryRefActualUse] ([Id], [ActualUseCode], [ActualUseName], [IsTaxExempt], [Rank], [Prefix], [IsActive]) VALUES
    (1, N'RES', N'RESIDENTIAL', 0, 1, N'RES', 1),
    (2, N'RG', N'RAW LAND', 0, 10, NULL, 0),
    (3, N'AG', N'AGRICULTURE', 0, NULL, NULL, 0),
    (4, N'COM', N'COMMERCIAL', 0, 3, N'COM', 1),
    (5, N'TRD', N'TRADING', 0, 8, NULL, 0),
    (6, N'IND', N'INDUSTRIAL', 0, 4, N'IND', 1),
    (7, N'MFG', N'MANUFACTURING', 0, 7, NULL, 0),
    (8, N'MIN', N'MINING', 0, 9, NULL, 0),
    (9, N'TIM', N'TIMBER/FORESTRY', 0, 7, NULL, 0),
    (10, N'CUL', N'SPECIAL- CULTURAL', 0, 7, N'CUL', 1),
    (11, N'HOS', N'SPECIAL- HOSPITAL', 0, 8, N'HOS', 1),
    (12, N'SCI', N'SPECIAL- SCIENTIFIC', 0, 9, N'SCI', 1),
    (13, N'INS', N'INSTITUTIONAL', 1, 13, NULL, 0),
    (14, N'REL', N'RELIGIOUS', 1, 11, N'REL', 0),
    (15, N'CHR', N'CHARITABLE', 1, 12, NULL, 0),
    (16, N'EDU', N'EDUCATIONAL', 1, 13, NULL, 0),
    (17, N'OTH', N'SPECIAL-OTHERS', 1, 14, NULL, 0),
    (18, N'PRK', N'PARK/OPEN SPACE', 1, 12, NULL, 0),
    (19, N'GOV', N'GOVERNMENT', 1, 10, NULL, 0),
    (20, N'REC', N'SPECIAL-RECREATIONAL', 0, 10, NULL, 0),
    (47, N'MACH', N'MACHINERY', 0, 9, NULL, 1),
    (48, N'AGR', N'AGRICULTURAL', 0, 2, N'AGR', 1),
    (49, N'MIN', N'MINERAL', 0, 5, N'MIN', 1),
    (50, N'TIM', N'TIMBERLAND', 0, 6, N'TIM', 1),
    (51, N'LWD', N'LOCAL WATER DISTRICTS', 0, NULL, N'LWD', 1),
    (52, N'CHAR', N'CHARITABLE', 0, 12, NULL, 0),
    (53, N'EDUC MAC', N'EDUCATIONAL MACHINERIES', 0, 8, NULL, 1),
    (72, N'SCL', N'SPECIAL', 0, 6, N'SCL', 1),
    (73, N'SLWD', N'SPECIAL-LOCAL WATER DISTRICT', 0, 10, NULL, 1);
    SET IDENTITY_INSERT [dbo].[MachineryRefActualUse] OFF;
END
GO

-- Seeding MachineryRefAssessmentLevel (Exact 21 rows from CAST.dbo.refMachineAssessmentLevel)
IF NOT EXISTS (SELECT 1 FROM [dbo].[MachineryRefAssessmentLevel])
BEGIN
    SET IDENTITY_INSERT [dbo].[MachineryRefAssessmentLevel] ON;
    INSERT INTO [dbo].[MachineryRefAssessmentLevel] ([Id], [ActualUseId], [ActualUseName], [AssessmentLevel], [RevisionYear], [IsActive]) VALUES
    (1, 10, N'SPECIAL- CULTURAL', 10.00, 2011, 1),
    (2, 4, N'COMMERCIAL', 80.00, 2002, 1),
    (3, 6, N'INDUSTRIAL', 80.00, 2002, 1),
    (4, 48, N'AGRICULTURAL', 40.00, 2002, 1),
    (5, 48, N'AGRICULTURAL', 35.00, 2005, 1),
    (6, 1, N'RESIDENTIAL', 45.00, 2005, 1),
    (7, 4, N'COMMERCIAL', 75.00, 2005, 1),
    (8, 6, N'INDUSTRIAL', 75.00, 2005, 1),
    (9, 10, N'SPECIAL- CULTURAL', 10.00, 2005, 1),
    (10, 12, N'SPECIAL- SCIENTIFIC', 10.00, 2005, 1),
    (11, 11, N'SPECIAL- HOSPITAL', 10.00, 2005, 1),
    (12, 51, N'LOCAL WATER DISTRICTS', 5.00, 2005, 1),
    (13, 19, N'GOVERNMENT', 5.00, 2005, 1),
    (14, 73, N'SPECIAL-LOCAL WATER DISTRICT', 5.00, 2005, 1),
    (15, 1, N'RESIDENTIAL', 45.00, 2011, 1),
    (16, 4, N'COMMERCIAL', 75.00, 2011, 1),
    (17, 6, N'INDUSTRIAL', 75.00, 2011, 1),
    (18, 12, N'SPECIAL- SCIENTIFIC', 10.00, 2011, 1),
    (19, 11, N'SPECIAL- HOSPITAL', 10.00, 2011, 1),
    (20, 73, N'SPECIAL-LOCAL WATER DISTRICT', 10.00, 2011, 1),
    (21, 48, N'AGRICULTURAL', 35.00, 2011, 1);
    SET IDENTITY_INSERT [dbo].[MachineryRefAssessmentLevel] OFF;
END
GO

PRINT 'Machinery Prerequisites and Tables Seeded Successfully (CAST System Standard)!';

GO


-- ==========================================================================================
-- SECTION: 20_CreateAndSeedBuildingDepreciationSchedule.sql
-- ==========================================================================================
PRINT 'Executing section: 20_CreateAndSeedBuildingDepreciationSchedule.sql...';
GO
-- =============================================================================
-- 20_CreateAndSeedBuildingDepreciationSchedule.sql
-- Purpose:
--   1. Creates the [dbo].[BuildingDepreciationSchedules] SQL table for the
--      official Schedule of Building Physical Depreciation (% Depreciation)
--      across Classifications I, II-A, II-B, III-A, III-B, IV-A, IV-B, V-A, V-B
--      and General Revision (GR) Cycles.
--   2. Seeds all 180 schedule entries (20 age rows x 9 classifications) with
--      Estimated Economic Life (years), Estimated Residual Value (%), explicit
--      cell Depreciation (%), and Effective Depreciation (%).
--   3. Alters [dbo].[TDNBuildings] to add [BuildingDepreciationScheduleId] FK,
--      [GeneralRevisionCycle], [EstimatedEconomicLifeYears], and
--      [EstimatedResidualValuePercent], and links existing TDNBuilding records.
-- =============================================================================

SET NOCOUNT ON;
GO

-- -----------------------------------------------------------------------------
-- 1. CREATE [dbo].[BuildingDepreciationSchedules] TABLE
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[BuildingDepreciationSchedules]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BuildingDepreciationSchedules] (
        [Id]                           INT            IDENTITY(1,1) NOT NULL,
        [GeneralRevisionCycle]         NVARCHAR(50)   NOT NULL,
        [BuildingAgeLabel]             NVARCHAR(20)   NOT NULL,
        [AgeLowerBound]                INT            NOT NULL,
        [AgeUpperBound]                INT            NOT NULL,
        [ClassificationCode]           NVARCHAR(20)   NOT NULL,
        [StructuralType]               NVARCHAR(30)   NOT NULL,
        [EstimatedEconomicLifeYears]   INT            NOT NULL,
        [EstimatedResidualValuePercent] DECIMAL(18,2) NOT NULL,
        [DepreciationPercent]          DECIMAL(18,2)  NULL,      -- NULL represents '—' (Residual Value Cap reached)
        [EffectiveDepreciationPercent] DECIMAL(18,2)  NOT NULL,  -- Explicit rate or (100 - ResidualValuePercent)
        [IsResidualFloorCap]           BIT            NOT NULL CONSTRAINT [DF_BuildingDepreciationSchedules_IsCap] DEFAULT (0),
        [RevisionYear]                 INT            NOT NULL CONSTRAINT [DF_BuildingDepreciationSchedules_RevYear] DEFAULT (2024),
        [SortOrder]                    INT            NOT NULL CONSTRAINT [DF_BuildingDepreciationSchedules_SortOrder] DEFAULT (0),
        [IsActive]                     BIT            NOT NULL CONSTRAINT [DF_BuildingDepreciationSchedules_IsActive] DEFAULT (1),
        CONSTRAINT [PK_BuildingDepreciationSchedules] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_BuildingDepreciationSchedules_Class_Age]
        ON [dbo].[BuildingDepreciationSchedules] ([ClassificationCode] ASC, [AgeLowerBound] ASC, [AgeUpperBound] ASC);
END
GO

-- -----------------------------------------------------------------------------
-- 2. SEED ALL 180 SCHEDULE VALUES (20 AGE ROWS x 9 CLASSIFICATIONS I TO V-B)
-- -----------------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[BuildingDepreciationSchedules] ON;

MERGE INTO [dbo].[BuildingDepreciationSchedules] AS Target
USING (VALUES
    -- =========================================================================
    -- 1st 3 yrs of GR (Building Age 1, 2, 3)
    -- =========================================================================
    -- Age 1 (1..1)
    (  1, N'1st 3 yrs of GR', N'1', 1, 1, N'I',     N'Type I',   15, 10.00,  6.70,  6.70, 0, 2024,   1, 1),
    (  2, N'1st 3 yrs of GR', N'1', 1, 1, N'II-A',  N'Type II',  20, 15.00,  5.00,  5.00, 0, 2024,   2, 1),
    (  3, N'1st 3 yrs of GR', N'1', 1, 1, N'II-B',  N'Type II',  18, 12.00,  5.00,  5.00, 0, 2024,   3, 1),
    (  4, N'1st 3 yrs of GR', N'1', 1, 1, N'III-A', N'Type III', 30, 25.00,  3.30,  3.30, 0, 2024,   4, 1),
    (  5, N'1st 3 yrs of GR', N'1', 1, 1, N'III-B', N'Type III', 25, 20.00,  4.00,  4.00, 0, 2024,   5, 1),
    (  6, N'1st 3 yrs of GR', N'1', 1, 1, N'IV-A',  N'Type IV',  40, 35.00,  2.50,  2.50, 0, 2024,   6, 1),
    (  7, N'1st 3 yrs of GR', N'1', 1, 1, N'IV-B',  N'Type IV',  38, 33.00,  2.60,  2.60, 0, 2024,   7, 1),
    (  8, N'1st 3 yrs of GR', N'1', 1, 1, N'V-A',   N'Type V',   50, 40.00,  2.00,  2.00, 0, 2024,   8, 1),
    (  9, N'1st 3 yrs of GR', N'1', 1, 1, N'V-B',   N'Type V',   45, 37.00,  2.20,  2.20, 0, 2024,   9, 1),

    -- Age 2 (2..2)
    ( 10, N'1st 3 yrs of GR', N'2', 2, 2, N'I',     N'Type I',   15, 10.00, 13.30, 13.30, 0, 2024,  10, 1),
    ( 11, N'1st 3 yrs of GR', N'2', 2, 2, N'II-A',  N'Type II',  20, 15.00, 10.00, 10.00, 0, 2024,  11, 1),
    ( 12, N'1st 3 yrs of GR', N'2', 2, 2, N'II-B',  N'Type II',  18, 12.00, 11.10, 11.10, 0, 2024,  12, 1),
    ( 13, N'1st 3 yrs of GR', N'2', 2, 2, N'III-A', N'Type III', 30, 25.00,  6.70,  6.70, 0, 2024,  13, 1),
    ( 14, N'1st 3 yrs of GR', N'2', 2, 2, N'III-B', N'Type III', 25, 20.00,  8.00,  8.00, 0, 2024,  14, 1),
    ( 15, N'1st 3 yrs of GR', N'2', 2, 2, N'IV-A',  N'Type IV',  40, 35.00,  5.00,  5.00, 0, 2024,  15, 1),
    ( 16, N'1st 3 yrs of GR', N'2', 2, 2, N'IV-B',  N'Type IV',  38, 33.00,  5.30,  5.30, 0, 2024,  16, 1),
    ( 17, N'1st 3 yrs of GR', N'2', 2, 2, N'V-A',   N'Type V',   50, 40.00,  4.00,  4.00, 0, 2024,  17, 1),
    ( 18, N'1st 3 yrs of GR', N'2', 2, 2, N'V-B',   N'Type V',   45, 37.00,  4.40,  4.40, 0, 2024,  18, 1),

    -- Age 3 (3..3)
    ( 19, N'1st 3 yrs of GR', N'3', 3, 3, N'I',     N'Type I',   15, 10.00, 20.00, 20.00, 0, 2024,  19, 1),
    ( 20, N'1st 3 yrs of GR', N'3', 3, 3, N'II-A',  N'Type II',  20, 15.00, 15.00, 15.00, 0, 2024,  20, 1),
    ( 21, N'1st 3 yrs of GR', N'3', 3, 3, N'II-B',  N'Type II',  18, 12.00, 16.70, 16.70, 0, 2024,  21, 1),
    ( 22, N'1st 3 yrs of GR', N'3', 3, 3, N'III-A', N'Type III', 30, 25.00, 10.00, 10.00, 0, 2024,  22, 1),
    ( 23, N'1st 3 yrs of GR', N'3', 3, 3, N'III-B', N'Type III', 25, 20.00, 12.00, 12.00, 0, 2024,  23, 1),
    ( 24, N'1st 3 yrs of GR', N'3', 3, 3, N'IV-A',  N'Type IV',  40, 35.00,  7.50,  7.50, 0, 2024,  24, 1),
    ( 25, N'1st 3 yrs of GR', N'3', 3, 3, N'IV-B',  N'Type IV',  38, 33.00,  7.90,  7.90, 0, 2024,  25, 1),
    ( 26, N'1st 3 yrs of GR', N'3', 3, 3, N'V-A',   N'Type V',   50, 40.00,  6.00,  6.00, 0, 2024,  26, 1),
    ( 27, N'1st 3 yrs of GR', N'3', 3, 3, N'V-B',   N'Type V',   45, 37.00,  6.70,  6.70, 0, 2024,  27, 1),

    -- =========================================================================
    -- 2nd 3 yrs of GR (Building Age 4, 5, 6)
    -- =========================================================================
    -- Age 4 (4..4)
    ( 28, N'2nd 3 yrs of GR', N'4', 4, 4, N'I',     N'Type I',   15, 10.00, 26.70, 26.70, 0, 2024,  28, 1),
    ( 29, N'2nd 3 yrs of GR', N'4', 4, 4, N'II-A',  N'Type II',  20, 15.00, 20.00, 20.00, 0, 2024,  29, 1),
    ( 30, N'2nd 3 yrs of GR', N'4', 4, 4, N'II-B',  N'Type II',  18, 12.00, 22.20, 22.20, 0, 2024,  30, 1),
    ( 31, N'2nd 3 yrs of GR', N'4', 4, 4, N'III-A', N'Type III', 30, 25.00, 13.30, 13.30, 0, 2024,  31, 1),
    ( 32, N'2nd 3 yrs of GR', N'4', 4, 4, N'III-B', N'Type III', 25, 20.00, 16.00, 16.00, 0, 2024,  32, 1),
    ( 33, N'2nd 3 yrs of GR', N'4', 4, 4, N'IV-A',  N'Type IV',  40, 35.00, 10.00, 10.00, 0, 2024,  33, 1),
    ( 34, N'2nd 3 yrs of GR', N'4', 4, 4, N'IV-B',  N'Type IV',  38, 33.00, 10.50, 10.50, 0, 2024,  34, 1),
    ( 35, N'2nd 3 yrs of GR', N'4', 4, 4, N'V-A',   N'Type V',   50, 40.00,  8.00,  8.00, 0, 2024,  35, 1),
    ( 36, N'2nd 3 yrs of GR', N'4', 4, 4, N'V-B',   N'Type V',   45, 37.00,  8.90,  8.90, 0, 2024,  36, 1),

    -- Age 5 (5..5)
    ( 37, N'2nd 3 yrs of GR', N'5', 5, 5, N'I',     N'Type I',   15, 10.00, 33.30, 33.30, 0, 2024,  37, 1),
    ( 38, N'2nd 3 yrs of GR', N'5', 5, 5, N'II-A',  N'Type II',  20, 15.00, 25.00, 25.00, 0, 2024,  38, 1),
    ( 39, N'2nd 3 yrs of GR', N'5', 5, 5, N'II-B',  N'Type II',  18, 12.00, 27.80, 27.80, 0, 2024,  39, 1),
    ( 40, N'2nd 3 yrs of GR', N'5', 5, 5, N'III-A', N'Type III', 30, 25.00, 16.70, 16.70, 0, 2024,  40, 1),
    ( 41, N'2nd 3 yrs of GR', N'5', 5, 5, N'III-B', N'Type III', 25, 20.00, 20.00, 20.00, 0, 2024,  41, 1),
    ( 42, N'2nd 3 yrs of GR', N'5', 5, 5, N'IV-A',  N'Type IV',  40, 35.00, 12.50, 12.50, 0, 2024,  42, 1),
    ( 43, N'2nd 3 yrs of GR', N'5', 5, 5, N'IV-B',  N'Type IV',  38, 33.00, 13.20, 13.20, 0, 2024,  43, 1),
    ( 44, N'2nd 3 yrs of GR', N'5', 5, 5, N'V-A',   N'Type V',   50, 40.00, 10.00, 10.00, 0, 2024,  44, 1),
    ( 45, N'2nd 3 yrs of GR', N'5', 5, 5, N'V-B',   N'Type V',   45, 37.00, 11.10, 11.10, 0, 2024,  45, 1),

    -- Age 6 (6..6)
    ( 46, N'2nd 3 yrs of GR', N'6', 6, 6, N'I',     N'Type I',   15, 10.00, 40.00, 40.00, 0, 2024,  46, 1),
    ( 47, N'2nd 3 yrs of GR', N'6', 6, 6, N'II-A',  N'Type II',  20, 15.00, 30.00, 30.00, 0, 2024,  47, 1),
    ( 48, N'2nd 3 yrs of GR', N'6', 6, 6, N'II-B',  N'Type II',  18, 12.00, 33.30, 33.30, 0, 2024,  48, 1),
    ( 49, N'2nd 3 yrs of GR', N'6', 6, 6, N'III-A', N'Type III', 30, 25.00, 20.00, 20.00, 0, 2024,  49, 1),
    ( 50, N'2nd 3 yrs of GR', N'6', 6, 6, N'III-B', N'Type III', 25, 20.00, 24.00, 24.00, 0, 2024,  50, 1),
    ( 51, N'2nd 3 yrs of GR', N'6', 6, 6, N'IV-A',  N'Type IV',  40, 35.00, 15.00, 15.00, 0, 2024,  51, 1),
    ( 52, N'2nd 3 yrs of GR', N'6', 6, 6, N'IV-B',  N'Type IV',  38, 33.00, 15.80, 15.80, 0, 2024,  52, 1),
    ( 53, N'2nd 3 yrs of GR', N'6', 6, 6, N'V-A',   N'Type V',   50, 40.00, 12.00, 12.00, 0, 2024,  53, 1),
    ( 54, N'2nd 3 yrs of GR', N'6', 6, 6, N'V-B',   N'Type V',   45, 37.00, 13.30, 13.30, 0, 2024,  54, 1),

    -- =========================================================================
    -- 3rd 3 yrs of GR (Building Age 7, 8, 9)
    -- =========================================================================
    -- Age 7 (7..7)
    ( 55, N'3rd 3 yrs of GR', N'7', 7, 7, N'I',     N'Type I',   15, 10.00, 46.70, 46.70, 0, 2024,  55, 1),
    ( 56, N'3rd 3 yrs of GR', N'7', 7, 7, N'II-A',  N'Type II',  20, 15.00, 35.00, 35.00, 0, 2024,  56, 1),
    ( 57, N'3rd 3 yrs of GR', N'7', 7, 7, N'II-B',  N'Type II',  18, 12.00, 38.90, 38.90, 0, 2024,  57, 1),
    ( 58, N'3rd 3 yrs of GR', N'7', 7, 7, N'III-A', N'Type III', 30, 25.00, 23.30, 23.30, 0, 2024,  58, 1),
    ( 59, N'3rd 3 yrs of GR', N'7', 7, 7, N'III-B', N'Type III', 25, 20.00, 28.00, 28.00, 0, 2024,  59, 1),
    ( 60, N'3rd 3 yrs of GR', N'7', 7, 7, N'IV-A',  N'Type IV',  40, 35.00, 17.50, 17.50, 0, 2024,  60, 1),
    ( 61, N'3rd 3 yrs of GR', N'7', 7, 7, N'IV-B',  N'Type IV',  38, 33.00, 18.40, 18.40, 0, 2024,  61, 1),
    ( 62, N'3rd 3 yrs of GR', N'7', 7, 7, N'V-A',   N'Type V',   50, 40.00, 14.00, 14.00, 0, 2024,  62, 1),
    ( 63, N'3rd 3 yrs of GR', N'7', 7, 7, N'V-B',   N'Type V',   45, 37.00, 15.60, 15.60, 0, 2024,  63, 1),

    -- Age 8 (8..8)
    ( 64, N'3rd 3 yrs of GR', N'8', 8, 8, N'I',     N'Type I',   15, 10.00, 53.30, 53.30, 0, 2024,  64, 1),
    ( 65, N'3rd 3 yrs of GR', N'8', 8, 8, N'II-A',  N'Type II',  20, 15.00, 40.00, 40.00, 0, 2024,  65, 1),
    ( 66, N'3rd 3 yrs of GR', N'8', 8, 8, N'II-B',  N'Type II',  18, 12.00, 44.40, 44.40, 0, 2024,  66, 1),
    ( 67, N'3rd 3 yrs of GR', N'8', 8, 8, N'III-A', N'Type III', 30, 25.00, 26.70, 26.70, 0, 2024,  67, 1),
    ( 68, N'3rd 3 yrs of GR', N'8', 8, 8, N'III-B', N'Type III', 25, 20.00, 32.00, 32.00, 0, 2024,  68, 1),
    ( 69, N'3rd 3 yrs of GR', N'8', 8, 8, N'IV-A',  N'Type IV',  40, 35.00, 20.00, 20.00, 0, 2024,  69, 1),
    ( 70, N'3rd 3 yrs of GR', N'8', 8, 8, N'IV-B',  N'Type IV',  38, 33.00, 21.10, 21.10, 0, 2024,  70, 1),
    ( 71, N'3rd 3 yrs of GR', N'8', 8, 8, N'V-A',   N'Type V',   50, 40.00, 16.00, 16.00, 0, 2024,  71, 1),
    ( 72, N'3rd 3 yrs of GR', N'8', 8, 8, N'V-B',   N'Type V',   45, 37.00, 17.80, 17.80, 0, 2024,  72, 1),

    -- Age 9 (9..9)
    ( 73, N'3rd 3 yrs of GR', N'9', 9, 9, N'I',     N'Type I',   15, 10.00, 60.00, 60.00, 0, 2024,  73, 1),
    ( 74, N'3rd 3 yrs of GR', N'9', 9, 9, N'II-A',  N'Type II',  20, 15.00, 45.00, 45.00, 0, 2024,  74, 1),
    ( 75, N'3rd 3 yrs of GR', N'9', 9, 9, N'II-B',  N'Type II',  18, 12.00, 50.00, 50.00, 0, 2024,  75, 1),
    ( 76, N'3rd 3 yrs of GR', N'9', 9, 9, N'III-A', N'Type III', 30, 25.00, 30.00, 30.00, 0, 2024,  76, 1),
    ( 77, N'3rd 3 yrs of GR', N'9', 9, 9, N'III-B', N'Type III', 25, 20.00, 36.00, 36.00, 0, 2024,  77, 1),
    ( 78, N'3rd 3 yrs of GR', N'9', 9, 9, N'IV-A',  N'Type IV',  40, 35.00, 22.50, 22.50, 0, 2024,  78, 1),
    ( 79, N'3rd 3 yrs of GR', N'9', 9, 9, N'IV-B',  N'Type IV',  38, 33.00, 23.70, 23.70, 0, 2024,  79, 1),
    ( 80, N'3rd 3 yrs of GR', N'9', 9, 9, N'V-A',   N'Type V',   50, 40.00, 18.00, 18.00, 0, 2024,  80, 1),
    ( 81, N'3rd 3 yrs of GR', N'9', 9, 9, N'V-B',   N'Type V',   45, 37.00, 20.00, 20.00, 0, 2024,  81, 1),

    -- =========================================================================
    -- 4th 3 yrs of GR (Building Age 10, 11, 12)
    -- =========================================================================
    -- Age 10 (10..10)
    ( 82, N'4th 3 yrs of GR', N'10', 10, 10, N'I',     N'Type I',   15, 10.00, 66.70, 66.70, 0, 2024,  82, 1),
    ( 83, N'4th 3 yrs of GR', N'10', 10, 10, N'II-A',  N'Type II',  20, 15.00, 50.00, 50.00, 0, 2024,  83, 1),
    ( 84, N'4th 3 yrs of GR', N'10', 10, 10, N'II-B',  N'Type II',  18, 12.00, 55.60, 55.60, 0, 2024,  84, 1),
    ( 85, N'4th 3 yrs of GR', N'10', 10, 10, N'III-A', N'Type III', 30, 25.00, 33.30, 33.30, 0, 2024,  85, 1),
    ( 86, N'4th 3 yrs of GR', N'10', 10, 10, N'III-B', N'Type III', 25, 20.00, 40.00, 40.00, 0, 2024,  86, 1),
    ( 87, N'4th 3 yrs of GR', N'10', 10, 10, N'IV-A',  N'Type IV',  40, 35.00, 25.00, 25.00, 0, 2024,  87, 1),
    ( 88, N'4th 3 yrs of GR', N'10', 10, 10, N'IV-B',  N'Type IV',  38, 33.00, 26.30, 26.30, 0, 2024,  88, 1),
    ( 89, N'4th 3 yrs of GR', N'10', 10, 10, N'V-A',   N'Type V',   50, 40.00, 20.00, 20.00, 0, 2024,  89, 1),
    ( 90, N'4th 3 yrs of GR', N'10', 10, 10, N'V-B',   N'Type V',   45, 37.00, 22.20, 22.20, 0, 2024,  90, 1),

    -- Age 11 (11..11)
    ( 91, N'4th 3 yrs of GR', N'11', 11, 11, N'I',     N'Type I',   15, 10.00, 73.30, 73.30, 0, 2024,  91, 1),
    ( 92, N'4th 3 yrs of GR', N'11', 11, 11, N'II-A',  N'Type II',  20, 15.00, 55.00, 55.00, 0, 2024,  92, 1),
    ( 93, N'4th 3 yrs of GR', N'11', 11, 11, N'II-B',  N'Type II',  18, 12.00, 61.10, 61.10, 0, 2024,  93, 1),
    ( 94, N'4th 3 yrs of GR', N'11', 11, 11, N'III-A', N'Type III', 30, 25.00, 36.70, 36.70, 0, 2024,  94, 1),
    ( 95, N'4th 3 yrs of GR', N'11', 11, 11, N'III-B', N'Type III', 25, 20.00, 44.00, 44.00, 0, 2024,  95, 1),
    ( 96, N'4th 3 yrs of GR', N'11', 11, 11, N'IV-A',  N'Type IV',  40, 35.00, 27.50, 27.50, 0, 2024,  96, 1),
    ( 97, N'4th 3 yrs of GR', N'11', 11, 11, N'IV-B',  N'Type IV',  38, 33.00, 28.90, 28.90, 0, 2024,  97, 1),
    ( 98, N'4th 3 yrs of GR', N'11', 11, 11, N'V-A',   N'Type V',   50, 40.00, 22.00, 22.00, 0, 2024,  98, 1),
    ( 99, N'4th 3 yrs of GR', N'11', 11, 11, N'V-B',   N'Type V',   45, 37.00, 24.40, 24.40, 0, 2024,  99, 1),

    -- Age 12 (12..12)
    (100, N'4th 3 yrs of GR', N'12', 12, 12, N'I',     N'Type I',   15, 10.00, 80.00, 80.00, 0, 2024, 100, 1),
    (101, N'4th 3 yrs of GR', N'12', 12, 12, N'II-A',  N'Type II',  20, 15.00, 60.00, 60.00, 0, 2024, 101, 1),
    (102, N'4th 3 yrs of GR', N'12', 12, 12, N'II-B',  N'Type II',  18, 12.00, 66.70, 66.70, 0, 2024, 102, 1),
    (103, N'4th 3 yrs of GR', N'12', 12, 12, N'III-A', N'Type III', 30, 25.00, 40.00, 40.00, 0, 2024, 103, 1),
    (104, N'4th 3 yrs of GR', N'12', 12, 12, N'III-B', N'Type III', 25, 20.00, 48.00, 48.00, 0, 2024, 104, 1),
    (105, N'4th 3 yrs of GR', N'12', 12, 12, N'IV-A',  N'Type IV',  40, 35.00, 30.00, 30.00, 0, 2024, 105, 1),
    (106, N'4th 3 yrs of GR', N'12', 12, 12, N'IV-B',  N'Type IV',  38, 33.00, 31.60, 31.60, 0, 2024, 106, 1),
    (107, N'4th 3 yrs of GR', N'12', 12, 12, N'V-A',   N'Type V',   50, 40.00, 24.00, 24.00, 0, 2024, 107, 1),
    (108, N'4th 3 yrs of GR', N'12', 12, 12, N'V-B',   N'Type V',   45, 37.00, 26.70, 26.70, 0, 2024, 108, 1),

    -- =========================================================================
    -- 5th 3 yrs of GR (Building Age 13, 14, 15)
    -- =========================================================================
    -- Age 13 (13..13)
    (109, N'5th 3 yrs of GR', N'13', 13, 13, N'I',     N'Type I',   15, 10.00, 86.70, 86.70, 0, 2024, 109, 1),
    (110, N'5th 3 yrs of GR', N'13', 13, 13, N'II-A',  N'Type II',  20, 15.00, 65.00, 65.00, 0, 2024, 110, 1),
    (111, N'5th 3 yrs of GR', N'13', 13, 13, N'II-B',  N'Type II',  18, 12.00, 72.20, 72.20, 0, 2024, 111, 1),
    (112, N'5th 3 yrs of GR', N'13', 13, 13, N'III-A', N'Type III', 30, 25.00, 43.30, 43.30, 0, 2024, 112, 1),
    (113, N'5th 3 yrs of GR', N'13', 13, 13, N'III-B', N'Type III', 25, 20.00, 52.00, 52.00, 0, 2024, 113, 1),
    (114, N'5th 3 yrs of GR', N'13', 13, 13, N'IV-A',  N'Type IV',  40, 35.00, 32.50, 32.50, 0, 2024, 114, 1),
    (115, N'5th 3 yrs of GR', N'13', 13, 13, N'IV-B',  N'Type IV',  38, 33.00, 34.20, 34.20, 0, 2024, 115, 1),
    (116, N'5th 3 yrs of GR', N'13', 13, 13, N'V-A',   N'Type V',   50, 40.00, 26.00, 26.00, 0, 2024, 116, 1),
    (117, N'5th 3 yrs of GR', N'13', 13, 13, N'V-B',   N'Type V',   45, 37.00, 28.90, 28.90, 0, 2024, 117, 1),

    -- Age 14 (14..14)
    (118, N'5th 3 yrs of GR', N'14', 14, 14, N'I',     N'Type I',   15, 10.00, 93.30, 93.30, 0, 2024, 118, 1),
    (119, N'5th 3 yrs of GR', N'14', 14, 14, N'II-A',  N'Type II',  20, 15.00, 70.00, 70.00, 0, 2024, 119, 1),
    (120, N'5th 3 yrs of GR', N'14', 14, 14, N'II-B',  N'Type II',  18, 12.00, 77.80, 77.80, 0, 2024, 120, 1),
    (121, N'5th 3 yrs of GR', N'14', 14, 14, N'III-A', N'Type III', 30, 25.00, 46.70, 46.70, 0, 2024, 121, 1),
    (122, N'5th 3 yrs of GR', N'14', 14, 14, N'III-B', N'Type III', 25, 20.00, 56.00, 56.00, 0, 2024, 122, 1),
    (123, N'5th 3 yrs of GR', N'14', 14, 14, N'IV-A',  N'Type IV',  40, 35.00, 35.00, 35.00, 0, 2024, 123, 1),
    (124, N'5th 3 yrs of GR', N'14', 14, 14, N'IV-B',  N'Type IV',  38, 33.00, 36.80, 36.80, 0, 2024, 124, 1),
    (125, N'5th 3 yrs of GR', N'14', 14, 14, N'V-A',   N'Type V',   50, 40.00, 28.00, 28.00, 0, 2024, 125, 1),
    (126, N'5th 3 yrs of GR', N'14', 14, 14, N'V-B',   N'Type V',   45, 37.00, 31.10, 31.10, 0, 2024, 126, 1),

    -- Age 15 (15..15)
    (127, N'5th 3 yrs of GR', N'15', 15, 15, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 127, 1),
    (128, N'5th 3 yrs of GR', N'15', 15, 15, N'II-A',  N'Type II',  20, 15.00, 75.00, 75.00, 0, 2024, 128, 1),
    (129, N'5th 3 yrs of GR', N'15', 15, 15, N'II-B',  N'Type II',  18, 12.00, 83.30, 83.30, 0, 2024, 129, 1),
    (130, N'5th 3 yrs of GR', N'15', 15, 15, N'III-A', N'Type III', 30, 25.00, 50.00, 50.00, 0, 2024, 130, 1),
    (131, N'5th 3 yrs of GR', N'15', 15, 15, N'III-B', N'Type III', 25, 20.00, 60.00, 60.00, 0, 2024, 131, 1),
    (132, N'5th 3 yrs of GR', N'15', 15, 15, N'IV-A',  N'Type IV',  40, 35.00, 37.50, 37.50, 0, 2024, 132, 1),
    (133, N'5th 3 yrs of GR', N'15', 15, 15, N'IV-B',  N'Type IV',  38, 33.00, 39.50, 39.50, 0, 2024, 133, 1),
    (134, N'5th 3 yrs of GR', N'15', 15, 15, N'V-A',   N'Type V',   50, 40.00, 30.00, 30.00, 0, 2024, 134, 1),
    (135, N'5th 3 yrs of GR', N'15', 15, 15, N'V-B',   N'Type V',   45, 37.00, 33.30, 33.30, 0, 2024, 135, 1),

    -- =========================================================================
    -- 6th 3 yrs of GR (Building Age 16, 17, 18–20)
    -- =========================================================================
    -- Age 16 (16..16)
    (136, N'6th 3 yrs of GR', N'16', 16, 16, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 136, 1),
    (137, N'6th 3 yrs of GR', N'16', 16, 16, N'II-A',  N'Type II',  20, 15.00, 80.00, 80.00, 0, 2024, 137, 1),
    (138, N'6th 3 yrs of GR', N'16', 16, 16, N'II-B',  N'Type II',  18, 12.00, 88.90, 88.90, 0, 2024, 138, 1),
    (139, N'6th 3 yrs of GR', N'16', 16, 16, N'III-A', N'Type III', 30, 25.00, 53.30, 53.30, 0, 2024, 139, 1),
    (140, N'6th 3 yrs of GR', N'16', 16, 16, N'III-B', N'Type III', 25, 20.00, 64.00, 64.00, 0, 2024, 140, 1),
    (141, N'6th 3 yrs of GR', N'16', 16, 16, N'IV-A',  N'Type IV',  40, 35.00, 40.00, 40.00, 0, 2024, 141, 1),
    (142, N'6th 3 yrs of GR', N'16', 16, 16, N'IV-B',  N'Type IV',  38, 33.00, 42.10, 42.10, 0, 2024, 142, 1),
    (143, N'6th 3 yrs of GR', N'16', 16, 16, N'V-A',   N'Type V',   50, 40.00, 32.00, 32.00, 0, 2024, 143, 1),
    (144, N'6th 3 yrs of GR', N'16', 16, 16, N'V-B',   N'Type V',   45, 37.00, 35.60, 35.60, 0, 2024, 144, 1),

    -- Age 17 (17..17)
    (145, N'6th 3 yrs of GR', N'17', 17, 17, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 145, 1),
    (146, N'6th 3 yrs of GR', N'17', 17, 17, N'II-A',  N'Type II',  20, 15.00, 85.00, 85.00, 0, 2024, 146, 1),
    (147, N'6th 3 yrs of GR', N'17', 17, 17, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 147, 1),
    (148, N'6th 3 yrs of GR', N'17', 17, 17, N'III-A', N'Type III', 30, 25.00, 56.70, 56.70, 0, 2024, 148, 1),
    (149, N'6th 3 yrs of GR', N'17', 17, 17, N'III-B', N'Type III', 25, 20.00, 68.00, 68.00, 0, 2024, 149, 1),
    (150, N'6th 3 yrs of GR', N'17', 17, 17, N'IV-A',  N'Type IV',  40, 35.00, 42.50, 42.50, 0, 2024, 150, 1),
    (151, N'6th 3 yrs of GR', N'17', 17, 17, N'IV-B',  N'Type IV',  38, 33.00, 44.70, 44.70, 0, 2024, 151, 1),
    (152, N'6th 3 yrs of GR', N'17', 17, 17, N'V-A',   N'Type V',   50, 40.00, 34.00, 34.00, 0, 2024, 152, 1),
    (153, N'6th 3 yrs of GR', N'17', 17, 17, N'V-B',   N'Type V',   45, 37.00, 37.80, 37.80, 0, 2024, 153, 1),

    -- Age 18–20 (18..20)
    (154, N'6th 3 yrs of GR', N'18–20', 18, 20, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 154, 1),
    (155, N'6th 3 yrs of GR', N'18–20', 18, 20, N'II-A',  N'Type II',  20, 15.00,  NULL, 85.00, 1, 2024, 155, 1),
    (156, N'6th 3 yrs of GR', N'18–20', 18, 20, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 156, 1),
    (157, N'6th 3 yrs of GR', N'18–20', 18, 20, N'III-A', N'Type III', 30, 25.00, 66.70, 66.70, 0, 2024, 157, 1),
    (158, N'6th 3 yrs of GR', N'18–20', 18, 20, N'III-B', N'Type III', 25, 20.00, 80.00, 80.00, 0, 2024, 158, 1),
    (159, N'6th 3 yrs of GR', N'18–20', 18, 20, N'IV-A',  N'Type IV',  40, 35.00, 50.00, 50.00, 0, 2024, 159, 1),
    (160, N'6th 3 yrs of GR', N'18–20', 18, 20, N'IV-B',  N'Type IV',  38, 33.00, 52.60, 52.60, 0, 2024, 160, 1),
    (161, N'6th 3 yrs of GR', N'18–20', 18, 20, N'V-A',   N'Type V',   50, 40.00, 40.00, 40.00, 0, 2024, 161, 1),
    (162, N'6th 3 yrs of GR', N'18–20', 18, 20, N'V-B',   N'Type V',   45, 37.00, 44.40, 44.40, 0, 2024, 162, 1),

    -- =========================================================================
    -- After 20 yrs GR (Building Age 21–23, 24–26, 27–30)
    -- =========================================================================
    -- Age 21–23 (21..23)
    (163, N'After 20 yrs GR', N'21–23', 21, 23, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 163, 1),
    (164, N'After 20 yrs GR', N'21–23', 21, 23, N'II-A',  N'Type II',  20, 15.00,  NULL, 85.00, 1, 2024, 164, 1),
    (165, N'After 20 yrs GR', N'21–23', 21, 23, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 165, 1),
    (166, N'After 20 yrs GR', N'21–23', 21, 23, N'III-A', N'Type III', 30, 25.00, 76.70, 76.70, 0, 2024, 166, 1),
    (167, N'After 20 yrs GR', N'21–23', 21, 23, N'III-B', N'Type III', 25, 20.00, 76.50, 76.50, 0, 2024, 167, 1),
    (168, N'After 20 yrs GR', N'21–23', 21, 23, N'IV-A',  N'Type IV',  40, 35.00, 57.50, 57.50, 0, 2024, 168, 1),
    (169, N'After 20 yrs GR', N'21–23', 21, 23, N'IV-B',  N'Type IV',  38, 33.00, 60.50, 60.50, 0, 2024, 169, 1),
    (170, N'After 20 yrs GR', N'21–23', 21, 23, N'V-A',   N'Type V',   50, 40.00, 46.00, 46.00, 0, 2024, 170, 1),
    (171, N'After 20 yrs GR', N'21–23', 21, 23, N'V-B',   N'Type V',   45, 37.00, 51.10, 51.10, 0, 2024, 171, 1),

    -- Age 24–26 (24..26)
    (172, N'After 20 yrs GR', N'24–26', 24, 26, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 172, 1),
    (173, N'After 20 yrs GR', N'24–26', 24, 26, N'II-A',  N'Type II',  20, 15.00,  NULL, 85.00, 1, 2024, 173, 1),
    (174, N'After 20 yrs GR', N'24–26', 24, 26, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 174, 1),
    (175, N'After 20 yrs GR', N'24–26', 24, 26, N'III-A', N'Type III', 30, 25.00,  NULL, 75.00, 1, 2024, 175, 1),
    (176, N'After 20 yrs GR', N'24–26', 24, 26, N'III-B', N'Type III', 25, 20.00, 80.00, 80.00, 0, 2024, 176, 1),
    (177, N'After 20 yrs GR', N'24–26', 24, 26, N'IV-A',  N'Type IV',  40, 35.00, 65.00, 65.00, 0, 2024, 177, 1),
    (178, N'After 20 yrs GR', N'24–26', 24, 26, N'IV-B',  N'Type IV',  38, 33.00, 68.40, 68.40, 0, 2024, 178, 1),
    (179, N'After 20 yrs GR', N'24–26', 24, 26, N'V-A',   N'Type V',   50, 40.00, 52.00, 52.00, 0, 2024, 179, 1),
    (180, N'After 20 yrs GR', N'24–26', 24, 26, N'V-B',   N'Type V',   45, 37.00, 57.80, 57.80, 0, 2024, 180, 1),

    -- Age 27–30 (27..30)
    (181, N'After 20 yrs GR', N'27–30', 27, 30, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 181, 1),
    (182, N'After 20 yrs GR', N'27–30', 27, 30, N'II-A',  N'Type II',  20, 15.00,  NULL, 85.00, 1, 2024, 182, 1),
    (183, N'After 20 yrs GR', N'27–30', 27, 30, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 183, 1),
    (184, N'After 20 yrs GR', N'27–30', 27, 30, N'III-A', N'Type III', 30, 25.00,  NULL, 75.00, 1, 2024, 184, 1),
    (185, N'After 20 yrs GR', N'27–30', 27, 30, N'III-B', N'Type III', 25, 20.00,  NULL, 80.00, 1, 2024, 185, 1),
    (186, N'After 20 yrs GR', N'27–30', 27, 30, N'IV-A',  N'Type IV',  40, 35.00,  NULL, 65.00, 1, 2024, 186, 1),
    (187, N'After 20 yrs GR', N'27–30', 27, 30, N'IV-B',  N'Type IV',  38, 33.00,  NULL, 67.00, 1, 2024, 187, 1),
    (188, N'After 20 yrs GR', N'27–30', 27, 30, N'V-A',   N'Type V',   50, 40.00, 60.00, 60.00, 0, 2024, 188, 1),
    (189, N'After 20 yrs GR', N'27–30', 27, 30, N'V-B',   N'Type V',   45, 37.00, 64.40, 64.40, 0, 2024, 189, 1)
) AS Source (
    [Id],
    [GeneralRevisionCycle],
    [BuildingAgeLabel],
    [AgeLowerBound],
    [AgeUpperBound],
    [ClassificationCode],
    [StructuralType],
    [EstimatedEconomicLifeYears],
    [EstimatedResidualValuePercent],
    [DepreciationPercent],
    [EffectiveDepreciationPercent],
    [IsResidualFloorCap],
    [RevisionYear],
    [SortOrder],
    [IsActive]
)
ON Target.[Id] = Source.[Id]
WHEN MATCHED THEN
    UPDATE SET
        Target.[GeneralRevisionCycle]          = Source.[GeneralRevisionCycle],
        Target.[BuildingAgeLabel]              = Source.[BuildingAgeLabel],
        Target.[AgeLowerBound]                 = Source.[AgeLowerBound],
        Target.[AgeUpperBound]                 = Source.[AgeUpperBound],
        Target.[ClassificationCode]            = Source.[ClassificationCode],
        Target.[StructuralType]                = Source.[StructuralType],
        Target.[EstimatedEconomicLifeYears]    = Source.[EstimatedEconomicLifeYears],
        Target.[EstimatedResidualValuePercent] = Source.[EstimatedResidualValuePercent],
        Target.[DepreciationPercent]           = Source.[DepreciationPercent],
        Target.[EffectiveDepreciationPercent]  = Source.[EffectiveDepreciationPercent],
        Target.[IsResidualFloorCap]            = Source.[IsResidualFloorCap],
        Target.[RevisionYear]                  = Source.[RevisionYear],
        Target.[SortOrder]                     = Source.[SortOrder],
        Target.[IsActive]                      = Source.[IsActive]
WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Id],
        [GeneralRevisionCycle],
        [BuildingAgeLabel],
        [AgeLowerBound],
        [AgeUpperBound],
        [ClassificationCode],
        [StructuralType],
        [EstimatedEconomicLifeYears],
        [EstimatedResidualValuePercent],
        [DepreciationPercent],
        [EffectiveDepreciationPercent],
        [IsResidualFloorCap],
        [RevisionYear],
        [SortOrder],
        [IsActive]
    )
    VALUES (
        Source.[Id],
        Source.[GeneralRevisionCycle],
        Source.[BuildingAgeLabel],
        Source.[AgeLowerBound],
        Source.[AgeUpperBound],
        Source.[ClassificationCode],
        Source.[StructuralType],
        Source.[EstimatedEconomicLifeYears],
        Source.[EstimatedResidualValuePercent],
        Source.[DepreciationPercent],
        Source.[EffectiveDepreciationPercent],
        Source.[IsResidualFloorCap],
        Source.[RevisionYear],
        Source.[SortOrder],
        Source.[IsActive]
    );

SET IDENTITY_INSERT [dbo].[BuildingDepreciationSchedules] OFF;
GO

-- -----------------------------------------------------------------------------
-- 3. APPLY SCHEDULE COLUMNS & FOREIGN KEY TO [dbo].[TDNBuildings]
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[TDNBuildings]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.TDNBuildings', N'BuildingDepreciationScheduleId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings]
        ADD [BuildingDepreciationScheduleId] INT NULL;
    END

    IF COL_LENGTH(N'dbo.TDNBuildings', N'GeneralRevisionCycle') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings]
        ADD [GeneralRevisionCycle] NVARCHAR(50) NULL;
    END

    IF COL_LENGTH(N'dbo.TDNBuildings', N'EstimatedEconomicLifeYears') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings]
        ADD [EstimatedEconomicLifeYears] INT NOT NULL CONSTRAINT [DF_TDNBuildings_EstEconLife] DEFAULT (30);
    END

    IF COL_LENGTH(N'dbo.TDNBuildings', N'EstimatedResidualValuePercent') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings]
        ADD [EstimatedResidualValuePercent] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_TDNBuildings_EstResidualVal] DEFAULT (25.00);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_TDNBuildings_BuildingDepreciationSchedules_BuildingDepreciationScheduleId'
    )
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings] WITH CHECK
        ADD CONSTRAINT [FK_TDNBuildings_BuildingDepreciationSchedules_BuildingDepreciationScheduleId]
        FOREIGN KEY ([BuildingDepreciationScheduleId])
        REFERENCES [dbo].[BuildingDepreciationSchedules] ([Id])
        ON DELETE SET NULL;
    END
END
GO

-- -----------------------------------------------------------------------------
-- 4. BACKFILL EXISTING [dbo].[TDNBuildings] RECORDS WITH MATCHING SCHEDULE ROWS
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[TDNBuildings]', N'U') IS NOT NULL
BEGIN
    UPDATE b
    SET
        b.[BuildingDepreciationScheduleId] = s.[Id],
        b.[GeneralRevisionCycle]           = s.[GeneralRevisionCycle],
        b.[EstimatedEconomicLifeYears]     = s.[EstimatedEconomicLifeYears],
        b.[EstimatedResidualValuePercent]  = s.[EstimatedResidualValuePercent],
        b.[BuildingDepreciationPercent]    = CASE
                                                 WHEN b.[BuildingDepreciationPercent] <= 0 THEN s.[EffectiveDepreciationPercent]
                                                 ELSE b.[BuildingDepreciationPercent]
                                             END
    FROM [dbo].[TDNBuildings] b
    INNER JOIN [dbo].[BuildingDepreciationSchedules] s
        ON s.[ClassificationCode] = ISNULL(NULLIF(LTRIM(RTRIM(b.[BuildingClassification])), N''), N'III-A')
       AND (
            (b.[BuildingAgeYears] >= s.[AgeLowerBound] AND b.[BuildingAgeYears] <= s.[AgeUpperBound])
            OR (b.[BuildingAgeYears] > 30 AND s.[AgeUpperBound] = 30)
           )
    WHERE b.[BuildingAgeYears] > 0;
END
GO

GO


-- ==========================================================================================
-- SECTION: 21_UpdateBuildingStructuralMaterials.sql
-- ==========================================================================================
PRINT 'Executing section: 21_UpdateBuildingStructuralMaterials.sql...';
GO
-- ==========================================================================================
-- Script: 21_UpdateBuildingStructuralMaterials.sql
-- Description: Updates [dbo].[TDNBuildingStructuralMaterials] based on Local Assessment Manual
--              "Table 3. List of Structural Materials Codes" (16 Structural Parts):
--              1. Foundation          (RC, PC) - removed Stone / Rubble Footing
--              2. Columns             (S, RC, W)
--              3. Beams               (S, W, RC)
--              4. Roof Framing        (S, W, RC)
--              5. Roofing             (CGI, SPC, As, CT, CS, Al, LGI, N, An, C, PR)
--              6. Exterior Walls      (PC, ALP, W, GI, CHB, RC, BW, B, Sa)
--              7. Flooring            (PC, W, M, T, T&G, RC)
--              8. Doors               (S, Al, G, Gr, PVC, WP, FD, FO) - separated Aluminum & Glass
--              9. Ceiling             (Pw, L, As, D, SR, Hx, AF, Sw, B)
--              10. Windows            (SC, SCG, AC, ACG, WC, WCG, G)
--              11. Stairs             (S, RC, W, Sn)
--              12. Partition          (RC, CHB, GI, Pw, Pb, W, Hx [Hardiflex / Dry wall], AIF)
--              13. Wall Finish        (CP, WP, T, WOP, AR) - Floor Finish removed
--              14. Electrical Conduit (EPVC, MM, WM) - Concealed Knob & Tube removed
--              15. Toilet and Bath    (CT, M, PC)
--              16. Plumbing and Services (SP, GIP, PVCP [Poly Vinyl Carbonate Pipe], CP, AP)
-- Database: FairTrackDB / MS SQL Server
-- ==========================================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Remove deprecated 'Floor Finish' rows and re-align all existing TDNBuildings
    --    to the official 16 Structural Parts from Table 3 (Local Assessment Manual)
    DELETE FROM [dbo].[TDNBuildingStructuralMaterials];

    -- 2. Insert the 16 official Structural Materials rows for each existing TDNBuilding
    INSERT INTO [dbo].[TDNBuildingStructuralMaterials] (
        [Id],
        [TDNBuildingId],
        [PartNumber],
        [PartName],
        [MaterialSelected],
        [SpecificDescription]
    )
    SELECT
        NEWID(),
        b.[Id],
        m.[PartNumber],
        m.[PartName],
        m.[MaterialSelected],
        m.[Code]
    FROM [dbo].[TDNBuildings] b
    CROSS JOIN (
        VALUES
            (1,  N'1. Foundation',             N'Reinforced Concrete (RC)',               N'RC'),
            (2,  N'2. Columns',                N'Reinforced Concrete (RC)',               N'RC'),
            (3,  N'3. Beams',                  N'Reinforced Concrete (RC)',               N'RC'),
            (4,  N'4. Roof Framing',           N'Steel (S)',                              N'S'),
            (5,  N'5. Roofing',                N'Long Span G.I. Sheet-Pre painted (LGI)', N'LGI'),
            (6,  N'6. Exterior Walls',         N'Concrete Hollow Block (CHB)',            N'CHB'),
            (7,  N'7. Flooring',               N'Reinforced Concrete (RC)',               N'RC'),
            (8,  N'8. Doors',                  N'Wood Panel (WP)',                        N'WP'),
            (9,  N'9. Ceiling',                N'Hardiflex (Hx)',                         N'Hx'),
            (10, N'10. Windows',               N'Aluminum Casement (AC)',                 N'AC'),
            (11, N'11. Stairs',                N'Reinforced concrete (RC)',               N'RC'),
            (12, N'12. Partition',             N'Hardiflex / Dry wall (Hx)',              N'Hx'),
            (13, N'13. Wall Finish',           N'Cement Plaster (CP)',                    N'CP'),
            (14, N'14. Electrical Conduit',    N'Poly Vinyl Carbonate Conduit (EPVC)',    N'EPVC'),
            (15, N'15. Toilet and Bath',       N'Cement Tiles (CT)',                      N'CT'),
            (16, N'16. Plumbing and Services', N'Poly Vinyl Carbonate Pipe (PVCP)',       N'PVCP')
    ) AS m([PartNumber], [PartName], [MaterialSelected], [Code]);

    COMMIT TRANSACTION;
    PRINT 'TDNBuildingStructuralMaterials updated successfully with Table 3 Structural Materials Codes (16 parts per building).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

GO


-- ==========================================================================================
-- SECTION: 22_SeedAllSampleData.sql
-- ==========================================================================================
PRINT 'Executing section: 22_SeedAllSampleData.sql...';
GO
-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 03: FULL SAMPLE DATA SEEDING (SQL SERVER / T-SQL COMPATIBLE)
-- Seeds Staff Accounts, Fee Rates, Tax Declarations & Owners
-- =============================================================================

SET NOCOUNT ON;

-- -----------------------------------------------------------------------------
-- 1. SEED STAFF USER ACCOUNTS
-- Default Password for staff accounts: 'P@ssword2026!'
-- -----------------------------------------------------------------------------
DECLARE @StaffHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==';

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'admin.gensan')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000001', 'admin.gensan', @StaffHash, 'Engr. Roberto M. Dela Rosa', 'roberto.delarosa@gensan.gov.ph', 'EMP-2024-001', 0, 'City Assessor Executive Office', '0917-888-9001', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'assessor.head')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000002', 'assessor.head', @StaffHash, 'Atty. Clarissa V. Mendoza', 'clarissa.mendoza@gensan.gov.ph', 'EMP-2024-002', 1, 'City Assessor Executive Office', '0919-555-1122', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'officer.santos')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000003', 'officer.santos', @StaffHash, 'Marcus Aurelius Santos', 'marcus.santos@gensan.gov.ph', 'EMP-2025-014', 2, 'Real Property Assessment Division', '0920-777-3344', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'appraiser.reyes')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000004', 'appraiser.reyes', @StaffHash, 'Elena G. Reyes', 'elena.reyes@gensan.gov.ph', 'EMP-2025-028', 3, 'Property Valuation & Appraisal Div', '0918-444-5566', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'taxmap.valdez')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000005', 'taxmap.valdez', @StaffHash, 'Fernando K. Valdez', 'fernando.valdez@gensan.gov.ph', 'EMP-2025-035', 4, 'GIS & Tax Mapping Section', '0922-333-8899', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'clerk.villanueva')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000006', 'clerk.villanueva', @StaffHash, 'Patricia Ann Villanueva', 'patricia.villanueva@gensan.gov.ph', 'EMP-2026-050', 5, 'Taxpayer Services & Roll Encoding', '0917-222-6677', 1, GETDATE());

-- -----------------------------------------------------------------------------
-- 2. SEED ORDER OF PAYMENT FEE RATES
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-001')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000001', 'FEE-001', 'Certified True Copy of Tax Declaration (TDN)', 'Certifications', 150.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-002')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000002', 'FEE-002', 'Certificate of Aggregate Landholding', 'Certifications', 200.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-003')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000003', 'FEE-003', 'Certificate of No Real Property Improvements', 'Certifications', 150.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-004')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000004', 'FEE-004', 'Certificate of Property Identification & Verification', 'Tax Mapping', 300.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-005')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000005', 'FEE-005', 'Tax Map Copy / Parcel Layout Plan (A3 Print)', 'Tax Mapping', 500.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-006')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000006', 'FEE-006', 'Field Inspection Fee (Per Lot Parcel)', 'Inspection', 750.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-007')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000007', 'FEE-007', 'Certificate of Road Right of Way (RROW)', 'Certifications', 250.00, 1, GETDATE(), GETDATE());

-- -----------------------------------------------------------------------------
-- 3. SEED TAXPAYER OWNERS
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TaxpayerOwners] WHERE [TaxIdentNo] = '123-456-789-000')
    INSERT INTO [dbo].[TaxpayerOwners] ([Id], [OwnerType], [FirstName], [MiddleName], [LastName], [CorporateName], [TaxIdentNo], [RptinNumber], [ContactNo], [Email], [ProvinceName], [CityMunicipalityName], [BarangayName], [Address], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('60000000-0000-0000-0000-000000000001', 0, 'Juan', 'Mercado', 'Dela Cruz', 'Dela Cruz Holdings', '123-456-789-000', 'RPTIN-2026-001', '0917-111-2222', 'juan.delacruz@gmail.com', 'South Cotabato', 'General Santos City', 'Brgy. Dadiangas East', 'Pioneer Avenue, General Santos City', 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaxpayerOwners] WHERE [TaxIdentNo] = '987-654-321-000')
    INSERT INTO [dbo].[TaxpayerOwners] ([Id], [OwnerType], [FirstName], [MiddleName], [LastName], [CorporateName], [TaxIdentNo], [RptinNumber], [ContactNo], [Email], [ProvinceName], [CityMunicipalityName], [BarangayName], [Address], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('60000000-0000-0000-0000-000000000002', 0, 'Maria Clara', 'Ocampo', 'Santos', 'Santos Realty & Dev Corp', '987-654-321-000', 'RPTIN-2026-002', '0919-222-3333', 'maria.santos@gmail.com', 'South Cotabato', 'General Santos City', 'Brgy. Dadiangas South', 'Santiago Boulevard, General Santos City', 1, GETDATE(), GETDATE());

-- -----------------------------------------------------------------------------
-- 4. SEED REGISTERED PROPERTY TAX DECLARATIONS (NORMALIZED SCHEMA)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TaxDeclarations] WHERE [TaxDeclarationNumber] = 'TD-2026-GS-0001')
BEGIN
    INSERT INTO [dbo].[TaxDeclarations] (
        [Id], [OwnerId], [Kind], [TaxDeclarationNumber], [TransactionCode], [PropertyIndexNumber],
        [OwnerName], [OwnerTin], [OwnerPhone], [OwnerEmail], [OwnerAddress], [OwnerSex],
        [AppraisedByName], [DateAppraised], [AssessedByName], [DateAssessed],
        [RecommendingApprovalName], [DateRecommended], [Memoranda], [ApprovedByName], [DateApproved],
        [EntryInRollName], [EntryInRollPosition], [EntryInRollDate], [EncodedByName], [DateEncoded], [Status]
    )
    VALUES (
        '90000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000001', 0, N'TD-2026-GS-0001', N'TR-2026-NEW', N'126-00-0001-002-01',
        N'Engr. Juan Dela Cruz', N'123-456-789-000', N'0917-111-2222', N'juan.delacruz@gmail.com', N'Pioneer Avenue, General Santos City', N'Male',
        N'Elena G. Reyes (Property Appraiser)', GETDATE(), N'Marcus Aurelius Santos (Assessment Officer)', GETDATE(),
        N'Engr. Roberto M. Dela Rosa (Assistant Assessor)', GETDATE(), N'Standard residential land assessment',
        N'Atty. Clarissa V. Mendoza (City Assessor)', GETDATE(), N'Patricia Ann Villanueva', N'Assessment Roll Clerk', GETDATE(),
        N'Patricia Ann Villanueva', GETDATE(), 0
    );

    IF NOT EXISTS (SELECT 1 FROM [dbo].[TDNLands] WHERE [TaxDeclarationId] = '90000000-0000-0000-0000-000000000001')
    BEGIN
        INSERT INTO [dbo].[TDNLands] (
            [Id], [TaxDeclarationId], [RegistrationStatus], [TitleType], [TitleNo], [SurveyNo], [LotNo], [BlockNo],
            [StreetNo], [Municipality], [DistrictOrBarangay], [ProvinceCity],
            [BoundaryNorth], [BoundaryEast], [BoundaryWest], [BoundarySouth],
            [Classification], [SubClassification], [ActualUse], [AreaSqMeters], [UnitValue], [MarketValue],
            [AdjustedMarketValue], [AssessmentLevelPercent], [AssessedValue], [BasicTaxRate], [SefTaxRate], [AnnualTaxDue],
            [EffectivityYear], [EffectivityQuarter], [Taxability], [TaxabilityBasis],
            [ImprovementQty], [ImprovementUnitValue], [ImprovementMarketValue], [PercentAdjustment], [ValueAdjustment],
            [PreviousMarketValue], [PreviousAssessedValue]
        )
        VALUES (
            '91000000-0000-0000-0000-000000000001', '90000000-0000-0000-0000-000000000001', N'Titled', N'TCT', N'TCT-142-99881', N'Psu-12-00411', N'Lot 4', N'Block 12',
            N'Pioneer Avenue', N'General Santos City', N'Brgy. Dadiangas East', N'South Cotabato',
            N'Lot 101', N'Road Lot', N'Creek', N'Lot 103',
            1, N'First Class Residential', N'Residential Lot', 450.00, 5000.00, 2250000.00,
            2250000.00, 20.00, 450000.00, 0.02, 0.01, 13500.00,
            2026, N'1st Quarter', N'Taxable', N'RA 7160 Local Government Code',
            0, 0, 0, 0, 0,
            0, 0
        );
    END
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaxDeclarations] WHERE [TaxDeclarationNumber] = 'TD-2026-GS-0002')
BEGIN
    INSERT INTO [dbo].[TaxDeclarations] (
        [Id], [OwnerId], [Kind], [TaxDeclarationNumber], [TransactionCode], [PropertyIndexNumber],
        [OwnerName], [OwnerTin], [OwnerPhone], [OwnerEmail], [OwnerAddress], [OwnerSex],
        [AppraisedByName], [DateAppraised], [AssessedByName], [DateAssessed],
        [RecommendingApprovalName], [DateRecommended], [Memoranda], [ApprovedByName], [DateApproved],
        [EntryInRollName], [EntryInRollPosition], [EntryInRollDate], [EncodedByName], [DateEncoded], [Status]
    )
    VALUES (
        '90000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000002', 1, N'TD-2026-GS-0002', N'TR-2026-NEW', N'126-00-0002-005-08',
        N'Maria Clara O. Santos', N'987-654-321-000', N'0919-222-3333', N'maria.santos@gmail.com', N'Santiago Boulevard, General Santos City', N'Female',
        N'Elena G. Reyes (Property Appraiser)', GETDATE(), N'Marcus Aurelius Santos (Assessment Officer)', GETDATE(),
        N'Engr. Roberto M. Dela Rosa (Assistant Assessor)', GETDATE(), N'Commercial building assessment entry',
        N'Atty. Clarissa V. Mendoza (City Assessor)', GETDATE(), N'Patricia Ann Villanueva', N'Assessment Roll Clerk', GETDATE(),
        N'Patricia Ann Villanueva', GETDATE(), 0
    );

    IF NOT EXISTS (SELECT 1 FROM [dbo].[TDNBuildings] WHERE [TaxDeclarationId] = '90000000-0000-0000-0000-000000000002')
    BEGIN
        INSERT INTO [dbo].[TDNBuildings] (
            [Id], [TaxDeclarationId], [StreetNo], [Municipality], [DistrictOrBarangay], [ProvinceCity],
            [LandOwnerName], [LandOwnerTin], [LandOwnerPhone], [LandOwnerEmail], [LandOwnerAddress], [LandOwnerSex],
            [LandTdNo], [LandPin], [LandAreaSqm],
            [IsCct], [IsAdministrator],
            [StructuralType], [BuildingClassification], [BuildingUseType], [NoOfStoreys], [FloorLevel], [BuildingPermitNo], [BuildingAgeYears],
            [TotalFloorArea], [BaseRcnValue], [ReplacementCostNew], [BuildingDepreciationPercent], [BuildingDepreciationAccumulated],
            [DepreciatedMarketValue], [TotalAdditionalItemsValue], [MarketValue], [AssessmentLevelPercent], [AssessedValue],
            [BasicTaxRate], [SefTaxRate], [AnnualTaxDue], [EffectivityYear], [EffectivityQuarter], [Taxability]
        )
        VALUES (
            '92000000-0000-0000-0000-000000000001', '90000000-0000-0000-0000-000000000002', N'Santiago Boulevard', N'General Santos City', N'Brgy. Dadiangas South', N'South Cotabato',
            N'Engr. Juan Dela Cruz', N'123-456-789-000', N'0917-111-2222', N'juan.delacruz@gmail.com', N'Pioneer Avenue, General Santos City', N'Male',
            N'TD-2026-GS-0001', N'126-00-0001-002-01', 450.00,
            0, 0,
            N'Type I', N'Class A - Reinforced Concrete', N'Commercial Building', 2, N'Ground & 2nd Floor', N'BP-2025-091', 2,
            620.00, 13709.68, 8500000.00, 0.00, 0.00,
            8500000.00, 0.00, 8500000.00, 50.00, 4250000.00,
            0.02, 0.01, 127500.00, 2026, N'1st Quarter', N'Taxable'
        );
    END
END

-- -----------------------------------------------------------------------------
-- VERIFICATION SUMMARY
-- -----------------------------------------------------------------------------
SELECT 'UserAccounts' AS [Table], COUNT(*) AS [Count] FROM [dbo].[UserAccounts]
UNION ALL
SELECT 'OrderPayments' AS [Table], COUNT(*) AS [Count] FROM [dbo].[OrderPayments]
UNION ALL
SELECT 'TaxpayerOwners' AS [Table], COUNT(*) AS [Count] FROM [dbo].[TaxpayerOwners]
UNION ALL
SELECT 'TaxDeclarations' AS [Table], COUNT(*) AS [Count] FROM [dbo].[TaxDeclarations];


GO
