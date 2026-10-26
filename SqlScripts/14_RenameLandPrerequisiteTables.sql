-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 14_RenameLandPrerequisiteTables.sql
-- DESCRIPTION: Standardizes all TDN Land prerequisite lookup tables by adding
--              the 'Land' prefix (e.g., refSubClassification -> LandRefSubClassification).
--              Safely renames existing tables using sp_rename, or creates them
--              if not present, and ensures baseline reference data is seeded.
-- DATABASE: FairTrackDB
-- =============================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Standardizing Land Prerequisite Tables (Adding Land Prefix)...';
PRINT '=========================================================================';

-- -----------------------------------------------------------------------------
-- 1. Table: refClassification -> LandRefClassification
-- -----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refClassification')
   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefClassification')
BEGIN
    EXEC sp_rename 'dbo.refClassification', 'LandRefClassification';
    PRINT 'Renamed [dbo].[refClassification] -> [dbo].[LandRefClassification].';
END
ELSE IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefClassification')
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
    PRINT 'Created table [dbo].[LandRefClassification].';
END
GO

-- -----------------------------------------------------------------------------
-- 2. Table: refSubClassification -> LandRefSubClassification
-- -----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refSubClassification')
   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefSubClassification')
BEGIN
    EXEC sp_rename 'dbo.refSubClassification', 'LandRefSubClassification';
    PRINT 'Renamed [dbo].[refSubClassification] -> [dbo].[LandRefSubClassification].';
END
ELSE IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefSubClassification')
BEGIN
    CREATE TABLE [dbo].[LandRefSubClassification] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ClassificationId] INT NOT NULL,
        [SubClass] NVARCHAR(100) NOT NULL,
        [UnitValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
    PRINT 'Created table [dbo].[LandRefSubClassification].';
END
GO

-- -----------------------------------------------------------------------------
-- 3. Table: refActualUse -> LandRefActualUse
-- -----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refActualUse')
   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefActualUse')
BEGIN
    EXEC sp_rename 'dbo.refActualUse', 'LandRefActualUse';
    PRINT 'Renamed [dbo].[refActualUse] -> [dbo].[LandRefActualUse].';
END
ELSE IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefActualUse')
BEGIN
    CREATE TABLE [dbo].[LandRefActualUse] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActualUseCode] NVARCHAR(50) NOT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [IsTaxExempt] BIT NOT NULL DEFAULT (0),
        [Rank] INT NULL,
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
    PRINT 'Created table [dbo].[LandRefActualUse].';
END
GO

-- -----------------------------------------------------------------------------
-- 4. Table: refLandAssessmentLevel -> LandRefAssessmentLevel
-- -----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refLandAssessmentLevel')
   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefAssessmentLevel')
BEGIN
    EXEC sp_rename 'dbo.refLandAssessmentLevel', 'LandRefAssessmentLevel';
    PRINT 'Renamed [dbo].[refLandAssessmentLevel] -> [dbo].[LandRefAssessmentLevel].';
END
ELSE IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefAssessmentLevel')
BEGIN
    CREATE TABLE [dbo].[LandRefAssessmentLevel] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActualUseId] INT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [AssessmentLevel] DECIMAL(18,2) NOT NULL DEFAULT (20),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
    PRINT 'Created table [dbo].[LandRefAssessmentLevel].';
END
GO

-- -----------------------------------------------------------------------------
-- 5. Table: refAdjustment -> LandRefAdjustment
-- -----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refAdjustment')
   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefAdjustment')
BEGIN
    EXEC sp_rename 'dbo.refAdjustment', 'LandRefAdjustment';
    PRINT 'Renamed [dbo].[refAdjustment] -> [dbo].[LandRefAdjustment].';
END
ELSE IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefAdjustment')
BEGIN
    CREATE TABLE [dbo].[LandRefAdjustment] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Description] NVARCHAR(200) NOT NULL,
        [Distance] NVARCHAR(50) NULL,
        [AdjustmentPercent] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
    PRINT 'Created table [dbo].[LandRefAdjustment].';
