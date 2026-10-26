-- ==============================================================================
-- Seed Script: Standard Building Additional Items (CAST System Alignment)
-- Description: Inserts standard additional items / improvements reference data (52 items)
-- Target Table: dbo.BuildingAdditionalItems
-- ==============================================================================

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.BuildingAdditionalItems', N'U') IS NOT NULL
BEGIN
    PRINT 'Seeding reference data into dbo.BuildingAdditionalItems...';

    SET IDENTITY_INSERT [dbo].[BuildingAdditionalItems] ON;

    MERGE INTO [dbo].[BuildingAdditionalItems] AS target
    USING (VALUES
    (1, N'CARPORT', 30.00, 0.00, 0.00, 0.00, 2011, 1, 1),
    (2, N'MEZZANINE', 60.00, 0.00, 0.00, 0.00, 2011, 1, 2),
    (3, N'PORCH', 40.00, 0.00, 0.00, 0.00, 2011, 1, 3),
    (4, N'BALCONY', 45.00, 0.00, 0.00, 0.00, 2011, 1, 4),
    (5, N'GARAGE', 45.00, 0.00, 0.00, 0.00, 2011, 1, 5),
    (6, N'TERRACE : COVERED', 40.00, 0.00, 0.00, 0.00, 2011, 1, 6),
    (7, N'TERRACE : OPEN', 20.00, 0.00, 0.00, 0.00, 2011, 1, 7),
    (8, N'DECK ROOF : COVERED', 40.00, 0.00, 0.00, 0.00, 2011, 1, 8),
    (9, N'DECK ROOF : OPEN', 20.00, 0.00, 0.00, 0.00, 2011, 1, 9),
    (10, N'BASEMENT : RESIDENTIAL', 70.00, 0.00, 0.00, 0.00, 2011, 1, 10),
    (11, N'PAVEMENTS : TENNIS COURT', 0.00, 75.00, 0.00, 0.00, 2011, 1, 11),
    (12, N'PAVEMENTS : CONCRETE 5 CM. THICK', 0.00, 75.00, 0.00, 0.00, 2011, 1, 12),
    (13, N'PAVEMENTS : CONCRETE 10 CM. THICK', 0.00, 150.00, 0.00, 0.00, 2011, 1, 13),
    (14, N'PAVEMENTS :CONCRETE 15 CM. THICK', 0.00, 225.00, 0.00, 0.00, 2011, 1, 14),
    (15, N'PAVEMENTS : CONCRETE 20 CM. THICK', 0.00, 300.00, 0.00, 0.00, 2011, 1, 15),
    (16, N'PAVEMENTS : ASPHALT 1 COURSE', 0.00, 120.00, 0.00, 0.00, 2011, 1, 16),
    (17, N'PAVEMENTS : ASPHALT 2 COURSE', 0.00, 150.00, 0.00, 0.00, 2011, 1, 17),
    (18, N'PAVEMENTS : ASPHALT 3 COURSE', 0.00, 180.00, 0.00, 0.00, 2011, 1, 18),
    (19, N'FLOOR FINISHES : GRANITE TILES', 0.00, 1000.00, 0.00, 0.00, 2011, 1, 19),
    (20, N'FLOOR FINISHES : MARBLE SLABS', 0.00, 700.00, 0.00, 0.00, 2011, 1, 20),
    (21, N'FLOOR FINISHES : MARBLE TILES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 21),
    (22, N'FLOOR FINISHES : CERAMIC TILES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 22),
    (23, N'FLOOR FINISHES : CRAZY CUT MARBLES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 23),
    (24, N'FLOOR FINISHES : GRANOLITHIC', 0.00, 400.00, 0.00, 0.00, 2011, 1, 24),
    (25, N'FLOOR FINISHES : NARRA', 0.00, 600.00, 0.00, 0.00, 2011, 1, 25),
    (26, N'FLOOR FINISHES : YAKAL', 0.00, 500.00, 0.00, 0.00, 2011, 1, 26),
    (27, N'FLOOR FINISHES : NARRA/FANCY WOOD TILES', 0.00, 400.00, 0.00, 0.00, 2011, 1, 27),
    (28, N'FLOOR FINISHES : ORDINARY WOOD TILES', 0.00, 400.00, 0.00, 0.00, 2011, 1, 28),
    (29, N'FLOOR FINISHES : VINYL TILES', 0.00, 300.00, 0.00, 0.00, 2011, 1, 29),
    (30, N'FLOOR FINISHES : WASHOUT PEEBLES', 0.00, 100.00, 0.00, 0.00, 2011, 1, 30),
    (31, N'FLOOR FINISHES : UNGLAZED TILES', 0.00, 300.00, 0.00, 0.00, 2011, 1, 31),
    (33, N'WALLING : DOUBLE WALLING (ORDINARY PLYWOOD)', 0.00, 200.00, 0.00, 0.00, 2011, 1, 33),
    (34, N'WALLING : DOUBLE WALLING (NARRA PANELING)', 0.00, 300.00, 0.00, 0.00, 2011, 1, 34),
    (35, N'WALLING : GLAZED WHITE TILES', 0.00, 400.00, 0.00, 0.00, 2011, 1, 35),
    (36, N'WALLING : GLAZED COLORED TILES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 36),
    (37, N'WALLING : FANCY TILES', 0.00, 200.00, 0.00, 0.00, 2011, 1, 37),
    (38, N'WALLING : SYNTHETIC RUBLE', 0.00, 100.00, 0.00, 0.00, 2011, 1, 38),
    (39, N'WALLING : BRICKS', 0.00, 100.00, 0.00, 0.00, 2011, 1, 39),
    (40, N'SPECIAL PANELS : GLASS WITH WOODEN FRAMES', 0.00, 400.00, 0.00, 0.00, 2011, 1, 40),
    (41, N'SPECIAL PANELS : GLASS WITH ALUMINUM FRAMES', 0.00, 600.00, 0.00, 0.00, 2011, 1, 41),
    (42, N'CEILINGS BELOW CONCRETE FLOOR : ORDINARY PLYWOOD', 0.00, 200.00, 0.00, 0.00, 2011, 1, 42),
    (43, N'CEILINGS BELOW CONCRETE FLOOR : LUMINOUS CEILING', 0.00, 500.00, 0.00, 0.00, 2011, 1, 43),
    (44, N'CEILINGS BELOW CONCRETE FLOOR : ACOUSTIC', 0.00, 400.00, 0.00, 0.00, 2011, 1, 44),
    (45, N'CEILINGS BELOW CONCRETE FLOOR : SPECIAL FINISH', 0.00, 300.00, 0.00, 0.00, 2011, 1, 45),
    (46, N'GATES, GRILLS AND WINDOW GRILLS : STEEL GATES W/GRILLS (SPECIAL DESIGNS)', 0.00, 1000.00, 0.00, 0.00, 2011, 1, 46),
    (47, N'GATES, GRILLS AND WINDOW GRILLS : STEEL GRILLS W/GRILLS (W/O DESIGNS ORDINARY)', 0.00, 600.00, 0.00, 0.00, 2011, 1, 47),
    (48, N'GATES, GRILLS AND WINDOW GRILLS : WINDOW GRILLS', 0.00, 400.00, 0.00, 0.00, 2011, 1, 48),
    (49, N'EXCESS HEIGHTS : RESIDENTIAL AND COMMERCIAL IN THE EXCESS OF 3 METERS', 10.00, 0.00, 0.00, 0.00, 2011, 1, 49),
    (50, N'EXCESS HEIGHTS : BODEGA AND FACTORY IN THE EXCESS OF 4.5 METERS', 10.00, 0.00, 0.00, 0.00, 2011, 1, 50),
    (51, N'EXTRA T % B - ORDINARY FINISH', 0.00, 10000.00, 0.00, 0.00, 2011, 1, 51),
    (52, N'PAINTING', 10.00, 0.00, 0.00, 0.00, 2011, 1, 52),
    (53, N'SECOND HAND MATERIALS', -10.00, 0.00, 0.00, 0.00, 2011, 1, 53)
    ) AS source (
        [Id], [AdditionalItemDescription], [PercentUnitValue], [CostPerSQM],
        [ExcessSQM], [ExcessPercentage], [RevisionYear], [IsActive], [SortOrder]
    )
    ON target.[Id] = source.[Id]
    WHEN MATCHED THEN
        UPDATE SET
            target.[AdditionalItemDescription] = source.[AdditionalItemDescription],
            target.[PercentUnitValue] = source.[PercentUnitValue],
            target.[CostPerSQM] = source.[CostPerSQM],
            target.[ExcessSQM] = source.[ExcessSQM],
            target.[ExcessPercentage] = source.[ExcessPercentage],
            target.[RevisionYear] = source.[RevisionYear],
            target.[IsActive] = source.[IsActive],
            target.[SortOrder] = source.[SortOrder]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [AdditionalItemDescription], [PercentUnitValue], [CostPerSQM], [ExcessSQM], [ExcessPercentage], [RevisionYear], [IsActive], [SortOrder])
        VALUES (source.[Id], source.[AdditionalItemDescription], source.[PercentUnitValue], source.[CostPerSQM], source.[ExcessSQM], source.[ExcessPercentage], source.[RevisionYear], source.[IsActive], source.[SortOrder]);

    SET IDENTITY_INSERT [dbo].[BuildingAdditionalItems] OFF;

    PRINT 'dbo.BuildingAdditionalItems seeding complete. Total reference items seeded: 52.';
END
ELSE
BEGIN
    PRINT 'Table dbo.BuildingAdditionalItems does not exist yet. Please run EF Core migration first.';
END
GO
