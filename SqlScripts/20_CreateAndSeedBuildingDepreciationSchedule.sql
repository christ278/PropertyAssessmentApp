-- =============================================================================
-- 20_CreateAndSeedBuildingDepreciationSchedule.sql
-- Purpose:
--   1. Creates the [dbo].[BuildingDepreciationSchedules] SQL table for the
--      official Schedule of Building Physical Depreciation (% Depreciation)
--      across Classifications I, II-A, II-B, III-A, III-B, IV-A, IV-B, V-A, V-B
--      and General Revision (GR) Cycles.
--   2. Seeds all 180 schedule entries (20 age rows x 9 classifications) with
--      Estimated Economic Life (years), Estimated Residual Value (%), explicit
--      cell Depreciation (%), and Effective Depreciation (%).
--   3. Alters [dbo].[TDNBuildings] to add [BuildingDepreciationScheduleId] FK,
--      [GeneralRevisionCycle], [EstimatedEconomicLifeYears], and
--      [EstimatedResidualValuePercent], and links existing TDNBuilding records.
-- =============================================================================

SET NOCOUNT ON;
GO

-- -----------------------------------------------------------------------------
-- 1. CREATE [dbo].[BuildingDepreciationSchedules] TABLE
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[BuildingDepreciationSchedules]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BuildingDepreciationSchedules] (
        [Id]                           INT            IDENTITY(1,1) NOT NULL,
        [GeneralRevisionCycle]         NVARCHAR(50)   NOT NULL,
        [BuildingAgeLabel]             NVARCHAR(20)   NOT NULL,
        [AgeLowerBound]                INT            NOT NULL,
        [AgeUpperBound]                INT            NOT NULL,
        [ClassificationCode]           NVARCHAR(20)   NOT NULL,
        [StructuralType]               NVARCHAR(30)   NOT NULL,
        [EstimatedEconomicLifeYears]   INT            NOT NULL,
        [EstimatedResidualValuePercent] DECIMAL(18,2) NOT NULL,
        [DepreciationPercent]          DECIMAL(18,2)  NULL,      -- NULL represents '—' (Residual Value Cap reached)
        [EffectiveDepreciationPercent] DECIMAL(18,2)  NOT NULL,  -- Explicit rate or (100 - ResidualValuePercent)
        [IsResidualFloorCap]           BIT            NOT NULL CONSTRAINT [DF_BuildingDepreciationSchedules_IsCap] DEFAULT (0),
        [RevisionYear]                 INT            NOT NULL CONSTRAINT [DF_BuildingDepreciationSchedules_RevYear] DEFAULT (2024),
        [SortOrder]                    INT            NOT NULL CONSTRAINT [DF_BuildingDepreciationSchedules_SortOrder] DEFAULT (0),
        [IsActive]                     BIT            NOT NULL CONSTRAINT [DF_BuildingDepreciationSchedules_IsActive] DEFAULT (1),
        CONSTRAINT [PK_BuildingDepreciationSchedules] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_BuildingDepreciationSchedules_Class_Age]
        ON [dbo].[BuildingDepreciationSchedules] ([ClassificationCode] ASC, [AgeLowerBound] ASC, [AgeUpperBound] ASC);
END
GO

-- -----------------------------------------------------------------------------
-- 2. SEED ALL 180 SCHEDULE VALUES (20 AGE ROWS x 9 CLASSIFICATIONS I TO V-B)
-- -----------------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[BuildingDepreciationSchedules] ON;