END
GO

-- -----------------------------------------------------------------------------
-- 6. Table: refImprovementType -> LandRefImprovementType
-- -----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refImprovementType')
   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefImprovementType')
BEGIN
    EXEC sp_rename 'dbo.refImprovementType', 'LandRefImprovementType';
    PRINT 'Renamed [dbo].[refImprovementType] -> [dbo].[LandRefImprovementType].';
END
ELSE IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefImprovementType')
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
    PRINT 'Created table [dbo].[LandRefImprovementType].';
END
GO

-- -----------------------------------------------------------------------------
-- 7. Populate default data if empty
-- -----------------------------------------------------------------------------
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
    PRINT 'Seeded [dbo].[LandRefClassification].';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[LandRefSubClassification])
BEGIN
    SET IDENTITY_INSERT [dbo].[LandRefSubClassification] ON;
    INSERT INTO [dbo].[LandRefSubClassification] ([Id], [ClassificationId], [SubClass], [UnitValue], [RevisionYear], [IsActive]) VALUES
    -- Residential
    (1, 1, N'1st Class Residential', 520.00, 2011, 1),
    (2, 1, N'2nd Class Residential', 468.00, 2011, 1),
    (3, 1, N'3rd Class Residential', 410.80, 2011, 1),
    (4, 1, N'4th Class Residential', 351.00, 2011, 1),
    (5, 1, N'5th Class Residential', 286.00, 2011, 1),
    (6, 1, N'6th Class Residential', 234.00, 2011, 1),
    (7, 1, N'7th Class Residential', 163.80, 2011, 1),
    (8, 1, N'8th Class Residential', 117.00, 2011, 1),
    -- Commercial
    (9, 4, N'1st Class Commercial', 3120.00, 2011, 1),
    (10, 4, N'2nd Class Commercial', 2522.00, 2011, 1),
    (11, 4, N'3rd Class Commercial', 1924.00, 2011, 1),
    (12, 4, N'4th Class Commercial', 1378.00, 2011, 1),
    (13, 4, N'5th Class Commercial', 1222.00, 2011, 1),
    (14, 4, N'6th Class Commercial', 988.00, 2011, 1),
    -- Industrial
    (15, 6, N'1st Class Industrial', 910.00, 2011, 1),
    (16, 6, N'2nd Class Industrial', 806.00, 2011, 1),
    (17, 6, N'3rd Class Industrial', 676.00, 2011, 1),
    (18, 6, N'4th Class Industrial', 546.00, 2011, 1),
    (19, 6, N'5th Class Industrial', 416.00, 2011, 1),
    -- Agricultural
    (20, 48, N'Riceland Irrigated (1st Class)', 150000.00, 2011, 1),
    (21, 48, N'Riceland Unirrigated (2nd Class)', 130000.00, 2011, 1),
    (22, 48, N'Cornland (1st Class)', 110000.00, 2011, 1),
    (23, 48, N'Coconut Land', 100000.00, 2011, 1),
    (24, 48, N'Banana Plantation', 120000.00, 2011, 1),
    (25, 48, N'Fishpond / Aquaculture', 160000.00, 2011, 1);
    SET IDENTITY_INSERT [dbo].[LandRefSubClassification] OFF;
    PRINT 'Seeded [dbo].[LandRefSubClassification].';
END
GO

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
    PRINT 'Seeded [dbo].[LandRefActualUse].';
END
GO

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
    PRINT 'Seeded [dbo].[LandRefAssessmentLevel].';
END
GO

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
    PRINT 'Seeded [dbo].[LandRefAdjustment].';
END
GO

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
    PRINT 'Seeded [dbo].[LandRefImprovementType].';
END
GO

PRINT '=========================================================================';
PRINT 'Land Prerequisite Tables standardized successfully.';
PRINT '=========================================================================';
