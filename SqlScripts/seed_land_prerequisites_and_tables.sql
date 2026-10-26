-- ==========================================================================================
-- SCRIPT: seed_land_prerequisites_and_tables.sql
-- DESCRIPTION: Creates prerequisite lookup tables and operational Land Appraisal line tables
--              for Local Government Unit (LGU) Assessor's Real Property Tax Assessment (CAST Standard),
--              and populates default seed data (General Santos City / CAST Standard Schedule).
-- ==========================================================================================

USE [PropertyAssessmentDb]; -- Update to target database if different
GO

-- 1. Table: refClassification
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'refClassification')
BEGIN
    CREATE TABLE [dbo].[refClassification] (
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

-- 2. Table: refSubClassification
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'refSubClassification')
BEGIN
    CREATE TABLE [dbo].[refSubClassification] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ClassificationId] INT NOT NULL,
        [SubClass] NVARCHAR(100) NOT NULL,
        [UnitValue] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 3. Table: refActualUse
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'refActualUse')
BEGIN
    CREATE TABLE [dbo].[refActualUse] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActualUseCode] NVARCHAR(50) NOT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [IsTaxExempt] BIT NOT NULL DEFAULT (0),
        [Rank] INT NULL,
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 4. Table: refLandAssessmentLevel
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'refLandAssessmentLevel')
BEGIN
    CREATE TABLE [dbo].[refLandAssessmentLevel] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActualUseId] INT NULL,
        [ActualUseName] NVARCHAR(100) NOT NULL,
        [AssessmentLevel] DECIMAL(18,2) NOT NULL DEFAULT (20),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 5. Table: refAdjustment
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'refAdjustment')
BEGIN
    CREATE TABLE [dbo].[refAdjustment] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Description] NVARCHAR(200) NOT NULL,
        [Distance] NVARCHAR(50) NULL,
        [AdjustmentPercent] DECIMAL(18,2) NOT NULL DEFAULT (0),
        [RevisionYear] INT NOT NULL DEFAULT (2011),
        [IsActive] BIT NOT NULL DEFAULT (1)
    );
END
GO

-- 6. Table: refImprovementType
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'refImprovementType')
BEGIN
    CREATE TABLE [dbo].[refImprovementType] (
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

-- Seeding refClassification
IF NOT EXISTS (SELECT 1 FROM [dbo].[refClassification])
BEGIN
    SET IDENTITY_INSERT [dbo].[refClassification] ON;
    INSERT INTO [dbo].[refClassification] ([Id], [ClassificationCode], [ClassificationName], [Ranking], [Prefix], [IsActive], [SortOrder]) VALUES
    (1, N'RES', N'RESIDENTIAL', 1, N'RES', 1, 1),
    (4, N'COM', N'COMMERCIAL', 3, N'COM', 1, 2),
    (6, N'IND', N'INDUSTRIAL', 4, N'IND', 1, 3),
    (48, N'AGR', N'AGRICULTURAL', 2, N'AGR', 1, 4),
    (10, N'CUL', N'SPECIAL - CULTURAL', 7, N'CUL', 1, 5),
    (11, N'HOS', N'SPECIAL - HOSPITAL', 8, N'HOS', 1, 6),
    (12, N'SCI', N'SPECIAL - SCIENTIFIC', 9, N'SCI', 1, 7),
    (49, N'MIN', N'MINERAL', 5, N'MIN', 1, 8),
    (50, N'TIM', N'TIMBERLAND', 6, N'TIM', 1, 9);
    SET IDENTITY_INSERT [dbo].[refClassification] OFF;
END
GO

-- Seeding refSubClassification (Schedule of Unit Values)
IF NOT EXISTS (SELECT 1 FROM [dbo].[refSubClassification])
BEGIN
    SET IDENTITY_INSERT [dbo].[refSubClassification] ON;
    INSERT INTO [dbo].[refSubClassification] ([Id], [ClassificationId], [SubClass], [UnitValue], [RevisionYear], [IsActive]) VALUES
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
    SET IDENTITY_INSERT [dbo].[refSubClassification] OFF;
END
GO

-- Seeding refActualUse
IF NOT EXISTS (SELECT 1 FROM [dbo].[refActualUse])
BEGIN
    SET IDENTITY_INSERT [dbo].[refActualUse] ON;
    INSERT INTO [dbo].[refActualUse] ([Id], [ActualUseCode], [ActualUseName], [IsTaxExempt], [Rank], [IsActive]) VALUES
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
    SET IDENTITY_INSERT [dbo].[refActualUse] OFF;
END
GO

-- Seeding refLandAssessmentLevel
IF NOT EXISTS (SELECT 1 FROM [dbo].[refLandAssessmentLevel])
BEGIN
    SET IDENTITY_INSERT [dbo].[refLandAssessmentLevel] ON;
    INSERT INTO [dbo].[refLandAssessmentLevel] ([Id], [ActualUseId], [ActualUseName], [AssessmentLevel], [RevisionYear], [IsActive]) VALUES
    (1, 1, N'RESIDENTIAL', 20.00, 2011, 1),
    (2, 2, N'AGRICULTURAL', 40.00, 2011, 1),
    (3, 3, N'COMMERCIAL', 50.00, 2011, 1),
    (4, 4, N'INDUSTRIAL', 50.00, 2011, 1),
    (5, 5, N'MINERAL', 50.00, 2011, 1),
    (6, 6, N'SPECIAL - CULTURAL', 10.00, 2011, 1),
    (7, 7, N'SPECIAL - HOSPITAL', 10.00, 2011, 1),
    (8, 8, N'SPECIAL - SCIENTIFIC', 10.00, 2011, 1),
    (9, 9, N'GOVERNMENT', 0.00, 2011, 1);
    SET IDENTITY_INSERT [dbo].[refLandAssessmentLevel] OFF;
END
GO

-- Seeding refAdjustment
IF NOT EXISTS (SELECT 1 FROM [dbo].[refAdjustment])
BEGIN
    SET IDENTITY_INSERT [dbo].[refAdjustment] ON;
    INSERT INTO [dbo].[refAdjustment] ([Id], [Description], [Distance], [AdjustmentPercent], [RevisionYear], [IsActive]) VALUES
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
    SET IDENTITY_INSERT [dbo].[refAdjustment] OFF;
END
GO

-- Seeding refImprovementType
IF NOT EXISTS (SELECT 1 FROM [dbo].[refImprovementType])
BEGIN
    SET IDENTITY_INSERT [dbo].[refImprovementType] ON;
    INSERT INTO [dbo].[refImprovementType] ([Id], [TypeCode], [TypeDescription], [Kind], [BaseValue], [AssessmentLevel], [RevisionYear], [IsActive]) VALUES
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
    SET IDENTITY_INSERT [dbo].[refImprovementType] OFF;
END
GO

PRINT 'Land Prerequisites and Tables Seeded Successfully (CAST System Standard)!';