MERGE INTO [dbo].[BuildingDepreciationSchedules] AS Target
USING (VALUES
    -- =========================================================================
    -- 1st 3 yrs of GR (Building Age 1, 2, 3)
    -- =========================================================================
    -- Age 1 (1..1)
    (  1, N'1st 3 yrs of GR', N'1', 1, 1, N'I',     N'Type I',   15, 10.00,  6.70,  6.70, 0, 2024,   1, 1),
    (  2, N'1st 3 yrs of GR', N'1', 1, 1, N'II-A',  N'Type II',  20, 15.00,  5.00,  5.00, 0, 2024,   2, 1),
    (  3, N'1st 3 yrs of GR', N'1', 1, 1, N'II-B',  N'Type II',  18, 12.00,  5.00,  5.00, 0, 2024,   3, 1),
    (  4, N'1st 3 yrs of GR', N'1', 1, 1, N'III-A', N'Type III', 30, 25.00,  3.30,  3.30, 0, 2024,   4, 1),
    (  5, N'1st 3 yrs of GR', N'1', 1, 1, N'III-B', N'Type III', 25, 20.00,  4.00,  4.00, 0, 2024,   5, 1),
    (  6, N'1st 3 yrs of GR', N'1', 1, 1, N'IV-A',  N'Type IV',  40, 35.00,  2.50,  2.50, 0, 2024,   6, 1),
    (  7, N'1st 3 yrs of GR', N'1', 1, 1, N'IV-B',  N'Type IV',  38, 33.00,  2.60,  2.60, 0, 2024,   7, 1),
    (  8, N'1st 3 yrs of GR', N'1', 1, 1, N'V-A',   N'Type V',   50, 40.00,  2.00,  2.00, 0, 2024,   8, 1),
    (  9, N'1st 3 yrs of GR', N'1', 1, 1, N'V-B',   N'Type V',   45, 37.00,  2.20,  2.20, 0, 2024,   9, 1),

    -- Age 2 (2..2)
    ( 10, N'1st 3 yrs of GR', N'2', 2, 2, N'I',     N'Type I',   15, 10.00, 13.30, 13.30, 0, 2024,  10, 1),
    ( 11, N'1st 3 yrs of GR', N'2', 2, 2, N'II-A',  N'Type II',  20, 15.00, 10.00, 10.00, 0, 2024,  11, 1),
    ( 12, N'1st 3 yrs of GR', N'2', 2, 2, N'II-B',  N'Type II',  18, 12.00, 11.10, 11.10, 0, 2024,  12, 1),
    ( 13, N'1st 3 yrs of GR', N'2', 2, 2, N'III-A', N'Type III', 30, 25.00,  6.70,  6.70, 0, 2024,  13, 1),
    ( 14, N'1st 3 yrs of GR', N'2', 2, 2, N'III-B', N'Type III', 25, 20.00,  8.00,  8.00, 0, 2024,  14, 1),
    ( 15, N'1st 3 yrs of GR', N'2', 2, 2, N'IV-A',  N'Type IV',  40, 35.00,  5.00,  5.00, 0, 2024,  15, 1),
    ( 16, N'1st 3 yrs of GR', N'2', 2, 2, N'IV-B',  N'Type IV',  38, 33.00,  5.30,  5.30, 0, 2024,  16, 1),
    ( 17, N'1st 3 yrs of GR', N'2', 2, 2, N'V-A',   N'Type V',   50, 40.00,  4.00,  4.00, 0, 2024,  17, 1),
    ( 18, N'1st 3 yrs of GR', N'2', 2, 2, N'V-B',   N'Type V',   45, 37.00,  4.40,  4.40, 0, 2024,  18, 1),

    -- Age 3 (3..3)
    ( 19, N'1st 3 yrs of GR', N'3', 3, 3, N'I',     N'Type I',   15, 10.00, 20.00, 20.00, 0, 2024,  19, 1),
    ( 20, N'1st 3 yrs of GR', N'3', 3, 3, N'II-A',  N'Type II',  20, 15.00, 15.00, 15.00, 0, 2024,  20, 1),
    ( 21, N'1st 3 yrs of GR', N'3', 3, 3, N'II-B',  N'Type II',  18, 12.00, 16.70, 16.70, 0, 2024,  21, 1),
    ( 22, N'1st 3 yrs of GR', N'3', 3, 3, N'III-A', N'Type III', 30, 25.00, 10.00, 10.00, 0, 2024,  22, 1),
    ( 23, N'1st 3 yrs of GR', N'3', 3, 3, N'III-B', N'Type III', 25, 20.00, 12.00, 12.00, 0, 2024,  23, 1),
    ( 24, N'1st 3 yrs of GR', N'3', 3, 3, N'IV-A',  N'Type IV',  40, 35.00,  7.50,  7.50, 0, 2024,  24, 1),
    ( 25, N'1st 3 yrs of GR', N'3', 3, 3, N'IV-B',  N'Type IV',  38, 33.00,  7.90,  7.90, 0, 2024,  25, 1),
    ( 26, N'1st 3 yrs of GR', N'3', 3, 3, N'V-A',   N'Type V',   50, 40.00,  6.00,  6.00, 0, 2024,  26, 1),
    ( 27, N'1st 3 yrs of GR', N'3', 3, 3, N'V-B',   N'Type V',   45, 37.00,  6.70,  6.70, 0, 2024,  27, 1),

    -- =========================================================================
    -- 2nd 3 yrs of GR (Building Age 4, 5, 6)
    -- =========================================================================
    -- Age 4 (4..4)
    ( 28, N'2nd 3 yrs of GR', N'4', 4, 4, N'I',     N'Type I',   15, 10.00, 26.70, 26.70, 0, 2024,  28, 1),
    ( 29, N'2nd 3 yrs of GR', N'4', 4, 4, N'II-A',  N'Type II',  20, 15.00, 20.00, 20.00, 0, 2024,  29, 1),
    ( 30, N'2nd 3 yrs of GR', N'4', 4, 4, N'II-B',  N'Type II',  18, 12.00, 22.20, 22.20, 0, 2024,  30, 1),
    ( 31, N'2nd 3 yrs of GR', N'4', 4, 4, N'III-A', N'Type III', 30, 25.00, 13.30, 13.30, 0, 2024,  31, 1),
    ( 32, N'2nd 3 yrs of GR', N'4', 4, 4, N'III-B', N'Type III', 25, 20.00, 16.00, 16.00, 0, 2024,  32, 1),
    ( 33, N'2nd 3 yrs of GR', N'4', 4, 4, N'IV-A',  N'Type IV',  40, 35.00, 10.00, 10.00, 0, 2024,  33, 1),
    ( 34, N'2nd 3 yrs of GR', N'4', 4, 4, N'IV-B',  N'Type IV',  38, 33.00, 10.50, 10.50, 0, 2024,  34, 1),
    ( 35, N'2nd 3 yrs of GR', N'4', 4, 4, N'V-A',   N'Type V',   50, 40.00,  8.00,  8.00, 0, 2024,  35, 1),
    ( 36, N'2nd 3 yrs of GR', N'4', 4, 4, N'V-B',   N'Type V',   45, 37.00,  8.90,  8.90, 0, 2024,  36, 1),

    -- Age 5 (5..5)
    ( 37, N'2nd 3 yrs of GR', N'5', 5, 5, N'I',     N'Type I',   15, 10.00, 33.30, 33.30, 0, 2024,  37, 1),
    ( 38, N'2nd 3 yrs of GR', N'5', 5, 5, N'II-A',  N'Type II',  20, 15.00, 25.00, 25.00, 0, 2024,  38, 1),
    ( 39, N'2nd 3 yrs of GR', N'5', 5, 5, N'II-B',  N'Type II',  18, 12.00, 27.80, 27.80, 0, 2024,  39, 1),
    ( 40, N'2nd 3 yrs of GR', N'5', 5, 5, N'III-A', N'Type III', 30, 25.00, 16.70, 16.70, 0, 2024,  40, 1),
    ( 41, N'2nd 3 yrs of GR', N'5', 5, 5, N'III-B', N'Type III', 25, 20.00, 20.00, 20.00, 0, 2024,  41, 1),
    ( 42, N'2nd 3 yrs of GR', N'5', 5, 5, N'IV-A',  N'Type IV',  40, 35.00, 12.50, 12.50, 0, 2024,  42, 1),
    ( 43, N'2nd 3 yrs of GR', N'5', 5, 5, N'IV-B',  N'Type IV',  38, 33.00, 13.20, 13.20, 0, 2024,  43, 1),
    ( 44, N'2nd 3 yrs of GR', N'5', 5, 5, N'V-A',   N'Type V',   50, 40.00, 10.00, 10.00, 0, 2024,  44, 1),
    ( 45, N'2nd 3 yrs of GR', N'5', 5, 5, N'V-B',   N'Type V',   45, 37.00, 11.10, 11.10, 0, 2024,  45, 1),

    -- Age 6 (6..6)
    ( 46, N'2nd 3 yrs of GR', N'6', 6, 6, N'I',     N'Type I',   15, 10.00, 40.00, 40.00, 0, 2024,  46, 1),
    ( 47, N'2nd 3 yrs of GR', N'6', 6, 6, N'II-A',  N'Type II',  20, 15.00, 30.00, 30.00, 0, 2024,  47, 1),
    ( 48, N'2nd 3 yrs of GR', N'6', 6, 6, N'II-B',  N'Type II',  18, 12.00, 33.30, 33.30, 0, 2024,  48, 1),
    ( 49, N'2nd 3 yrs of GR', N'6', 6, 6, N'III-A', N'Type III', 30, 25.00, 20.00, 20.00, 0, 2024,  49, 1),
    ( 50, N'2nd 3 yrs of GR', N'6', 6, 6, N'III-B', N'Type III', 25, 20.00, 24.00, 24.00, 0, 2024,  50, 1),
    ( 51, N'2nd 3 yrs of GR', N'6', 6, 6, N'IV-A',  N'Type IV',  40, 35.00, 15.00, 15.00, 0, 2024,  51, 1),
    ( 52, N'2nd 3 yrs of GR', N'6', 6, 6, N'IV-B',  N'Type IV',  38, 33.00, 15.80, 15.80, 0, 2024,  52, 1),
    ( 53, N'2nd 3 yrs of GR', N'6', 6, 6, N'V-A',   N'Type V',   50, 40.00, 12.00, 12.00, 0, 2024,  53, 1),
    ( 54, N'2nd 3 yrs of GR', N'6', 6, 6, N'V-B',   N'Type V',   45, 37.00, 13.30, 13.30, 0, 2024,  54, 1),

    -- =========================================================================
    -- 3rd 3 yrs of GR (Building Age 7, 8, 9)
    -- =========================================================================
    -- Age 7 (7..7)
    ( 55, N'3rd 3 yrs of GR', N'7', 7, 7, N'I',     N'Type I',   15, 10.00, 46.70, 46.70, 0, 2024,  55, 1),
    ( 56, N'3rd 3 yrs of GR', N'7', 7, 7, N'II-A',  N'Type II',  20, 15.00, 35.00, 35.00, 0, 2024,  56, 1),
    ( 57, N'3rd 3 yrs of GR', N'7', 7, 7, N'II-B',  N'Type II',  18, 12.00, 38.90, 38.90, 0, 2024,  57, 1),
    ( 58, N'3rd 3 yrs of GR', N'7', 7, 7, N'III-A', N'Type III', 30, 25.00, 23.30, 23.30, 0, 2024,  58, 1),
    ( 59, N'3rd 3 yrs of GR', N'7', 7, 7, N'III-B', N'Type III', 25, 20.00, 28.00, 28.00, 0, 2024,  59, 1),
    ( 60, N'3rd 3 yrs of GR', N'7', 7, 7, N'IV-A',  N'Type IV',  40, 35.00, 17.50, 17.50, 0, 2024,  60, 1),
    ( 61, N'3rd 3 yrs of GR', N'7', 7, 7, N'IV-B',  N'Type IV',  38, 33.00, 18.40, 18.40, 0, 2024,  61, 1),
    ( 62, N'3rd 3 yrs of GR', N'7', 7, 7, N'V-A',   N'Type V',   50, 40.00, 14.00, 14.00, 0, 2024,  62, 1),
    ( 63, N'3rd 3 yrs of GR', N'7', 7, 7, N'V-B',   N'Type V',   45, 37.00, 15.60, 15.60, 0, 2024,  63, 1),

    -- Age 8 (8..8)
    ( 64, N'3rd 3 yrs of GR', N'8', 8, 8, N'I',     N'Type I',   15, 10.00, 53.30, 53.30, 0, 2024,  64, 1),
    ( 65, N'3rd 3 yrs of GR', N'8', 8, 8, N'II-A',  N'Type II',  20, 15.00, 40.00, 40.00, 0, 2024,  65, 1),
    ( 66, N'3rd 3 yrs of GR', N'8', 8, 8, N'II-B',  N'Type II',  18, 12.00, 44.40, 44.40, 0, 2024,  66, 1),
    ( 67, N'3rd 3 yrs of GR', N'8', 8, 8, N'III-A', N'Type III', 30, 25.00, 26.70, 26.70, 0, 2024,  67, 1),
    ( 68, N'3rd 3 yrs of GR', N'8', 8, 8, N'III-B', N'Type III', 25, 20.00, 32.00, 32.00, 0, 2024,  68, 1),
    ( 69, N'3rd 3 yrs of GR', N'8', 8, 8, N'IV-A',  N'Type IV',  40, 35.00, 20.00, 20.00, 0, 2024,  69, 1),
    ( 70, N'3rd 3 yrs of GR', N'8', 8, 8, N'IV-B',  N'Type IV',  38, 33.00, 21.10, 21.10, 0, 2024,  70, 1),
    ( 71, N'3rd 3 yrs of GR', N'8', 8, 8, N'V-A',   N'Type V',   50, 40.00, 16.00, 16.00, 0, 2024,  71, 1),
    ( 72, N'3rd 3 yrs of GR', N'8', 8, 8, N'V-B',   N'Type V',   45, 37.00, 17.80, 17.80, 0, 2024,  72, 1),

    -- Age 9 (9..9)
    ( 73, N'3rd 3 yrs of GR', N'9', 9, 9, N'I',     N'Type I',   15, 10.00, 60.00, 60.00, 0, 2024,  73, 1),
    ( 74, N'3rd 3 yrs of GR', N'9', 9, 9, N'II-A',  N'Type II',  20, 15.00, 45.00, 45.00, 0, 2024,  74, 1),
    ( 75, N'3rd 3 yrs of GR', N'9', 9, 9, N'II-B',  N'Type II',  18, 12.00, 50.00, 50.00, 0, 2024,  75, 1),
    ( 76, N'3rd 3 yrs of GR', N'9', 9, 9, N'III-A', N'Type III', 30, 25.00, 30.00, 30.00, 0, 2024,  76, 1),
    ( 77, N'3rd 3 yrs of GR', N'9', 9, 9, N'III-B', N'Type III', 25, 20.00, 36.00, 36.00, 0, 2024,  77, 1),
    ( 78, N'3rd 3 yrs of GR', N'9', 9, 9, N'IV-A',  N'Type IV',  40, 35.00, 22.50, 22.50, 0, 2024,  78, 1),
    ( 79, N'3rd 3 yrs of GR', N'9', 9, 9, N'IV-B',  N'Type IV',  38, 33.00, 23.70, 23.70, 0, 2024,  79, 1),
    ( 80, N'3rd 3 yrs of GR', N'9', 9, 9, N'V-A',   N'Type V',   50, 40.00, 18.00, 18.00, 0, 2024,  80, 1),
    ( 81, N'3rd 3 yrs of GR', N'9', 9, 9, N'V-B',   N'Type V',   45, 37.00, 20.00, 20.00, 0, 2024,  81, 1),

    -- =========================================================================
    -- 4th 3 yrs of GR (Building Age 10, 11, 12)
    -- =========================================================================
    -- Age 10 (10..10)
    ( 82, N'4th 3 yrs of GR', N'10', 10, 10, N'I',     N'Type I',   15, 10.00, 66.70, 66.70, 0, 2024,  82, 1),
    ( 83, N'4th 3 yrs of GR', N'10', 10, 10, N'II-A',  N'Type II',  20, 15.00, 50.00, 50.00, 0, 2024,  83, 1),
    ( 84, N'4th 3 yrs of GR', N'10', 10, 10, N'II-B',  N'Type II',  18, 12.00, 55.60, 55.60, 0, 2024,  84, 1),
    ( 85, N'4th 3 yrs of GR', N'10', 10, 10, N'III-A', N'Type III', 30, 25.00, 33.30, 33.30, 0, 2024,  85, 1),
    ( 86, N'4th 3 yrs of GR', N'10', 10, 10, N'III-B', N'Type III', 25, 20.00, 40.00, 40.00, 0, 2024,  86, 1),
    ( 87, N'4th 3 yrs of GR', N'10', 10, 10, N'IV-A',  N'Type IV',  40, 35.00, 25.00, 25.00, 0, 2024,  87, 1),
    ( 88, N'4th 3 yrs of GR', N'10', 10, 10, N'IV-B',  N'Type IV',  38, 33.00, 26.30, 26.30, 0, 2024,  88, 1),
    ( 89, N'4th 3 yrs of GR', N'10', 10, 10, N'V-A',   N'Type V',   50, 40.00, 20.00, 20.00, 0, 2024,  89, 1),
    ( 90, N'4th 3 yrs of GR', N'10', 10, 10, N'V-B',   N'Type V',   45, 37.00, 22.20, 22.20, 0, 2024,  90, 1),

    -- Age 11 (11..11)
    ( 91, N'4th 3 yrs of GR', N'11', 11, 11, N'I',     N'Type I',   15, 10.00, 73.30, 73.30, 0, 2024,  91, 1),
    ( 92, N'4th 3 yrs of GR', N'11', 11, 11, N'II-A',  N'Type II',  20, 15.00, 55.00, 55.00, 0, 2024,  92, 1),
    ( 93, N'4th 3 yrs of GR', N'11', 11, 11, N'II-B',  N'Type II',  18, 12.00, 61.10, 61.10, 0, 2024,  93, 1),
    ( 94, N'4th 3 yrs of GR', N'11', 11, 11, N'III-A', N'Type III', 30, 25.00, 36.70, 36.70, 0, 2024,  94, 1),
    ( 95, N'4th 3 yrs of GR', N'11', 11, 11, N'III-B', N'Type III', 25, 20.00, 44.00, 44.00, 0, 2024,  95, 1),
    ( 96, N'4th 3 yrs of GR', N'11', 11, 11, N'IV-A',  N'Type IV',  40, 35.00, 27.50, 27.50, 0, 2024,  96, 1),
    ( 97, N'4th 3 yrs of GR', N'11', 11, 11, N'IV-B',  N'Type IV',  38, 33.00, 28.90, 28.90, 0, 2024,  97, 1),
    ( 98, N'4th 3 yrs of GR', N'11', 11, 11, N'V-A',   N'Type V',   50, 40.00, 22.00, 22.00, 0, 2024,  98, 1),
    ( 99, N'4th 3 yrs of GR', N'11', 11, 11, N'V-B',   N'Type V',   45, 37.00, 24.40, 24.40, 0, 2024,  99, 1),

    -- Age 12 (12..12)
    (100, N'4th 3 yrs of GR', N'12', 12, 12, N'I',     N'Type I',   15, 10.00, 80.00, 80.00, 0, 2024, 100, 1),
    (101, N'4th 3 yrs of GR', N'12', 12, 12, N'II-A',  N'Type II',  20, 15.00, 60.00, 60.00, 0, 2024, 101, 1),
    (102, N'4th 3 yrs of GR', N'12', 12, 12, N'II-B',  N'Type II',  18, 12.00, 66.70, 66.70, 0, 2024, 102, 1),
    (103, N'4th 3 yrs of GR', N'12', 12, 12, N'III-A', N'Type III', 30, 25.00, 40.00, 40.00, 0, 2024, 103, 1),
    (104, N'4th 3 yrs of GR', N'12', 12, 12, N'III-B', N'Type III', 25, 20.00, 48.00, 48.00, 0, 2024, 104, 1),
    (105, N'4th 3 yrs of GR', N'12', 12, 12, N'IV-A',  N'Type IV',  40, 35.00, 30.00, 30.00, 0, 2024, 105, 1),
    (106, N'4th 3 yrs of GR', N'12', 12, 12, N'IV-B',  N'Type IV',  38, 33.00, 31.60, 31.60, 0, 2024, 106, 1),
    (107, N'4th 3 yrs of GR', N'12', 12, 12, N'V-A',   N'Type V',   50, 40.00, 24.00, 24.00, 0, 2024, 107, 1),
    (108, N'4th 3 yrs of GR', N'12', 12, 12, N'V-B',   N'Type V',   45, 37.00, 26.70, 26.70, 0, 2024, 108, 1),

    -- =========================================================================
    -- 5th 3 yrs of GR (Building Age 13, 14, 15)
    -- =========================================================================
    -- Age 13 (13..13)
    (109, N'5th 3 yrs of GR', N'13', 13, 13, N'I',     N'Type I',   15, 10.00, 86.70, 86.70, 0, 2024, 109, 1),
    (110, N'5th 3 yrs of GR', N'13', 13, 13, N'II-A',  N'Type II',  20, 15.00, 65.00, 65.00, 0, 2024, 110, 1),
    (111, N'5th 3 yrs of GR', N'13', 13, 13, N'II-B',  N'Type II',  18, 12.00, 72.20, 72.20, 0, 2024, 111, 1),
    (112, N'5th 3 yrs of GR', N'13', 13, 13, N'III-A', N'Type III', 30, 25.00, 43.30, 43.30, 0, 2024, 112, 1),
    (113, N'5th 3 yrs of GR', N'13', 13, 13, N'III-B', N'Type III', 25, 20.00, 52.00, 52.00, 0, 2024, 113, 1),
    (114, N'5th 3 yrs of GR', N'13', 13, 13, N'IV-A',  N'Type IV',  40, 35.00, 32.50, 32.50, 0, 2024, 114, 1),
    (115, N'5th 3 yrs of GR', N'13', 13, 13, N'IV-B',  N'Type IV',  38, 33.00, 34.20, 34.20, 0, 2024, 115, 1),
    (116, N'5th 3 yrs of GR', N'13', 13, 13, N'V-A',   N'Type V',   50, 40.00, 26.00, 26.00, 0, 2024, 116, 1),
    (117, N'5th 3 yrs of GR', N'13', 13, 13, N'V-B',   N'Type V',   45, 37.00, 28.90, 28.90, 0, 2024, 117, 1),

    -- Age 14 (14..14)
    (118, N'5th 3 yrs of GR', N'14', 14, 14, N'I',     N'Type I',   15, 10.00, 93.30, 93.30, 0, 2024, 118, 1),
    (119, N'5th 3 yrs of GR', N'14', 14, 14, N'II-A',  N'Type II',  20, 15.00, 70.00, 70.00, 0, 2024, 119, 1),
    (120, N'5th 3 yrs of GR', N'14', 14, 14, N'II-B',  N'Type II',  18, 12.00, 77.80, 77.80, 0, 2024, 120, 1),
    (121, N'5th 3 yrs of GR', N'14', 14, 14, N'III-A', N'Type III', 30, 25.00, 46.70, 46.70, 0, 2024, 121, 1),
    (122, N'5th 3 yrs of GR', N'14', 14, 14, N'III-B', N'Type III', 25, 20.00, 56.00, 56.00, 0, 2024, 122, 1),
    (123, N'5th 3 yrs of GR', N'14', 14, 14, N'IV-A',  N'Type IV',  40, 35.00, 35.00, 35.00, 0, 2024, 123, 1),
    (124, N'5th 3 yrs of GR', N'14', 14, 14, N'IV-B',  N'Type IV',  38, 33.00, 36.80, 36.80, 0, 2024, 124, 1),
    (125, N'5th 3 yrs of GR', N'14', 14, 14, N'V-A',   N'Type V',   50, 40.00, 28.00, 28.00, 0, 2024, 125, 1),
    (126, N'5th 3 yrs of GR', N'14', 14, 14, N'V-B',   N'Type V',   45, 37.00, 31.10, 31.10, 0, 2024, 126, 1),

    -- Age 15 (15..15)
    (127, N'5th 3 yrs of GR', N'15', 15, 15, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 127, 1),
    (128, N'5th 3 yrs of GR', N'15', 15, 15, N'II-A',  N'Type II',  20, 15.00, 75.00, 75.00, 0, 2024, 128, 1),
    (129, N'5th 3 yrs of GR', N'15', 15, 15, N'II-B',  N'Type II',  18, 12.00, 83.30, 83.30, 0, 2024, 129, 1),
    (130, N'5th 3 yrs of GR', N'15', 15, 15, N'III-A', N'Type III', 30, 25.00, 50.00, 50.00, 0, 2024, 130, 1),
    (131, N'5th 3 yrs of GR', N'15', 15, 15, N'III-B', N'Type III', 25, 20.00, 60.00, 60.00, 0, 2024, 131, 1),
    (132, N'5th 3 yrs of GR', N'15', 15, 15, N'IV-A',  N'Type IV',  40, 35.00, 37.50, 37.50, 0, 2024, 132, 1),
    (133, N'5th 3 yrs of GR', N'15', 15, 15, N'IV-B',  N'Type IV',  38, 33.00, 39.50, 39.50, 0, 2024, 133, 1),
    (134, N'5th 3 yrs of GR', N'15', 15, 15, N'V-A',   N'Type V',   50, 40.00, 30.00, 30.00, 0, 2024, 134, 1),
    (135, N'5th 3 yrs of GR', N'15', 15, 15, N'V-B',   N'Type V',   45, 37.00, 33.30, 33.30, 0, 2024, 135, 1),

    -- =========================================================================
    -- 6th 3 yrs of GR (Building Age 16, 17, 18–20)
    -- =========================================================================
    -- Age 16 (16..16)
    (136, N'6th 3 yrs of GR', N'16', 16, 16, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 136, 1),
    (137, N'6th 3 yrs of GR', N'16', 16, 16, N'II-A',  N'Type II',  20, 15.00, 80.00, 80.00, 0, 2024, 137, 1),
    (138, N'6th 3 yrs of GR', N'16', 16, 16, N'II-B',  N'Type II',  18, 12.00, 88.90, 88.90, 0, 2024, 138, 1),
    (139, N'6th 3 yrs of GR', N'16', 16, 16, N'III-A', N'Type III', 30, 25.00, 53.30, 53.30, 0, 2024, 139, 1),
    (140, N'6th 3 yrs of GR', N'16', 16, 16, N'III-B', N'Type III', 25, 20.00, 64.00, 64.00, 0, 2024, 140, 1),
    (141, N'6th 3 yrs of GR', N'16', 16, 16, N'IV-A',  N'Type IV',  40, 35.00, 40.00, 40.00, 0, 2024, 141, 1),
    (142, N'6th 3 yrs of GR', N'16', 16, 16, N'IV-B',  N'Type IV',  38, 33.00, 42.10, 42.10, 0, 2024, 142, 1),
    (143, N'6th 3 yrs of GR', N'16', 16, 16, N'V-A',   N'Type V',   50, 40.00, 32.00, 32.00, 0, 2024, 143, 1),
    (144, N'6th 3 yrs of GR', N'16', 16, 16, N'V-B',   N'Type V',   45, 37.00, 35.60, 35.60, 0, 2024, 144, 1),

    -- Age 17 (17..17)
    (145, N'6th 3 yrs of GR', N'17', 17, 17, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 145, 1),
    (146, N'6th 3 yrs of GR', N'17', 17, 17, N'II-A',  N'Type II',  20, 15.00, 85.00, 85.00, 0, 2024, 146, 1),
    (147, N'6th 3 yrs of GR', N'17', 17, 17, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 147, 1),
    (148, N'6th 3 yrs of GR', N'17', 17, 17, N'III-A', N'Type III', 30, 25.00, 56.70, 56.70, 0, 2024, 148, 1),
    (149, N'6th 3 yrs of GR', N'17', 17, 17, N'III-B', N'Type III', 25, 20.00, 68.00, 68.00, 0, 2024, 149, 1),
    (150, N'6th 3 yrs of GR', N'17', 17, 17, N'IV-A',  N'Type IV',  40, 35.00, 42.50, 42.50, 0, 2024, 150, 1),
    (151, N'6th 3 yrs of GR', N'17', 17, 17, N'IV-B',  N'Type IV',  38, 33.00, 44.70, 44.70, 0, 2024, 151, 1),
    (152, N'6th 3 yrs of GR', N'17', 17, 17, N'V-A',   N'Type V',   50, 40.00, 34.00, 34.00, 0, 2024, 152, 1),
    (153, N'6th 3 yrs of GR', N'17', 17, 17, N'V-B',   N'Type V',   45, 37.00, 37.80, 37.80, 0, 2024, 153, 1),

    -- Age 18–20 (18..20)
    (154, N'6th 3 yrs of GR', N'18–20', 18, 20, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 154, 1),
    (155, N'6th 3 yrs of GR', N'18–20', 18, 20, N'II-A',  N'Type II',  20, 15.00,  NULL, 85.00, 1, 2024, 155, 1),
    (156, N'6th 3 yrs of GR', N'18–20', 18, 20, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 156, 1),
    (157, N'6th 3 yrs of GR', N'18–20', 18, 20, N'III-A', N'Type III', 30, 25.00, 66.70, 66.70, 0, 2024, 157, 1),
    (158, N'6th 3 yrs of GR', N'18–20', 18, 20, N'III-B', N'Type III', 25, 20.00, 80.00, 80.00, 0, 2024, 158, 1),
    (159, N'6th 3 yrs of GR', N'18–20', 18, 20, N'IV-A',  N'Type IV',  40, 35.00, 50.00, 50.00, 0, 2024, 159, 1),
    (160, N'6th 3 yrs of GR', N'18–20', 18, 20, N'IV-B',  N'Type IV',  38, 33.00, 52.60, 52.60, 0, 2024, 160, 1),
    (161, N'6th 3 yrs of GR', N'18–20', 18, 20, N'V-A',   N'Type V',   50, 40.00, 40.00, 40.00, 0, 2024, 161, 1),
    (162, N'6th 3 yrs of GR', N'18–20', 18, 20, N'V-B',   N'Type V',   45, 37.00, 44.40, 44.40, 0, 2024, 162, 1),

    -- =========================================================================
    -- After 20 yrs GR (Building Age 21–23, 24–26, 27–30)
    -- =========================================================================
    -- Age 21–23 (21..23)
    (163, N'After 20 yrs GR', N'21–23', 21, 23, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 163, 1),
    (164, N'After 20 yrs GR', N'21–23', 21, 23, N'II-A',  N'Type II',  20, 15.00,  NULL, 85.00, 1, 2024, 164, 1),
    (165, N'After 20 yrs GR', N'21–23', 21, 23, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 165, 1),
    (166, N'After 20 yrs GR', N'21–23', 21, 23, N'III-A', N'Type III', 30, 25.00, 76.70, 76.70, 0, 2024, 166, 1),
    (167, N'After 20 yrs GR', N'21–23', 21, 23, N'III-B', N'Type III', 25, 20.00, 76.50, 76.50, 0, 2024, 167, 1),
    (168, N'After 20 yrs GR', N'21–23', 21, 23, N'IV-A',  N'Type IV',  40, 35.00, 57.50, 57.50, 0, 2024, 168, 1),
    (169, N'After 20 yrs GR', N'21–23', 21, 23, N'IV-B',  N'Type IV',  38, 33.00, 60.50, 60.50, 0, 2024, 169, 1),
    (170, N'After 20 yrs GR', N'21–23', 21, 23, N'V-A',   N'Type V',   50, 40.00, 46.00, 46.00, 0, 2024, 170, 1),
    (171, N'After 20 yrs GR', N'21–23', 21, 23, N'V-B',   N'Type V',   45, 37.00, 51.10, 51.10, 0, 2024, 171, 1),

    -- Age 24–26 (24..26)
    (172, N'After 20 yrs GR', N'24–26', 24, 26, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 172, 1),
    (173, N'After 20 yrs GR', N'24–26', 24, 26, N'II-A',  N'Type II',  20, 15.00,  NULL, 85.00, 1, 2024, 173, 1),
    (174, N'After 20 yrs GR', N'24–26', 24, 26, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 174, 1),
    (175, N'After 20 yrs GR', N'24–26', 24, 26, N'III-A', N'Type III', 30, 25.00,  NULL, 75.00, 1, 2024, 175, 1),
    (176, N'After 20 yrs GR', N'24–26', 24, 26, N'III-B', N'Type III', 25, 20.00, 80.00, 80.00, 0, 2024, 176, 1),
    (177, N'After 20 yrs GR', N'24–26', 24, 26, N'IV-A',  N'Type IV',  40, 35.00, 65.00, 65.00, 0, 2024, 177, 1),
    (178, N'After 20 yrs GR', N'24–26', 24, 26, N'IV-B',  N'Type IV',  38, 33.00, 68.40, 68.40, 0, 2024, 178, 1),
    (179, N'After 20 yrs GR', N'24–26', 24, 26, N'V-A',   N'Type V',   50, 40.00, 52.00, 52.00, 0, 2024, 179, 1),
    (180, N'After 20 yrs GR', N'24–26', 24, 26, N'V-B',   N'Type V',   45, 37.00, 57.80, 57.80, 0, 2024, 180, 1),

    -- Age 27–30 (27..30)
    (181, N'After 20 yrs GR', N'27–30', 27, 30, N'I',     N'Type I',   15, 10.00,  NULL, 90.00, 1, 2024, 181, 1),
    (182, N'After 20 yrs GR', N'27–30', 27, 30, N'II-A',  N'Type II',  20, 15.00,  NULL, 85.00, 1, 2024, 182, 1),
    (183, N'After 20 yrs GR', N'27–30', 27, 30, N'II-B',  N'Type II',  18, 12.00,  NULL, 88.00, 1, 2024, 183, 1),
    (184, N'After 20 yrs GR', N'27–30', 27, 30, N'III-A', N'Type III', 30, 25.00,  NULL, 75.00, 1, 2024, 184, 1),
    (185, N'After 20 yrs GR', N'27–30', 27, 30, N'III-B', N'Type III', 25, 20.00,  NULL, 80.00, 1, 2024, 185, 1),
    (186, N'After 20 yrs GR', N'27–30', 27, 30, N'IV-A',  N'Type IV',  40, 35.00,  NULL, 65.00, 1, 2024, 186, 1),
    (187, N'After 20 yrs GR', N'27–30', 27, 30, N'IV-B',  N'Type IV',  38, 33.00,  NULL, 67.00, 1, 2024, 187, 1),
    (188, N'After 20 yrs GR', N'27–30', 27, 30, N'V-A',   N'Type V',   50, 40.00, 60.00, 60.00, 0, 2024, 188, 1),
    (189, N'After 20 yrs GR', N'27–30', 27, 30, N'V-B',   N'Type V',   45, 37.00, 64.40, 64.40, 0, 2024, 189, 1)
) AS Source (
    [Id],
    [GeneralRevisionCycle],
    [BuildingAgeLabel],
    [AgeLowerBound],
    [AgeUpperBound],
    [ClassificationCode],
    [StructuralType],
    [EstimatedEconomicLifeYears],
    [EstimatedResidualValuePercent],
    [DepreciationPercent],
    [EffectiveDepreciationPercent],
    [IsResidualFloorCap],
    [RevisionYear],
    [SortOrder],
    [IsActive]
)
ON Target.[Id] = Source.[Id]
WHEN MATCHED THEN
    UPDATE SET
        Target.[GeneralRevisionCycle]          = Source.[GeneralRevisionCycle],
        Target.[BuildingAgeLabel]              = Source.[BuildingAgeLabel],
        Target.[AgeLowerBound]                 = Source.[AgeLowerBound],
        Target.[AgeUpperBound]                 = Source.[AgeUpperBound],
        Target.[ClassificationCode]            = Source.[ClassificationCode],
        Target.[StructuralType]                = Source.[StructuralType],
        Target.[EstimatedEconomicLifeYears]    = Source.[EstimatedEconomicLifeYears],
        Target.[EstimatedResidualValuePercent] = Source.[EstimatedResidualValuePercent],
        Target.[DepreciationPercent]           = Source.[DepreciationPercent],
        Target.[EffectiveDepreciationPercent]  = Source.[EffectiveDepreciationPercent],
        Target.[IsResidualFloorCap]            = Source.[IsResidualFloorCap],
        Target.[RevisionYear]                  = Source.[RevisionYear],
        Target.[SortOrder]                     = Source.[SortOrder],
        Target.[IsActive]                      = Source.[IsActive]
WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Id],
        [GeneralRevisionCycle],
        [BuildingAgeLabel],
        [AgeLowerBound],
        [AgeUpperBound],
        [ClassificationCode],
        [StructuralType],
        [EstimatedEconomicLifeYears],
        [EstimatedResidualValuePercent],
        [DepreciationPercent],
        [EffectiveDepreciationPercent],
        [IsResidualFloorCap],
        [RevisionYear],
        [SortOrder],
        [IsActive]
    )
    VALUES (
        Source.[Id],
        Source.[GeneralRevisionCycle],
        Source.[BuildingAgeLabel],
        Source.[AgeLowerBound],
        Source.[AgeUpperBound],
        Source.[ClassificationCode],
        Source.[StructuralType],
        Source.[EstimatedEconomicLifeYears],
        Source.[EstimatedResidualValuePercent],
        Source.[DepreciationPercent],
        Source.[EffectiveDepreciationPercent],
        Source.[IsResidualFloorCap],
        Source.[RevisionYear],
        Source.[SortOrder],
        Source.[IsActive]
    );

SET IDENTITY_INSERT [dbo].[BuildingDepreciationSchedules] OFF;
GO

-- -----------------------------------------------------------------------------
-- 3. APPLY SCHEDULE COLUMNS & FOREIGN KEY TO [dbo].[TDNBuildings]
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[TDNBuildings]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.TDNBuildings', N'BuildingDepreciationScheduleId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings]
        ADD [BuildingDepreciationScheduleId] INT NULL;
    END

    IF COL_LENGTH(N'dbo.TDNBuildings', N'GeneralRevisionCycle') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings]
        ADD [GeneralRevisionCycle] NVARCHAR(50) NULL;
    END

    IF COL_LENGTH(N'dbo.TDNBuildings', N'EstimatedEconomicLifeYears') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings]
        ADD [EstimatedEconomicLifeYears] INT NOT NULL CONSTRAINT [DF_TDNBuildings_EstEconLife] DEFAULT (30);
    END

    IF COL_LENGTH(N'dbo.TDNBuildings', N'EstimatedResidualValuePercent') IS NULL
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings]
        ADD [EstimatedResidualValuePercent] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_TDNBuildings_EstResidualVal] DEFAULT (25.00);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_TDNBuildings_BuildingDepreciationSchedules_BuildingDepreciationScheduleId'
    )
    BEGIN
        ALTER TABLE [dbo].[TDNBuildings] WITH CHECK
        ADD CONSTRAINT [FK_TDNBuildings_BuildingDepreciationSchedules_BuildingDepreciationScheduleId]
        FOREIGN KEY ([BuildingDepreciationScheduleId])
        REFERENCES [dbo].[BuildingDepreciationSchedules] ([Id])
        ON DELETE SET NULL;
    END
