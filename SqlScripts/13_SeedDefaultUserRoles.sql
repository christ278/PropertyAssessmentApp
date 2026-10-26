-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 13_SeedDefaultUserRoles.sql
-- DESCRIPTION: Seeds default user roles into [dbo].[UserRoleSettings].
--              Ensures table schema compatibility, preserves deterministic GUIDs,
--              maps Division and Workflow TransactionStatus IDs, and executes
--              safe idempotent upserts (MERGE).
-- DATABASE: FairTrackDB
-- =============================================================================

USE [FairTrackDB];
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
