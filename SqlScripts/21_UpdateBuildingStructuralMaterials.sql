-- ==========================================================================================
-- Script: 21_UpdateBuildingStructuralMaterials.sql
-- Description: Updates [dbo].[TDNBuildingStructuralMaterials] based on Local Assessment Manual
--              "Table 3. List of Structural Materials Codes" (16 Structural Parts):
--              1. Foundation          (RC, PC) - removed Stone / Rubble Footing
--              2. Columns             (S, RC, W)
--              3. Beams               (S, W, RC)
--              4. Roof Framing        (S, W, RC)
--              5. Roofing             (CGI, SPC, As, CT, CS, Al, LGI, N, An, C, PR)
--              6. Exterior Walls      (PC, ALP, W, GI, CHB, RC, BW, B, Sa)
--              7. Flooring            (PC, W, M, T, T&G, RC)
--              8. Doors               (S, Al, G, Gr, PVC, WP, FD, FO) - separated Aluminum & Glass
--              9. Ceiling             (Pw, L, As, D, SR, Hx, AF, Sw, B)
--              10. Windows            (SC, SCG, AC, ACG, WC, WCG, G)
--              11. Stairs             (S, RC, W, Sn)
--              12. Partition          (RC, CHB, GI, Pw, Pb, W, Hx [Hardiflex / Dry wall], AIF)
--              13. Wall Finish        (CP, WP, T, WOP, AR) - Floor Finish removed
--              14. Electrical Conduit (EPVC, MM, WM) - Concealed Knob & Tube removed
--              15. Toilet and Bath    (CT, M, PC)
--              16. Plumbing and Services (SP, GIP, PVCP [Poly Vinyl Carbonate Pipe], CP, AP)
-- Database: FairTrackDB / MS SQL Server
-- ==========================================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Remove deprecated 'Floor Finish' rows and re-align all existing TDNBuildings
    --    to the official 16 Structural Parts from Table 3 (Local Assessment Manual)
    DELETE FROM [dbo].[TDNBuildingStructuralMaterials];

    -- 2. Insert the 16 official Structural Materials rows for each existing TDNBuilding
    INSERT INTO [dbo].[TDNBuildingStructuralMaterials] (
        [Id],
        [TDNBuildingId],
        [PartNumber],
        [PartName],
        [MaterialSelected],
        [SpecificDescription]
    )
    SELECT
        NEWID(),
        b.[Id],
        m.[PartNumber],
        m.[PartName],
        m.[MaterialSelected],
        m.[Code]
    FROM [dbo].[TDNBuildings] b
    CROSS JOIN (
        VALUES
            (1,  N'1. Foundation',             N'Reinforced Concrete (RC)',               N'RC'),
            (2,  N'2. Columns',                N'Reinforced Concrete (RC)',               N'RC'),
            (3,  N'3. Beams',                  N'Reinforced Concrete (RC)',               N'RC'),
            (4,  N'4. Roof Framing',           N'Steel (S)',                              N'S'),
            (5,  N'5. Roofing',                N'Long Span G.I. Sheet-Pre painted (LGI)', N'LGI'),
            (6,  N'6. Exterior Walls',         N'Concrete Hollow Block (CHB)',            N'CHB'),
            (7,  N'7. Flooring',               N'Reinforced Concrete (RC)',               N'RC'),
            (8,  N'8. Doors',                  N'Wood Panel (WP)',                        N'WP'),
            (9,  N'9. Ceiling',                N'Hardiflex (Hx)',                         N'Hx'),
            (10, N'10. Windows',               N'Aluminum Casement (AC)',                 N'AC'),
            (11, N'11. Stairs',                N'Reinforced concrete (RC)',               N'RC'),
            (12, N'12. Partition',             N'Hardiflex / Dry wall (Hx)',              N'Hx'),
            (13, N'13. Wall Finish',           N'Cement Plaster (CP)',                    N'CP'),
            (14, N'14. Electrical Conduit',    N'Poly Vinyl Carbonate Conduit (EPVC)',    N'EPVC'),
            (15, N'15. Toilet and Bath',       N'Cement Tiles (CT)',                      N'CT'),
            (16, N'16. Plumbing and Services', N'Poly Vinyl Carbonate Pipe (PVCP)',       N'PVCP')
    ) AS m([PartNumber], [PartName], [MaterialSelected], [Code]);

    COMMIT TRANSACTION;
    PRINT 'TDNBuildingStructuralMaterials updated successfully with Table 3 Structural Materials Codes (16 parts per building).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
