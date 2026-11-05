-- ==========================================================================================
-- SCRIPT: 00_RunAllScripts_Master.sql
-- DESCRIPTION: Master SQL script that executes all schema, lookup, and seed scripts
--              in proper sequential order using SQL Server SQLCMD mode.
--
-- INSTRUCTIONS FOR SSMS (SQL Server Management Studio):
-- 1. Open this file in SSMS.
-- 2. In the top menu bar, click: [Query] -> [SQLCMD Mode] (Make sure it is enabled/highlighted).
-- 3. Verify or adjust the :setvar ScriptPath below to your folder location if needed.
-- 4. Select your target database and click [Execute] (or press F5).
--
-- INSTRUCTIONS FOR COMMAND LINE (sqlcmd):
--    sqlcmd -S <server> -d <database> -U <user> -P <password> -i "00_RunAllScripts_Master.sql"
-- ==========================================================================================

-- Set folder path containing the SQL scripts:
:setvar ScriptPath "C:\Users\HP_02\Desktop\PropertyAssessmentApp\SqlScripts"

PRINT '==========================================================================================';
PRINT 'STARTING CITY ASSESSOR FULL DATABASE INITIALIZATION & SEED SEQUENCE';
PRINT '==========================================================================================';
GO

-- ------------------------------------------------------------------------------------------
-- PHASE 1: CORE SCHEMA & TABLES DDL
-- ------------------------------------------------------------------------------------------
PRINT '>>> [1/21] Executing 01_CreateTables.sql...';
:r $(ScriptPath)\01_CreateTables.sql
GO

PRINT '>>> [2/21] Executing 04_SeedFileLocationDefaultSetup.sql...';
:r $(ScriptPath)\04_SeedFileLocationDefaultSetup.sql
GO

PRINT '>>> [3/21] Executing 05_CreateInspectionAndAssessmentRequestTables.sql...';
:r $(ScriptPath)\05_CreateInspectionAndAssessmentRequestTables.sql
GO

PRINT '>>> [4/21] Executing 17_AddTransactionTypeToRequestForInspection.sql...';
:r $(ScriptPath)\17_AddTransactionTypeToRequestForInspection.sql
GO

PRINT '>>> [5/21] Executing 18_AddCancellationAndSupersedingColumnsToTaxDeclaration.sql...';
:r $(ScriptPath)\18_AddCancellationAndSupersedingColumnsToTaxDeclaration.sql
GO

-- ------------------------------------------------------------------------------------------
-- PHASE 2: SYSTEM ROLES, MODULES & ADMIN ACCOUNT
-- ------------------------------------------------------------------------------------------
PRINT '>>> [6/21] Executing 02_SeedDefaultAdmin.sql...';
:r $(ScriptPath)\02_SeedDefaultAdmin.sql
GO

PRINT '>>> [7/21] Executing 12_SeedDefaultModules.sql...';
:r $(ScriptPath)\12_SeedDefaultModules.sql
GO

PRINT '>>> [8/21] Executing 13_SeedDefaultUserRoles.sql...';
:r $(ScriptPath)\13_SeedDefaultUserRoles.sql
GO

PRINT '>>> [9/21] Executing 06_SyncUserRolesWithWorkflowSteps.sql...';
:r $(ScriptPath)\06_SyncUserRolesWithWorkflowSteps.sql
GO

PRINT '>>> [10/21] Executing 08_SeedDefaultRolePermissions.sql...';
:r $(ScriptPath)\08_SeedDefaultRolePermissions.sql
GO

PRINT '>>> [11/21] Executing 19_SeedTransactionStatusesAndWorkflowSequences.sql...';
:r $(ScriptPath)\19_SeedTransactionStatusesAndWorkflowSequences.sql
GO

PRINT '>>> [12/21] Executing 07_ImportUsersFromCast.sql...';
:r $(ScriptPath)\07_ImportUsersFromCast.sql
GO

-- ------------------------------------------------------------------------------------------
-- PHASE 3: PREREQUISITES & REFERENCE APPRAISAL TABLES
-- ------------------------------------------------------------------------------------------
PRINT '>>> [13/21] Executing 09_SeedBuildingClassifications.sql...';
:r $(ScriptPath)\09_SeedBuildingClassifications.sql
GO

PRINT '>>> [14/21] Executing 10_SeedBuildingAdditionalItems.sql...';
:r $(ScriptPath)\10_SeedBuildingAdditionalItems.sql
GO

PRINT '>>> [15/21] Executing 11_SeedBuildingAssessmentDepreciationAndRates.sql...';
:r $(ScriptPath)\11_SeedBuildingAssessmentDepreciationAndRates.sql
GO

PRINT '>>> [16/21] Executing 14_SeedLandPrerequisitesAndTables.sql...';
:r $(ScriptPath)\14_SeedLandPrerequisitesAndTables.sql
GO

PRINT '>>> [17/21] Executing 15_SeedDefaultProvincesCitiesBarangays.sql...';
:r $(ScriptPath)\15_SeedDefaultProvincesCitiesBarangays.sql
GO

PRINT '>>> [18/21] Executing 16_SeedMachineryPrerequisitesAndTables.sql...';
:r $(ScriptPath)\16_SeedMachineryPrerequisitesAndTables.sql
GO

PRINT '>>> [19/21] Executing 20_CreateAndSeedBuildingDepreciationSchedule.sql...';
:r $(ScriptPath)\20_CreateAndSeedBuildingDepreciationSchedule.sql
GO

PRINT '>>> [20/21] Executing 21_UpdateBuildingStructuralMaterials.sql...';
:r $(ScriptPath)\21_UpdateBuildingStructuralMaterials.sql
GO

-- ------------------------------------------------------------------------------------------
-- PHASE 4: SAMPLE DATA (MUST BE RUN LAST)
-- ------------------------------------------------------------------------------------------
PRINT '>>> [21/21] Executing 22_SeedAllSampleData.sql...';
:r $(ScriptPath)\22_SeedAllSampleData.sql
GO

PRINT '==========================================================================================';
PRINT 'SUCCESS: ALL 21 DATABASE SCRIPTS EXECUTED IN PROPER SEQUENCE!';
PRINT '==========================================================================================';
GO