END
GO

-- -----------------------------------------------------------------------------
-- 4. BACKFILL EXISTING [dbo].[TDNBuildings] RECORDS WITH MATCHING SCHEDULE ROWS
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[TDNBuildings]', N'U') IS NOT NULL
BEGIN
    UPDATE b
    SET
        b.[BuildingDepreciationScheduleId] = s.[Id],
        b.[GeneralRevisionCycle]           = s.[GeneralRevisionCycle],
        b.[EstimatedEconomicLifeYears]     = s.[EstimatedEconomicLifeYears],
        b.[EstimatedResidualValuePercent]  = s.[EstimatedResidualValuePercent],
        b.[BuildingDepreciationPercent]    = CASE
                                                 WHEN b.[BuildingDepreciationPercent] <= 0 THEN s.[EffectiveDepreciationPercent]
                                                 ELSE b.[BuildingDepreciationPercent]
                                             END
    FROM [dbo].[TDNBuildings] b
    INNER JOIN [dbo].[BuildingDepreciationSchedules] s
        ON s.[ClassificationCode] = ISNULL(NULLIF(LTRIM(RTRIM(b.[BuildingClassification])), N''), N'III-A')
       AND (
            (b.[BuildingAgeYears] >= s.[AgeLowerBound] AND b.[BuildingAgeYears] <= s.[AgeUpperBound])
            OR (b.[BuildingAgeYears] > 30 AND s.[AgeUpperBound] = 30)
           )
    WHERE b.[BuildingAgeYears] > 0;
END
GO
