-- ==========================================================================================
-- SCRIPT: seed_machinery_prerequisites_and_tables.sql
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
