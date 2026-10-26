-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 03: FULL SAMPLE DATA SEEDING (SQL SERVER / T-SQL COMPATIBLE)
-- Seeds Staff Accounts, Fee Rates, Tax Declarations & Owners
-- =============================================================================

SET NOCOUNT ON;

-- -----------------------------------------------------------------------------
-- 1. SEED STAFF USER ACCOUNTS
-- Default Password for staff accounts: 'P@ssword2026!'
-- -----------------------------------------------------------------------------
DECLARE @StaffHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==';

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'admin.gensan')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000001', 'admin.gensan', @StaffHash, 'Engr. Roberto M. Dela Rosa', 'roberto.delarosa@gensan.gov.ph', 'EMP-2024-001', 0, 'City Assessor Executive Office', '0917-888-9001', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'assessor.head')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000002', 'assessor.head', @StaffHash, 'Atty. Clarissa V. Mendoza', 'clarissa.mendoza@gensan.gov.ph', 'EMP-2024-002', 1, 'City Assessor Executive Office', '0919-555-1122', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'officer.santos')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000003', 'officer.santos', @StaffHash, 'Marcus Aurelius Santos', 'marcus.santos@gensan.gov.ph', 'EMP-2025-014', 2, 'Real Property Assessment Division', '0920-777-3344', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'appraiser.reyes')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000004', 'appraiser.reyes', @StaffHash, 'Elena G. Reyes', 'elena.reyes@gensan.gov.ph', 'EMP-2025-028', 3, 'Property Valuation & Appraisal Div', '0918-444-5566', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'taxmap.valdez')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000005', 'taxmap.valdez', @StaffHash, 'Fernando K. Valdez', 'fernando.valdez@gensan.gov.ph', 'EMP-2025-035', 4, 'GIS & Tax Mapping Section', '0922-333-8899', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'clerk.villanueva')
    INSERT INTO [dbo].[UserAccounts] ([Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate])
    VALUES ('70000000-0000-0000-0000-000000000006', 'clerk.villanueva', @StaffHash, 'Patricia Ann Villanueva', 'patricia.villanueva@gensan.gov.ph', 'EMP-2026-050', 5, 'Taxpayer Services & Roll Encoding', '0917-222-6677', 1, GETDATE());

