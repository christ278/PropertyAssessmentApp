-- =========================================================================
-- SCRIPT: 09_SeedBuildingClassifications.sql
-- DESCRIPTION: Sets up and seeds the default reference Building Classifications
--              table ([dbo].[BuildingClassifications]) based on the CAST 
--              Property Appraisal System standards.
--              Includes Structural BUCC classes, Residential, Commercial,
--              Industrial, Institutional, Special, and Agricultural types.
-- DATABASE: FairTrackDB
-- DATE: 2026-10-26
-- =========================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Building Classifications Setup & Default Seeding';
PRINT '=========================================================================';

BEGIN TRANSACTION;

BEGIN TRY
    -- ---------------------------------------------------------------------
    -- 1. Ensure Table Exists
    -- ---------------------------------------------------------------------
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BuildingClassifications' AND schema_id = SCHEMA_ID('dbo'))
    BEGIN
        PRINT 'Creating table [dbo].[BuildingClassifications]...';
        CREATE TABLE [dbo].[BuildingClassifications] (
            [Id] INT IDENTITY(1,1) NOT NULL,
            [ClassificationCode] NVARCHAR(50) NOT NULL,
            [ClassificationName] NVARCHAR(250) NOT NULL,
            [Description] NVARCHAR(MAX) NULL,
            [StructuralTypeDefault] NVARCHAR(MAX) NULL,
            [DefaultUnitConstructionCost] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
            [IsActive] BIT NOT NULL DEFAULT 1,
            [SortOrder] INT NOT NULL DEFAULT 0,
            CONSTRAINT [PK_BuildingClassifications] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Table [dbo].[BuildingClassifications] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [dbo].[BuildingClassifications] already exists.';
    END

    -- ---------------------------------------------------------------------
    -- 2. Define Reference Building Classifications (CAST Standard)
    -- ---------------------------------------------------------------------
    DECLARE @ClassTable TABLE (
        [ClassificationCode] NVARCHAR(50),
        [ClassificationName] NVARCHAR(250),
        [Description] NVARCHAR(MAX),
        [StructuralTypeDefault] NVARCHAR(MAX),
        [DefaultUnitConstructionCost] DECIMAL(18,2),
        [IsActive] BIT,
        [SortOrder] INT
    );

    INSERT INTO @ClassTable ([ClassificationCode], [ClassificationName], [Description], [StructuralTypeDefault], [DefaultUnitConstructionCost], [IsActive], [SortOrder])
    VALUES
        (N'I-A',    N'Class A - Reinforced Concrete',                      N'Reinforced Concrete Frame (RC Columns, Beams, Slabs & Walls)',        N'Type I',   15000.00, 1, 1),
        (N'I-B',    N'Class B - Semi-Concrete / Hollow Blocks',            N'Reinforced Concrete Columns & Beams with CHB Exterior Walls',         N'Type II',  12500.00, 1, 2),
        (N'II-A',   N'Class C - Reinforced Concrete & Wood',               N'Concrete Ground Floor with Timber / Wood Upper Floors',                N'Type III',  9800.00, 1, 3),
        (N'III-A',  N'Class D - Wood / Timber Frame',                      N'Wood Columns, Girders, Timber Framing & Wood Flooring',                N'Type IV',   6500.00, 1, 4),
        (N'IV-A',   N'Class E - Light Materials / Mixed',                  N'Light G.I. Sheets, Bamboo, Sawali, Salvaged Materials',                N'Type V',    3500.00, 1, 5),
        (N'RES-01', N'Residential - One Family Dwelling',                  N'Single Family Detached Residential Building',                          N'Type I',   14000.00, 1, 6),
        (N'RES-02', N'Residential - Two Family Dwelling',                  N'Duplex Residential Unit',                                              N'Type I',   13500.00, 1, 7),
        (N'RES-03', N'Residential - Multi-Family / Apartment',              N'Apartment Building, Rowhouse, Accessoria',                             N'Type I',   14500.00, 1, 8),
        (N'RES-04', N'Residential - Condominium Unit',                     N'High-Rise / Mid-Rise Residential Condominium',                         N'Type I',   22000.00, 1, 9),
        (N'COM-01', N'Commercial - Retail / Shopping Center',              N'Retail Stores, Shopping Mall, Supermarket',                            N'Type I',   18000.00, 1, 10),
        (N'COM-02', N'Commercial - Office Building / Bank',                N'Office Tower, Financial Center, Bank Building',                        N'Type I',   20000.00, 1, 11),
        (N'COM-03', N'Commercial - Hotel / Motel / Inn',                   N'Hospitality Building, Hotel, Resort Accommodation',                    N'Type I',   24000.00, 1, 12),
        (N'IND-01', N'Industrial - Factory / Manufacturing Plant',         N'Industrial Production and Manufacturing Complex',                       N'Type III', 16000.00, 1, 13),
        (N'IND-02', N'Industrial - Warehouse / Storage Facility',          N'Storage Bodega, Logistics Center, Cold Storage',                       N'Type III', 11000.00, 1, 14),
        (N'INS-01', N'Institutional - School / University',                N'Educational Classrooms, Campus Laboratories',                          N'Type I',   15500.00, 1, 15),
        (N'INS-02', N'Institutional - Hospital / Medical Clinic',          N'Hospital, Clinical Laboratory, Health Facility',                       N'Type I',   25000.00, 1, 16),
        (N'SPE-01', N'Special - Gymnasium / Coliseum / Arena',             N'Sports Coliseum, Cultural Theater, Auditorium',                        N'Type I',   21000.00, 1, 17),
        (N'AGR-01', N'Agricultural - Farm Shed / Livestock Facility',      N'Poultry House, Piggery Pen, Farm Machinery Shed',                      N'Type IV',   4500.00, 1, 18);

    -- ---------------------------------------------------------------------
    -- 3. Idempotent Upsert / Merge into [dbo].[BuildingClassifications]
    -- ---------------------------------------------------------------------
    DECLARE @InsertedCount INT = 0;
    DECLARE @UpdatedCount INT = 0;

    MERGE INTO [dbo].[BuildingClassifications] AS target
    USING @ClassTable AS source
    ON (target.[ClassificationCode] = source.[ClassificationCode])
    WHEN MATCHED THEN
        UPDATE SET
            target.[ClassificationName]          = source.[ClassificationName],
            target.[Description]                 = source.[Description],
            target.[StructuralTypeDefault]       = source.[StructuralTypeDefault],
            target.[DefaultUnitConstructionCost] = source.[DefaultUnitConstructionCost],
            target.[IsActive]                    = source.[IsActive],
            target.[SortOrder]                   = source.[SortOrder]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ClassificationCode], [ClassificationName], [Description], [StructuralTypeDefault], [DefaultUnitConstructionCost], [IsActive], [SortOrder])
        VALUES (source.[ClassificationCode], source.[ClassificationName], source.[Description], source.[StructuralTypeDefault], source.[DefaultUnitConstructionCost], source.[IsActive], source.[SortOrder]);

    PRINT 'Building Classifications upsert completed successfully.';

    COMMIT TRANSACTION;

    PRINT '=========================================================================';
    PRINT 'Summary of [dbo].[BuildingClassifications]:';
    SELECT [Id], [ClassificationCode], [ClassificationName], [StructuralTypeDefault], [DefaultUnitConstructionCost], [SortOrder]
    FROM [dbo].[BuildingClassifications]
    ORDER BY [SortOrder] ASC;
    PRINT '=========================================================================';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT 'ERROR encountered: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO
