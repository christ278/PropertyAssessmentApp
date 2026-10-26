-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 15_SeedDefaultProvincesCitiesBarangays.sql
-- DESCRIPTION: Seeds default geographic lookup master data:
--              1. [dbo].[Provinces] (South Cotabato, Cavite, NCR, Cebu, etc.)
--              2. [dbo].[CityMunicipalities] (General Santos City, Koronadal, etc.)
--              3. [dbo].[Barangays] (Complete 26 official Barangays of GenSan + sample brgys)
--              Uses idempotent MERGE operations with deterministic GUIDs matching
--              GeographicService for seamless local / database operation.
-- DATABASE: FairTrackDB
-- =============================================================================

USE [FairTrackDB];
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Default Geographic Data Seeding (Provinces, Cities, Barangays)...';
PRINT '=========================================================================';

-- -----------------------------------------------------------------------------
-- 1. Ensure Tables Exist
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Provinces')
BEGIN
    CREATE TABLE [dbo].[Provinces] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Region] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created table [dbo].[Provinces].';
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CityMunicipalities')
BEGIN
    CREATE TABLE [dbo].[CityMunicipalities] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [ProvinceId] UNIQUEIDENTIFIER NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [ZipCode] NVARCHAR(20) NOT NULL,
        [IsCity] BIT NOT NULL DEFAULT 1,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_CityMunicipalities_Provinces] FOREIGN KEY ([ProvinceId]) REFERENCES [dbo].[Provinces]([Id])
    );
    PRINT 'Created table [dbo].[CityMunicipalities].';
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Barangays')
BEGIN
    CREATE TABLE [dbo].[Barangays] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [CityMunicipalityId] UNIQUEIDENTIFIER NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [District] NVARCHAR(50) NOT NULL DEFAULT 'District 1',
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_Barangays_CityMunicipalities] FOREIGN KEY ([CityMunicipalityId]) REFERENCES [dbo].[CityMunicipalities]([Id])
    );
    PRINT 'Created table [dbo].[Barangays].';
END
GO

