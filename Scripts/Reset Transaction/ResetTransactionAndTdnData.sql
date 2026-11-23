-- =============================================================================
-- Reset Script: Delete All Transaction Records & TDN Records
-- Database: PropertyAssessmentApp (SQL Server)
--
-- WARNING: This permanently deletes all transaction requests, workflow logs,
--          order payments, property taggings, and Tax Declaration (TDN) records.
--          System setup, user accounts, roles, permissions, geographic data,
--          and assessment schedule/rate tables are PRESERVED.
--
-- Review carefully before executing. Recommended: take a database backup first.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Property Tagging & Audit Logs
    DELETE FROM [PropertyTaggingLogs];
    DELETE FROM [PropertyTaggings];

    -- 2. Workflow, Notification, Transfer, Billing & Report Logs
    DELETE FROM [DocumentForwardingLogs];
    DELETE FROM [SmsNotificationLogs];
    DELETE FROM [DataTransferLogs];
    DELETE FROM [OrderPayments];
    DELETE FROM [RPTReports];

    -- 3. Transaction Requests
    DELETE FROM [PropertyAssessmentRequests];
    DELETE FROM [RequestForInspections];
    DELETE FROM [TaxDeclarationRequests];
    DELETE FROM [LandHoldingRequests];
    DELETE FROM [TaxMappingRequests];

    -- 4. TDN Normalized Child Tables (delete children before parents)
    DELETE FROM [TDNPropertyImages];

    -- Building children & parent
    DELETE FROM [TDNBuildingFloors];
    DELETE FROM [TDNBuildingAdditionalItems];
    DELETE FROM [TDNBuildingStructuralMaterials];
    DELETE FROM [TDNBuildingLandReferences];
    DELETE FROM [TDNBuildings];

    -- Land children & parent
    DELETE FROM [TDNLandDetails];
    DELETE FROM [TDNLandImprovements];
    DELETE FROM [TDNLandBaseMarkets];
    DELETE FROM [TDNLandPropertyAssessments];
    DELETE FROM [TDNLands];

    -- Machinery children & parent
    DELETE FROM [TDNMachineryDetails];
    DELETE FROM [TDNMachineryPropertyAssessments];
    DELETE FROM [TDNMachineries];

    -- 5. Master Tax Declarations Table
    DELETE FROM [TaxDeclarations];

    -- 6. OPTIONAL: Uncomment the line below ONLY if you also want to delete all Taxpayer Owners
    -- DELETE FROM [TaxpayerOwners];

    COMMIT TRANSACTION;
    PRINT 'Successfully deleted all transaction and TDN records.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO
