-- =============================================================================
-- SQL SCRIPT STEP 05: CREATE REQUEST FOR INSPECTION & PROPERTY ASSESSMENT TABLES
-- Target Database: (localdb)\mssqllocaldb / PropertyAssessmentAppDb
-- Reference: CAST System (RequestForInspection & PropertyAssessment workflows)
-- =============================================================================

USE [PropertyAssessmentAppDb];
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
END
GO