-- -----------------------------------------------------------------------------
-- 2. Seed / Upsert Provinces
-- -----------------------------------------------------------------------------
BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @SourceProvinces TABLE (
        [Id] UNIQUEIDENTIFIER,
        [Code] NVARCHAR(50),
        [Name] NVARCHAR(100),
        [Region] NVARCHAR(100),
        [IsActive] BIT
    );

    INSERT INTO @SourceProvinces ([Id], [Code], [Name], [Region], [IsActive])
    VALUES
        ('10000000-0000-0000-0000-000000000001', 'PROV-1263', 'South Cotabato',       'Region XII (SOCCSKSARGEN)', 1),
        ('10000000-0000-0000-0000-000000000002', 'PROV-0421', 'Cavite',               'Region IV-A (CALABARZON)',   1),
        ('10000000-0000-0000-0000-000000000003', 'PROV-1300', 'Metro Manila (NCR)',   'National Capital Region',   1),
        ('10000000-0000-0000-0000-000000000004', 'PROV-0722', 'Cebu',                 'Region VII (Central Visayas)',1),
        ('10000000-0000-0000-0000-000000000005', 'PROV-1124', 'Davao del Sur',        'Region XI (Davao Region)',   1),
        ('10000000-0000-0000-0000-000000000006', 'PROV-1280', 'Sarangani',            'Region XII (SOCCSKSARGEN)', 1);

    MERGE [dbo].[Provinces] AS Target
    USING @SourceProvinces AS Source
    ON Target.[Id] = Source.[Id] OR Target.[Code] = Source.[Code]
    WHEN MATCHED THEN
        UPDATE SET
            Target.[Name]     = Source.[Name],
            Target.[Region]   = Source.[Region],
            Target.[IsActive] = Source.[IsActive]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [Code], [Name], [Region], [IsActive])
        VALUES (Source.[Id], Source.[Code], Source.[Name], Source.[Region], Source.[IsActive]);

    PRINT 'Provinces seeded successfully.';

    -- -------------------------------------------------------------------------
    -- 3. Seed / Upsert CityMunicipalities
    -- -------------------------------------------------------------------------
    DECLARE @SourceCities TABLE (
        [Id] UNIQUEIDENTIFIER,
        [ProvinceId] UNIQUEIDENTIFIER,
        [Code] NVARCHAR(50),
        [Name] NVARCHAR(100),
        [ZipCode] NVARCHAR(20),
        [IsCity] BIT,
        [IsActive] BIT
    );

    INSERT INTO @SourceCities ([Id], [ProvinceId], [Code], [Name], [ZipCode], [IsCity], [IsActive])
    VALUES
        ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'CITY-9500', 'General Santos City', '9500', 1, 1),
        ('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'CITY-9506', 'Koronadal City',     '9506', 1, 1),
        ('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'MUNI-9504', 'Polomolok',          '9504', 0, 1),
        ('20000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000002', 'CITY-4114', 'Dasmariñas City',     '4114', 1, 1),
        ('20000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000003', 'CITY-1100', 'Quezon City',         '1100', 1, 1),
        ('20000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000005', 'CITY-8000', 'Davao City',          '8000', 1, 1),
        ('20000000-0000-0000-0000-000000000007', '10000000-0000-0000-0000-000000000006', 'MUNI-9501', 'Alabel',              '9501', 0, 1);

    MERGE [dbo].[CityMunicipalities] AS Target
    USING @SourceCities AS Source
    ON Target.[Id] = Source.[Id] OR Target.[Code] = Source.[Code]
    WHEN MATCHED THEN
        UPDATE SET
            Target.[ProvinceId] = Source.[ProvinceId],
            Target.[Name]       = Source.[Name],
            Target.[ZipCode]    = Source.[ZipCode],
            Target.[IsCity]     = Source.[IsCity],
            Target.[IsActive]   = Source.[IsActive]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [ProvinceId], [Code], [Name], [ZipCode], [IsCity], [IsActive])
        VALUES (Source.[Id], Source.[ProvinceId], Source.[Code], Source.[Name], Source.[ZipCode], Source.[IsCity], Source.[IsActive]);

    PRINT 'CityMunicipalities seeded successfully.';

    -- -------------------------------------------------------------------------
    -- 4. Seed / Upsert Barangays (General Santos City 26 Official Barangays + Samples)
    -- -------------------------------------------------------------------------
    DECLARE @SourceBarangays TABLE (
        [Id] UNIQUEIDENTIFIER,
        [CityMunicipalityId] UNIQUEIDENTIFIER,
        [Code] NVARCHAR(50),
        [Name] NVARCHAR(100),
        [District] NVARCHAR(50),
        [IsActive] BIT
    );

    INSERT INTO @SourceBarangays ([Id], [CityMunicipalityId], [Code], [Name], [District], [IsActive])
    VALUES
        -- General Santos City (Complete 26 Barangays)
        ('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-01', 'Brgy. Dadiangas East (Poblacion)', 'District 1', 1),
        ('30000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-02', 'Brgy. Lagao',                   'District 2', 1),
        ('30000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-03', 'Brgy. Calumpang',               'District 1', 1),
        ('30000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-04', 'Brgy. San Isidro',               'District 3', 1),
        ('30000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-05', 'Brgy. City Heights',              'District 2', 1),
        ('30000000-0000-0000-0000-000000000010', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-10', 'Brgy. Apopong',                   'District 1', 1),
        ('30000000-0000-0000-0000-000000000011', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-11', 'Brgy. Baluan',                    'District 2', 1),
        ('30000000-0000-0000-0000-000000000012', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-12', 'Brgy. Batomelong',                'District 2', 1),
        ('30000000-0000-0000-0000-000000000013', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-13', 'Brgy. Buayan',                     'District 2', 1),
        ('30000000-0000-0000-0000-000000000014', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-14', 'Brgy. Bula',                       'District 1', 1),
        ('30000000-0000-0000-0000-000000000015', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-15', 'Brgy. Conel',                      'District 2', 1),
        ('30000000-0000-0000-0000-000000000016', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-16', 'Brgy. Dadiangas North',           'District 1', 1),
        ('30000000-0000-0000-0000-000000000017', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-17', 'Brgy. Dadiangas South',           'District 1', 1),
        ('30000000-0000-0000-0000-000000000018', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-18', 'Brgy. Dadiangas West',            'District 1', 1),
        ('30000000-0000-0000-0000-000000000019', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-19', 'Brgy. Fatima',                     'District 1', 1),
        ('30000000-0000-0000-0000-000000000020', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-20', 'Brgy. Katangawan',                 'District 2', 1),
        ('30000000-0000-0000-0000-000000000021', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-21', 'Brgy. Labangal',                   'District 1', 1),
        ('30000000-0000-0000-0000-000000000022', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-22', 'Brgy. Ligaya',                     'District 2', 1),
        ('30000000-0000-0000-0000-000000000023', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-23', 'Brgy. Mabuhay',                    'District 2', 1),
        ('30000000-0000-0000-0000-000000000024', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-24', 'Brgy. San Jose',                   'District 3', 1),
        ('30000000-0000-0000-0000-000000000025', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-25', 'Brgy. Sinawal',                    'District 1', 1),
        ('30000000-0000-0000-0000-000000000026', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-26', 'Brgy. Tambler',                    'District 1', 1),
        ('30000000-0000-0000-0000-000000000027', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-27', 'Brgy. Tinagacan',                  'District 2', 1),
        ('30000000-0000-0000-0000-000000000028', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-28', 'Brgy. Upper Labay',                 'District 2', 1),
        ('30000000-0000-0000-0000-000000000029', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-29', 'Brgy. Olympog',                    'District 2', 1),
        ('30000000-0000-0000-0000-000000000030', '20000000-0000-0000-0000-000000000001', 'BRGY-9500-30', 'Brgy. Siguel',                     'District 1', 1),

        -- Koronadal City
        ('30000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000002', 'BRGY-9506-01', 'Brgy. Zone 1 (Poblacion)',         'District 1', 1),
        ('30000000-0000-0000-0000-000000000007', '20000000-0000-0000-0000-000000000002', 'BRGY-9506-02', 'Brgy. Morales',                    'District 2', 1),
        ('30000000-0000-0000-0000-000000000031', '20000000-0000-0000-0000-000000000002', 'BRGY-9506-03', 'Brgy. Assumption',                 'District 2', 1),
        ('30000000-0000-0000-0000-000000000032', '20000000-0000-0000-0000-000000000002', 'BRGY-9506-04', 'Brgy. Carpenter Hill',            'District 2', 1),

        -- Dasmariñas City
        ('30000000-0000-0000-0000-000000000008', '20000000-0000-0000-0000-000000000004', 'BRGY-4114-01', 'Brgy. Salitran I',                 'Zone 1',     1),
        ('30000000-0000-0000-0000-000000000009', '20000000-0000-0000-0000-000000000004', 'BRGY-4114-02', 'Brgy. Paliparan I',                'Zone 2',     1);

    MERGE [dbo].[Barangays] AS Target
    USING @SourceBarangays AS Source
    ON Target.[Id] = Source.[Id] OR Target.[Code] = Source.[Code]
    WHEN MATCHED THEN
        UPDATE SET
            Target.[CityMunicipalityId] = Source.[CityMunicipalityId],
            Target.[Name]               = Source.[Name],
            Target.[District]           = Source.[District],
            Target.[IsActive]           = Source.[IsActive]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [CityMunicipalityId], [Code], [Name], [District], [IsActive])
        VALUES (Source.[Id], Source.[CityMunicipalityId], Source.[Code], Source.[Name], Source.[District], Source.[IsActive]);

    PRINT 'Barangays seeded successfully.';

    COMMIT TRANSACTION;
    PRINT '=========================================================================';
    PRINT 'Geographic data seeding completed successfully.';
    PRINT '=========================================================================';
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

-- -----------------------------------------------------------------------------
-- 5. Verification Output
-- -----------------------------------------------------------------------------
PRINT 'Verification Counts:';
SELECT 
    (SELECT COUNT(*) FROM [dbo].[Provinces]) AS [TotalProvinces],
    (SELECT COUNT(*) FROM [dbo].[CityMunicipalities]) AS [TotalCitiesMunicipalities],
    (SELECT COUNT(*) FROM [dbo].[Barangays]) AS [TotalBarangays];
GO