-- -----------------------------------------------------------------------------
-- 2. SEED ORDER OF PAYMENT FEE RATES
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-001')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000001', 'FEE-001', 'Certified True Copy of Tax Declaration (TDN)', 'Certifications', 150.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-002')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000002', 'FEE-002', 'Certificate of Aggregate Landholding', 'Certifications', 200.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-003')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000003', 'FEE-003', 'Certificate of No Real Property Improvements', 'Certifications', 150.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-004')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000004', 'FEE-004', 'Certificate of Property Identification & Verification', 'Tax Mapping', 300.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-005')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000005', 'FEE-005', 'Tax Map Copy / Parcel Layout Plan (A3 Print)', 'Tax Mapping', 500.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-006')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000006', 'FEE-006', 'Field Inspection Fee (Per Lot Parcel)', 'Inspection', 750.00, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [Code] = 'FEE-007')
    INSERT INTO [dbo].[OrderPayments] ([Id], [Code], [Particular], [Category], [UnitCost], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('80000000-0000-0000-0000-000000000007', 'FEE-007', 'Certificate of Road Right of Way (RROW)', 'Certifications', 250.00, 1, GETDATE(), GETDATE());

-- -----------------------------------------------------------------------------
-- 3. SEED TAXPAYER OWNERS
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TaxpayerOwners] WHERE [TaxIdentNo] = '123-456-789-000')
    INSERT INTO [dbo].[TaxpayerOwners] ([Id], [OwnerType], [FirstName], [MiddleName], [LastName], [CorporateName], [TaxIdentNo], [RptinNumber], [ContactNo], [Email], [ProvinceName], [CityMunicipalityName], [BarangayName], [Address], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('60000000-0000-0000-0000-000000000001', 0, 'Juan', 'Mercado', 'Dela Cruz', 'Dela Cruz Holdings', '123-456-789-000', 'RPTIN-2026-001', '0917-111-2222', 'juan.delacruz@gmail.com', 'South Cotabato', 'General Santos City', 'Brgy. Dadiangas East', 'Pioneer Avenue, General Santos City', 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaxpayerOwners] WHERE [TaxIdentNo] = '987-654-321-000')
    INSERT INTO [dbo].[TaxpayerOwners] ([Id], [OwnerType], [FirstName], [MiddleName], [LastName], [CorporateName], [TaxIdentNo], [RptinNumber], [ContactNo], [Email], [ProvinceName], [CityMunicipalityName], [BarangayName], [Address], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('60000000-0000-0000-0000-000000000002', 0, 'Maria Clara', 'Ocampo', 'Santos', 'Santos Realty & Dev Corp', '987-654-321-000', 'RPTIN-2026-002', '0919-222-3333', 'maria.santos@gmail.com', 'South Cotabato', 'General Santos City', 'Brgy. Dadiangas South', 'Santiago Boulevard, General Santos City', 1, GETDATE(), GETDATE());

-- -----------------------------------------------------------------------------
-- 4. SEED REGISTERED PROPERTY TAX DECLARATIONS
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TaxDeclarations] WHERE [TaxDeclarationNumber] = 'TD-2026-GS-0001')
BEGIN
    INSERT INTO [dbo].[TaxDeclarations] ([Id], [Kind], [TaxDeclarationNumber], [TransactionCode], [PropertyIndexNumber], [OwnerName], [OwnerTin], [OwnerPhone], [OwnerEmail], [OwnerAddress], [OwnerSex], [AdminPossessorName], [AdminPossessorSex], [AdminPossessorAddress], [AdminPossessorTin], [AdminPossessorPhone], [AdminPossessorEmail], [RegistrationStatus], [TitleType], [TitleNo], [DateRegistered], [SurveyNo], [LotNo], [BlockNo], [IsLocallyManufactured], [MachineryRegNo], [ImportationPermitNo], [ImportationDateIssued], [CountryOfOrigin], [SupplierSeller], [SupplierAddress], [AcquisitionCost], [OrNoAndDate], [BuildingUseType], [StreetNo], [Municipality], [DistrictOrBarangay], [ProvinceCity], [LandOwnerName], [LandOwnerTin], [LandOwnerPhone], [LandOwnerEmail], [LandOwnerAddress], [LandOwnerSex], [LandTdNo], [LandPin], [LandTitleNo], [LandSurveyNo], [LandLotNo], [LandBlockNo], [LandAreaSqm], [BuildingReferenceName], [BuildingOwnerTin], [BuildingOwnerPhone], [BuildingOwnerEmail], [BuildingOwnerAddress], [BuildingOwnerSex], [BuildingTdNo], [BuildingPin], [BoundaryNorth], [BoundaryEast], [BoundaryWest], [BoundarySouth], [StructuralType], [NoOfStoreys], [BuildingClassification], [BuildingPermitNo], [BuildingAgeYears], [DateCompleted], [OccupancyPermitNo], [DateOccupied], [FloorLevel], [OtherFloorsSubtotal], [TotalBuildingAreaSqm], [StructuralFoundation], [StructuralColumns], [StructuralBeams], [StructuralTrussRoof], [StructuralRoof], [StructuralExteriorWalls], [StructuralFinishFront], [StructuralFinishRear], [StructuralFinishLeft], [StructuralFinishRight], [StructuralInsideFinish], [StructuralPartitionWalls], [StructuralFlooring], [StructuralFloorFinish], [StructuralStairs], [StructuralWindows], [DoorMain], [DoorRoom], [DoorOthers], [ElectricalSystem], [ToiletAndBathroom], [PlumbingSewer], [Paintings], [Ceiling], [MaterialsOrCode], [DescriptionBrand], [RatedCapacity], [ConditionWhenAcquired], [DateAcquired], [EconomicLifeYears], [YearInstalled], [YearOfOperation], [OriginalCost], [InstallationCost], [TotalCost], [YearsUsed], [RemainingEconomicLife], [DepreciationRatePercent], [DepreciatedValue], [Classification], [SubClassification], [AreaSqMeters], [UnitValue], [MarketValue], [ImprovementType], [ImprovementQty], [ImprovementUnitOfMeasure], [ImprovementUnitValue], [ImprovementMarketValue], [AdjustmentFactor], [PercentAdjustment], [ValueAdjustment], [AdjustedMarketValue], [AssessmentLevelPercent], [ActualUse], [BasicTaxRate], [SefTaxRate], [EffectivityYear], [EffectivityQuarter], [Taxability], [TaxabilityBasis], [TaxPeriodFromYear], [TaxPeriodToYear], [PreviousTdNo], [PreviousMarketValue], [PreviousOwner], [PreviousPin], [PreviousAssessedValue], [PreviousEffectivity], [AppraisedByName], [DateAppraised], [AssessedByName], [DateAssessed], [RecommendingApprovalName], [DateRecommended], [Memoranda], [ApprovedByName], [DateApproved], [EntryInRollName], [EntryInRollPosition], [EntryInRollDate], [EncodedByName], [DateEncoded], [Status])
    VALUES ('90000000-0000-0000-0000-000000000001', 0, N'TD-2026-GS-0001', N'TR-2026-NEW', N'126-00-0001-002-01', N'Engr. Juan Dela Cruz', N'123-456-789-000', N'0917-111-2222', N'juan.delacruz@gmail.com', N'Pioneer Avenue, General Santos City', N'Male', NULL, NULL, NULL, NULL, NULL, NULL, N'Titled', N'TCT', N'TCT-142-99881', NULL, N'Psu-12-00411', N'Lot 4', N'Block 12', 0, N'', N'', NULL, N'', N'', N'', 0.0000, N'', N'Residential Condominium', N'Pioneer Avenue', N'General Santos City', N'Brgy. Dadiangas East', N'South Cotabato', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0.0000, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'Lot 101', N'Road Lot', N'Creek', N'Lot 103', N'Type I', 1, N'Class A - Reinforced Concrete', N'', 0, NULL, N'', NULL, N'Ground Floor', 0.0000, 0.0000, N'Reinforced Concrete Footing', N'Reinforced Concrete', N'Reinforced Concrete', N'Steel Truss', N'Long Span Colorbond G.I. Sheets', N'Concrete Hollow Blocks', N'Cement Plaster Paint Finish', N'Cement Plaster Paint Finish', N'Cement Plaster Paint Finish', N'Cement Plaster Paint Finish', N'Painted Plaster', N'Drywall / CHB', N'Concrete Slab', N'Granite Tiles', N'Reinforced Concrete', N'Sliding Glass Windows', N'Solid Wood Panel', N'Flush Door', N'PVC Door', N'Concealed Conduit Wiring', N'Complete Fixtures & Tiled', N'PVC Piping to Septic Vault', N'Acrytex Paint', N'Gypsum Board on Metal Framing', N'MAT-2026-A1', N'', N'', N'New', NULL, 10, 2026, 2026, 0.0000, 0.0000, 0.0000, 0, 10, 0.0000, 0.0000, 0, N'First Class Residential', 450.0000, 5000.0000, 2250000.0000, N'Fence / Pavement', 0.0000, N'sq.m.', 0.0000, 0.0000, N'Location & Elevation', 0.0000, 0.0000, 2250000.0000, 20.0000, N'Residential Lot', 2.0000, 1.0000, 2026, N'1st Quarter', N'Taxable', N'RA 7160 Local Government Code', 2026, 2029, NULL, 0.0000, NULL, NULL, 0.0000, NULL, N'Elena G. Reyes (Property Appraiser)', '2026-10-24 14:32:35', N'Marcus Aurelius Santos (Assessment Officer)', '2026-10-24 14:32:35', N'Engr. Roberto M. Dela Rosa (Assistant Assessor)', '2026-10-24 14:32:35', N'Standard assessment roll entry', N'Atty. Clarissa V. Mendoza (City Assessor)', '2026-10-24 14:32:35', N'Patricia Ann Villanueva', N'Assessment Roll Clerk', '2026-10-24 14:32:35', N'Patricia Ann Villanueva', '2026-10-24 14:32:35', 0);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaxDeclarations] WHERE [TaxDeclarationNumber] = 'TD-2026-GS-0002')
