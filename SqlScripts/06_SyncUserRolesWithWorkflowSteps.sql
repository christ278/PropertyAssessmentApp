-- =========================================================================
-- SCRIPT: 06_SyncUserRolesWithWorkflowSteps.sql
-- DESCRIPTION: Syncs [dbo].[UserRoleSettings] with [dbo].[TransactionStatus]
--              so that User Roles represent the Workflow Steps / Allowed Target Steps
-- DATABASE: FairTrackDB
-- =========================================================================

USE [FairTrackDB];
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
