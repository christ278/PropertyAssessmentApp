-- =========================================================================
-- SCRIPT: 08_SeedDefaultRolePermissions.sql
-- DESCRIPTION: Sets up default module permissions in [dbo].[RoleModulePermission]:
--              1. By default, EVERY active role has access to:
--                 - Pool Transactions (POOL_TRANSACTIONS)
--                 - My Transactions (MY_TRANSACTIONS)
--              2. Division 1 Receiving role gets:
--                 - RPA: Property Assessment Requests (PROPERTY_ASSESSMENT)
--                 - RFI: Field Inspection Requests (INSPECTION)
--              3. Division 2 Receiving role gets:
--                 - Tax Mapping (TAX_MAPPING)
--              4. Division 3 Receiving role gets:
--                 - Tax Declaration (TAX_DECLARATION)
--                 - Land Holding (LAND_HOLDINGS)
--              5. System Administrator gets all active system modules.
-- DATABASE: FairTrackDB
-- DATE: 2026-10-25
-- =========================================================================

USE [FairTrackDB];
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Default Role & Module Permissions Seeding';
PRINT '=========================================================================';

BEGIN TRANSACTION;

BEGIN TRY
    -- ---------------------------------------------------------------------
    -- 1. Resolve Module IDs dynamically from [dbo].[SystemModule]
    -- ---------------------------------------------------------------------
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

    IF @PoolModuleId IS NULL OR @MyTransModuleId IS NULL
    BEGIN
        RAISERROR('Required modules POOL_TRANSACTIONS or MY_TRANSACTIONS not found in SystemModule table.', 16, 1);
    END

    PRINT 'Resolved target System Modules successfully.';

    -- ---------------------------------------------------------------------
    -- 2. Resolve Receiving Role IDs dynamically from [dbo].[UserRoleSettings]
    -- ---------------------------------------------------------------------
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

    -- ---------------------------------------------------------------------
    -- 3. Reset non-admin permissions to clean baseline
    --    (Ensures roles only have the exact intended permissions)
    -- ---------------------------------------------------------------------
    PRINT 'Cleaning existing permissions for non-admin roles...';
    
    DELETE FROM [dbo].[RoleModulePermission]
    WHERE RoleId <> @AdminRoleId;

    -- ---------------------------------------------------------------------
    -- 4. Grant EVERY Role default access to POOL_TRANSACTIONS & MY_TRANSACTIONS
    -- ---------------------------------------------------------------------
    PRINT 'Granting Pool Transactions & My Transactions to ALL active roles...';

    -- 4a. POOL_TRANSACTIONS for all active roles
    INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
    SELECT Id, @PoolModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE()
    FROM [dbo].[UserRoleSettings]
    WHERE IsActive = 1 AND Id <> @AdminRoleId;

    -- 4b. MY_TRANSACTIONS for all active roles
    INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
    SELECT Id, @MyTransModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE()
    FROM [dbo].[UserRoleSettings]
    WHERE IsActive = 1 AND Id <> @AdminRoleId;

    PRINT 'Default Pool & My Transactions access granted to all roles.';

    -- ---------------------------------------------------------------------
    -- 5. Division 1 Receiving: Additional access to RPA (PROPERTY_ASSESSMENT) and RFI (INSPECTION)
    -- ---------------------------------------------------------------------
    IF @Div1ReceivingRoleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 1 Receiving additional permissions (RPA & RFI)...';

        -- Additional: RPA (PROPERTY_ASSESSMENT)
        IF @PropAssessmentModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div1ReceivingRoleId, @PropAssessmentModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        -- Additional: RFI (INSPECTION)
        IF @InspectionModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div1ReceivingRoleId, @InspectionModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        PRINT 'Division 1 Receiving permissions configured.';
    END

    -- ---------------------------------------------------------------------
    -- 6. Division 2 Receiving: Additional access to Tax Mapping (TAX_MAPPING)
    -- ---------------------------------------------------------------------
    IF @Div2ReceivingRoleId IS NOT NULL AND @TaxMappingModuleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 2 Receiving additional permissions (Tax Mapping)...';

        INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
        VALUES (@Div2ReceivingRoleId, @TaxMappingModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());

        PRINT 'Division 2 Receiving permissions configured.';
    END

    -- ---------------------------------------------------------------------
    -- 7. Division 3 Receiving: Additional access to Tax Dec (TAX_DECLARATION) and Land Holding (LAND_HOLDINGS)
    -- ---------------------------------------------------------------------
    IF @Div3ReceivingRoleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 3 Receiving additional permissions (Tax Dec & Land Holding)...';

        -- Additional: Tax Dec (TAX_DECLARATION)
        IF @TaxDecModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div3ReceivingRoleId, @TaxDecModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        -- Additional: Land Holding (LAND_HOLDINGS)
        IF @LandHoldingModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div3ReceivingRoleId, @LandHoldingModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        PRINT 'Division 3 Receiving permissions configured.';
    END

    -- ---------------------------------------------------------------------
    -- 8. Ensure System Administrator has access to all active system modules
    -- ---------------------------------------------------------------------
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
-- 8. VERIFICATION QUERY: Display configured permissions for Receiving and Admin
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