BEGIN
    INSERT INTO [dbo].[TaxDeclarations] ([Id], [Kind], [TaxDeclarationNumber], [TransactionCode], [PropertyIndexNumber], [OwnerName], [OwnerTin], [OwnerPhone], [OwnerEmail], [OwnerAddress], [OwnerSex], [AdminPossessorName], [AdminPossessorSex], [AdminPossessorAddress], [AdminPossessorTin], [AdminPossessorPhone], [AdminPossessorEmail], [RegistrationStatus], [TitleType], [TitleNo], [DateRegistered], [SurveyNo], [LotNo], [BlockNo], [IsLocallyManufactured], [MachineryRegNo], [ImportationPermitNo], [ImportationDateIssued], [CountryOfOrigin], [SupplierSeller], [SupplierAddress], [AcquisitionCost], [OrNoAndDate], [BuildingUseType], [StreetNo], [Municipality], [DistrictOrBarangay], [ProvinceCity], [LandOwnerName], [LandOwnerTin], [LandOwnerPhone], [LandOwnerEmail], [LandOwnerAddress], [LandOwnerSex], [LandTdNo], [LandPin], [LandTitleNo], [LandSurveyNo], [LandLotNo], [LandBlockNo], [LandAreaSqm], [BuildingReferenceName], [BuildingOwnerTin], [BuildingOwnerPhone], [BuildingOwnerEmail], [BuildingOwnerAddress], [BuildingOwnerSex], [BuildingTdNo], [BuildingPin], [BoundaryNorth], [BoundaryEast], [BoundaryWest], [BoundarySouth], [StructuralType], [NoOfStoreys], [BuildingClassification], [BuildingPermitNo], [BuildingAgeYears], [DateCompleted], [OccupancyPermitNo], [DateOccupied], [FloorLevel], [OtherFloorsSubtotal], [TotalBuildingAreaSqm], [StructuralFoundation], [StructuralColumns], [StructuralBeams], [StructuralTrussRoof], [StructuralRoof], [StructuralExteriorWalls], [StructuralFinishFront], [StructuralFinishRear], [StructuralFinishLeft], [StructuralFinishRight], [StructuralInsideFinish], [StructuralPartitionWalls], [StructuralFlooring], [StructuralFloorFinish], [StructuralStairs], [StructuralWindows], [DoorMain], [DoorRoom], [DoorOthers], [ElectricalSystem], [ToiletAndBathroom], [PlumbingSewer], [Paintings], [Ceiling], [MaterialsOrCode], [DescriptionBrand], [RatedCapacity], [ConditionWhenAcquired], [DateAcquired], [EconomicLifeYears], [YearInstalled], [YearOfOperation], [OriginalCost], [InstallationCost], [TotalCost], [YearsUsed], [RemainingEconomicLife], [DepreciationRatePercent], [DepreciatedValue], [Classification], [SubClassification], [AreaSqMeters], [UnitValue], [MarketValue], [ImprovementType], [ImprovementQty], [ImprovementUnitOfMeasure], [ImprovementUnitValue], [ImprovementMarketValue], [AdjustmentFactor], [PercentAdjustment], [ValueAdjustment], [AdjustedMarketValue], [AssessmentLevelPercent], [ActualUse], [BasicTaxRate], [SefTaxRate], [EffectivityYear], [EffectivityQuarter], [Taxability], [TaxabilityBasis], [TaxPeriodFromYear], [TaxPeriodToYear], [PreviousTdNo], [PreviousMarketValue], [PreviousOwner], [PreviousPin], [PreviousAssessedValue], [PreviousEffectivity], [AppraisedByName], [DateAppraised], [AssessedByName], [DateAssessed], [RecommendingApprovalName], [DateRecommended], [Memoranda], [ApprovedByName], [DateApproved], [EntryInRollName], [EntryInRollPosition], [EntryInRollDate], [EncodedByName], [DateEncoded], [Status])
    VALUES ('90000000-0000-0000-0000-000000000002', 1, N'TD-2026-GS-0002', N'TR-2026-NEW', N'126-00-0002-005-08', N'Maria Clara O. Santos', N'987-654-321-000', N'0919-222-3333', N'maria.santos@gmail.com', N'Santiago Boulevard, General Santos City', N'Female', NULL, NULL, NULL, NULL, NULL, NULL, N'Titled', N'TCT', N'TCT-142-55412', NULL, N'Psd-12-00812', N'Lot 8', N'Block 3', 0, N'', N'', NULL, N'', N'', N'', 0.0000, N'', N'Residential Condominium', N'Santiago Boulevard', N'General Santos City', N'Brgy. Dadiangas South', N'South Cotabato', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0.0000, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'Lot 101', N'Road Lot', N'Creek', N'Lot 103', N'Type I', 1, N'Class A - Reinforced Concrete', N'', 0, NULL, N'', NULL, N'Ground Floor', 0.0000, 0.0000, N'Reinforced Concrete Footing', N'Reinforced Concrete', N'Reinforced Concrete', N'Steel Truss', N'Long Span Colorbond G.I. Sheets', N'Concrete Hollow Blocks', N'Cement Plaster Paint Finish', N'Cement Plaster Paint Finish', N'Cement Plaster Paint Finish', N'Cement Plaster Paint Finish', N'Painted Plaster', N'Drywall / CHB', N'Concrete Slab', N'Granite Tiles', N'Reinforced Concrete', N'Sliding Glass Windows', N'Solid Wood Panel', N'Flush Door', N'PVC Door', N'Concealed Conduit Wiring', N'Complete Fixtures & Tiled', N'PVC Piping to Septic Vault', N'Acrytex Paint', N'Gypsum Board on Metal Framing', N'MAT-2026-A1', N'', N'', N'New', NULL, 10, 2026, 2026, 0.0000, 0.0000, 0.0000, 0, 10, 0.0000, 0.0000, 1, N'Commercial Building', 620.0000, 13709.6800, 8500000.0000, N'Fence / Pavement', 0.0000, N'sq.m.', 0.0000, 0.0000, N'Location & Elevation', 0.0000, 0.0000, 8500000.0000, 50.0000, N'Commercial Building', 2.0000, 1.0000, 2026, N'1st Quarter', N'Taxable', N'RA 7160 Local Government Code', 2026, 2029, NULL, 0.0000, NULL, NULL, 0.0000, NULL, N'Elena G. Reyes (Property Appraiser)', '2026-10-24 14:32:35', N'Marcus Aurelius Santos (Assessment Officer)', '2026-10-24 14:32:35', N'Engr. Roberto M. Dela Rosa (Assistant Assessor)', '2026-10-24 14:32:35', N'Commercial building assessment entry', N'Atty. Clarissa V. Mendoza (City Assessor)', '2026-10-24 14:32:35', N'Patricia Ann Villanueva', N'Assessment Roll Clerk', '2026-10-24 14:32:35', N'Patricia Ann Villanueva', '2026-10-24 14:32:35', 0);
END

-- -----------------------------------------------------------------------------
-- VERIFICATION SUMMARY
-- -----------------------------------------------------------------------------
SELECT 'UserAccounts' AS [Table], COUNT(*) AS [Count] FROM [dbo].[UserAccounts]
UNION ALL
SELECT 'OrderPayments' AS [Table], COUNT(*) AS [Count] FROM [dbo].[OrderPayments]
UNION ALL
SELECT 'TaxpayerOwners' AS [Table], COUNT(*) AS [Count] FROM [dbo].[TaxpayerOwners]
UNION ALL
SELECT 'TaxDeclarations' AS [Table], COUNT(*) AS [Count] FROM [dbo].[TaxDeclarations];
