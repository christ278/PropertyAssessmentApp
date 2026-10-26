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
