using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeTaxDeclarationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TDNBuildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StreetNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Municipality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistrictOrBarangay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProvinceCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandOwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandOwnerTin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandOwnerPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandOwnerEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandOwnerAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandOwnerSex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandTdNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandTitleNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandSurveyNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandLotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandBlockNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandAreaSqm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsCct = table.Column<bool>(type: "bit", nullable: false),
                    CctTitleNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CctDateRegistered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnerNameReflectedInCct = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAdministrator = table.Column<bool>(type: "bit", nullable: false),
                    AdministratorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministratorAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministratorContactNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministratorTin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministratorGender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministratorCivilStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministratorEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StructuralType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingUseType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherBuildingUseType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoOfStoreys = table.Column<int>(type: "int", nullable: false),
                    FloorLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingPermitNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingAgeYears = table.Column<int>(type: "int", nullable: false),
                    DateConstructed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OccupancyPermitNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOccupied = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalFloorArea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseRcnValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReplacementCostNew = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BuildingDepreciationPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BuildingDepreciationAccumulated = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepreciatedMarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAdditionalItemsValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssessmentLevelPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AssessedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BasicTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SefTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AnnualTaxDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectivityYear = table.Column<int>(type: "int", nullable: false),
                    EffectivityQuarter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Taxability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessmentBasis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackTaxesYearFrom = table.Column<int>(type: "int", nullable: true),
                    BackTaxesYearTo = table.Column<int>(type: "int", nullable: true),
                    FloorBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNBuildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNBuildings_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TDNLands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateRegistered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SurveyNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StreetNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Municipality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistrictOrBarangay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProvinceCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoundaryNorth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoundaryEast = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoundaryWest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoundarySouth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    SubClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActualUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreaSqMeters = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImprovementType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImprovementQty = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImprovementUnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImprovementUnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImprovementMarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentFactor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PercentAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValueAdjustment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustedMarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssessmentLevelPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AssessedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BasicTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SefTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AnnualTaxDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectivityYear = table.Column<int>(type: "int", nullable: false),
                    EffectivityQuarter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Taxability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxabilityBasis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackTaxesYearFrom = table.Column<int>(type: "int", nullable: true),
                    BackTaxesYearTo = table.Column<int>(type: "int", nullable: true),
                    PreviousTdNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousOwner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousMarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousAssessedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNLands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNLands_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TDNMachineries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsLocallyManufactured = table.Column<bool>(type: "bit", nullable: false),
                    MachineryRegNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImportationPermitNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImportationDateIssued = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierSeller = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcquisitionCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OrNoAndDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionBrand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RatedCapacity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConditionWhenAcquired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAcquired = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EconomicLifeYears = table.Column<int>(type: "int", nullable: false),
                    YearInstalled = table.Column<int>(type: "int", nullable: false),
                    YearOfOperation = table.Column<int>(type: "int", nullable: false),
                    OriginalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallationCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YearsUsed = table.Column<int>(type: "int", nullable: false),
                    RemainingEconomicLife = table.Column<int>(type: "int", nullable: false),
                    DepreciationRatePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DepreciatedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StreetNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Municipality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistrictOrBarangay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProvinceCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandOwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandTdNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingReferenceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerTin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerSex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingTdNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    ActualUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessmentLevelPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AssessedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BasicTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SefTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AnnualTaxDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectivityYear = table.Column<int>(type: "int", nullable: false),
                    EffectivityQuarter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Taxability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxabilityBasis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackTaxesYearFrom = table.Column<int>(type: "int", nullable: true),
                    BackTaxesYearTo = table.Column<int>(type: "int", nullable: true),
                    PreviousTdNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousOwner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousMarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousAssessedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNMachineries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNMachineries_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TDNBuildingAdditionalItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TDNBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MultiplierPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false),
                    ForAssessment = table.Column<bool>(type: "bit", nullable: false),
                    FloorNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNBuildingAdditionalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNBuildingAdditionalItems_TDNBuildings_TDNBuildingId",
                        column: x => x.TDNBuildingId,
                        principalTable: "TDNBuildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TDNBuildingFloors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TDNBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorOrder = table.Column<int>(type: "int", nullable: false),
                    FloorNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    FloorAreaSqm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseUnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNBuildingFloors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNBuildingFloors_TDNBuildings_TDNBuildingId",
                        column: x => x.TDNBuildingId,
                        principalTable: "TDNBuildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TDNBuildingStructuralMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TDNBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<int>(type: "int", nullable: false),
                    PartName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MaterialSelected = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SpecificDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNBuildingStructuralMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNBuildingStructuralMaterials_TDNBuildings_TDNBuildingId",
                        column: x => x.TDNBuildingId,
                        principalTable: "TDNBuildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TDNBuildingAdditionalItems_TDNBuildingId",
                table: "TDNBuildingAdditionalItems",
                column: "TDNBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_TDNBuildingFloors_TDNBuildingId",
                table: "TDNBuildingFloors",
                column: "TDNBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_TDNBuildings_TaxDeclarationId",
                table: "TDNBuildings",
                column: "TaxDeclarationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TDNBuildingStructuralMaterials_TDNBuildingId",
                table: "TDNBuildingStructuralMaterials",
                column: "TDNBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_TDNLands_TaxDeclarationId",
                table: "TDNLands",
                column: "TaxDeclarationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TDNMachineries_TaxDeclarationId",
                table: "TDNMachineries",
                column: "TaxDeclarationId",
                unique: true);
            // Data migration: Copy existing TaxDeclarations records to normalized tables
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxDeclarations' AND COLUMN_NAME = 'TotalBuildingAreaSqm')
                BEGIN
                    INSERT INTO TDNBuildings (
                        Id, TaxDeclarationId, StreetNo, Municipality, DistrictOrBarangay, ProvinceCity,
                        LandOwnerName, LandOwnerTin, LandOwnerPhone, LandOwnerEmail, LandOwnerAddress, LandOwnerSex,
                        LandTdNo, LandPin, LandTitleNo, LandSurveyNo, LandLotNo, LandBlockNo, LandAreaSqm,
                        IsCct, CctTitleNo, CctDateRegistered, OwnerNameReflectedInCct, IsAdministrator,
                        AdministratorName, AdministratorAddress, AdministratorContactNo, AdministratorTin,
                        AdministratorGender, AdministratorCivilStatus, AdministratorEmail,
                        StructuralType, BuildingClassification, BuildingUseType, OtherBuildingUseType,
                        NoOfStoreys, FloorLevel, BuildingPermitNo, BuildingAgeYears, DateConstructed,
                        DateCompleted, OccupancyPermitNo, DateOccupied, TotalFloorArea, BaseRcnValue,
                        ReplacementCostNew, BuildingDepreciationPercent, BuildingDepreciationAccumulated,
                        DepreciatedMarketValue, TotalAdditionalItemsValue, MarketValue, AssessmentLevelPercent,
                        AssessedValue, BasicTaxRate, SefTaxRate, AnnualTaxDue, EffectivityYear,
                        EffectivityQuarter, Taxability, AssessmentBasis, BackTaxesYearFrom, BackTaxesYearTo,
                        FloorBreakdownJson, AdditionalItemsJson
                    )
                    SELECT 
                        NEWID(), Id, StreetNo, Municipality, DistrictOrBarangay, ProvinceCity,
                        LandOwnerName, LandOwnerTin, LandOwnerPhone, LandOwnerEmail, LandOwnerAddress, LandOwnerSex,
                        LandTdNo, LandPin, LandTitleNo, LandSurveyNo, LandLotNo, LandBlockNo, LandAreaSqm,
                        IsCct, CctTitleNo, CctDateRegistered, OwnerNameReflectedInCct, IsAdministrator,
                        AdministratorName, AdministratorAddress, AdministratorContactNo, AdministratorTin,
                        AdministratorGender, AdministratorCivilStatus, AdministratorEmail,
                        StructuralType, BuildingClassification, BuildingUseType, NULL,
                        NoOfStoreys, FloorLevel, BuildingPermitNo, BuildingAgeYears, DateConstructed,
                        DateCompleted, OccupancyPermitNo, DateOccupied, TotalBuildingAreaSqm, BaseRcnValue,
                        0, BuildingDepreciationPercent, BuildingDepreciationAccumulated,
                        BuildingDepreciatedMarketValue, BuildingTotalAdditionsValue, MarketValue, AssessmentLevelPercent,
                        0, BasicTaxRate, SefTaxRate, 0, EffectivityYear,
                        EffectivityQuarter, Taxability, AssessmentBasis, BackTaxesYearFrom, BackTaxesYearTo,
                        BuildingFloorBreakdownJson, BuildingAdditionalItemsJson
                    FROM TaxDeclarations
                    WHERE Kind = 1;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxDeclarations' AND COLUMN_NAME = 'BoundaryNorth')
                BEGIN
                    INSERT INTO TDNLands (
                        Id, TaxDeclarationId, RegistrationStatus, TitleType, TitleNo, DateRegistered,
                        SurveyNo, LotNo, BlockNo, StreetNo, Municipality, DistrictOrBarangay, ProvinceCity,
                        BoundaryNorth, BoundaryEast, BoundaryWest, BoundarySouth, Classification,
                        SubClassification, ActualUse, AreaSqMeters, UnitValue, MarketValue, ImprovementType,
                        ImprovementQty, ImprovementUnitOfMeasure, ImprovementUnitValue, ImprovementMarketValue,
                        AdjustmentFactor, PercentAdjustment, ValueAdjustment, AdjustedMarketValue,
                        AssessmentLevelPercent, AssessedValue, BasicTaxRate, SefTaxRate, AnnualTaxDue,
                        EffectivityYear, EffectivityQuarter, Taxability, TaxabilityBasis, BackTaxesYearFrom,
                        BackTaxesYearTo, PreviousTdNo, PreviousPin, PreviousOwner, PreviousMarketValue,
                        PreviousAssessedValue
                    )
                    SELECT
                        NEWID(), Id, RegistrationStatus, TitleType, TitleNo, DateRegistered,
                        SurveyNo, LotNo, BlockNo, StreetNo, Municipality, DistrictOrBarangay, ProvinceCity,
                        BoundaryNorth, BoundaryEast, BoundaryWest, BoundarySouth, Classification,
                        SubClassification, ActualUse, AreaSqMeters, UnitValue, MarketValue, ImprovementType,
                        ImprovementQty, ImprovementUnitOfMeasure, ImprovementUnitValue, ImprovementMarketValue,
                        AdjustmentFactor, PercentAdjustment, ValueAdjustment, AdjustedMarketValue,
                        AssessmentLevelPercent, 0, BasicTaxRate, SefTaxRate, 0,
                        EffectivityYear, EffectivityQuarter, Taxability, TaxabilityBasis, BackTaxesYearFrom,
                        BackTaxesYearTo, PreviousTdNo, PreviousPin, PreviousOwner, PreviousMarketValue,
                        PreviousAssessedValue
                    FROM TaxDeclarations
                    WHERE Kind = 0;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxDeclarations' AND COLUMN_NAME = 'MachineryRegNo')
                BEGIN
                    INSERT INTO TDNMachineries (
                        Id, TaxDeclarationId, IsLocallyManufactured, MachineryRegNo, ImportationPermitNo,
                        ImportationDateIssued, CountryOfOrigin, SupplierSeller, SupplierAddress,
                        AcquisitionCost, OrNoAndDate, DescriptionBrand, RatedCapacity, ConditionWhenAcquired,
                        DateAcquired, EconomicLifeYears, YearInstalled, YearOfOperation, OriginalCost,
                        InstallationCost, TotalCost, YearsUsed, RemainingEconomicLife, DepreciationRatePercent,
                        DepreciatedValue, StreetNo, Municipality, DistrictOrBarangay, ProvinceCity,
                        LandOwnerName, LandTdNo, LandPin, BuildingReferenceName, BuildingOwnerTin,
                        BuildingOwnerPhone, BuildingOwnerAddress, BuildingOwnerSex, BuildingTdNo,
                        BuildingPin, Classification, ActualUse, AssessmentLevelPercent, AssessedValue,
                        BasicTaxRate, SefTaxRate, AnnualTaxDue, EffectivityYear, EffectivityQuarter,
                        Taxability, TaxabilityBasis, BackTaxesYearFrom, BackTaxesYearTo, PreviousTdNo,
                        PreviousPin, PreviousOwner, PreviousMarketValue, PreviousAssessedValue
                    )
                    SELECT
                        NEWID(), Id, IsLocallyManufactured, MachineryRegNo, ImportationPermitNo,
                        ImportationDateIssued, CountryOfOrigin, SupplierSeller, SupplierAddress,
                        AcquisitionCost, OrNoAndDate, DescriptionBrand, RatedCapacity, ConditionWhenAcquired,
                        DateAcquired, EconomicLifeYears, YearInstalled, YearOfOperation, OriginalCost,
                        InstallationCost, TotalCost, YearsUsed, RemainingEconomicLife, DepreciationRatePercent,
                        DepreciatedValue, StreetNo, Municipality, DistrictOrBarangay, ProvinceCity,
                        LandOwnerName, LandTdNo, LandPin, BuildingReferenceName, BuildingOwnerTin,
                        BuildingOwnerPhone, BuildingOwnerAddress, BuildingOwnerSex, BuildingTdNo,
                        BuildingPin, Classification, ActualUse, AssessmentLevelPercent, 0,
                        BasicTaxRate, SefTaxRate, 0, EffectivityYear, EffectivityQuarter,
                        Taxability, TaxabilityBasis, BackTaxesYearFrom, BackTaxesYearTo, PreviousTdNo,
                        PreviousPin, PreviousOwner, PreviousMarketValue, PreviousAssessedValue
                    FROM TaxDeclarations
                    WHERE Kind = 2;
                END
            ");
            migrationBuilder.RenameColumn(
                name: "BuildingFloorPlanSketchPath",
                table: "TaxDeclarations",
                newName: "FloorPlanSketchPath");
            migrationBuilder.DropColumn(
                name: "AcquisitionCost",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ActualUse",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdjustedMarketValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdjustmentFactor",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorAddress",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorCivilStatus",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorContactNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorEmail",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorGender",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorName",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorTin",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AreaSqMeters",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AssessmentBasis",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AssessmentLevelPercent",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BackTaxesYearFrom",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BackTaxesYearTo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BaseRcnValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BasicTaxRate",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BlockNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BoundaryEast",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BoundaryNorth",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BoundarySouth",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BoundaryWest",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingAdditionalItemsJson",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingAgeYears",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingClassification",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingDepreciatedMarketValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingDepreciationAccumulated",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingDepreciationPercent",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingFloorBreakdownJson",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PreviousTdNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingOwnerAddress",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingOwnerEmail",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingOwnerPhone",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingOwnerSex",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingOwnerTin",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingPermitNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingPin",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingReferenceName",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingTdNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingTotalAdditionsValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingUseType",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "CctDateRegistered",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "CctTitleNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "Ceiling",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ConditionWhenAcquired",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "CountryOfOrigin",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DateAcquired",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DateCompleted",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DateConstructed",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DateOccupied",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DateRegistered",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DepreciatedValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DepreciationRatePercent",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DescriptionBrand",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DistrictOrBarangay",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DoorMain",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DoorOthers",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DoorRoom",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "EconomicLifeYears",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "EffectivityQuarter",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "EffectivityYear",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ElectricalSystem",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "FloorLevel",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ImportationDateIssued",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ImportationPermitNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ImprovementMarketValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ImprovementQty",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ImprovementType",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ImprovementUnitOfMeasure",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ImprovementUnitValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "InstallationCost",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "IsAdministrator",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "IsCct",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "IsLocallyManufactured",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandAreaSqm",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandBlockNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandLotNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandOwnerAddress",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandOwnerEmail",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandOwnerName",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandOwnerPhone",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandOwnerSex",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandOwnerTin",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandPin",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandSurveyNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandTdNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LandTitleNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LotNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "MachineryRegNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "MarketValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "MaterialsOrCode",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "Municipality",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "NoOfStoreys",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "OccupancyPermitNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "OrNoAndDate",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "OriginalCost",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "OtherFloorsSubtotal",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "OwnerNameReflectedInCct",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "Paintings",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PercentAdjustment",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PlumbingSewer",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PreviousAssessedValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PreviousEffectivity",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PreviousMarketValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PreviousOwner",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PreviousPin",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ProvinceCity",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "RatedCapacity",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "RegistrationStatus",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "RemainingEconomicLife",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "SefTaxRate",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StreetNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralBeams",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralColumns",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralExteriorWalls",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralFinishFront",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralFinishLeft",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralFinishRear",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralFinishRight",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralFloorFinish",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralFlooring",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralFoundation",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralInsideFinish",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralPartitionWalls",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralRoof",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralStairs",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralTrussRoof",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralType",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "StructuralWindows",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "SubClassification",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "SupplierAddress",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "SupplierSeller",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "SurveyNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "TaxPeriodFromYear",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "TaxPeriodToYear",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "Taxability",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "TaxabilityBasis",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "TitleNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "TitleType",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ToiletAndBathroom",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "TotalBuildingAreaSqm",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "UnitValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ValueAdjustment",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "YearInstalled",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "YearOfOperation",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "YearsUsed",
                table: "TaxDeclarations");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TDNBuildingAdditionalItems");

            migrationBuilder.DropTable(
                name: "TDNBuildingFloors");

            migrationBuilder.DropTable(
                name: "TDNBuildingStructuralMaterials");

            migrationBuilder.DropTable(
                name: "TDNLands");

            migrationBuilder.DropTable(
                name: "TDNMachineries");

            migrationBuilder.DropTable(
                name: "TDNBuildings");

            migrationBuilder.RenameColumn(
                name: "FloorPlanSketchPath",
                table: "TaxDeclarations",
                newName: "PreviousTdNo");

            migrationBuilder.AddColumn<decimal>(
                name: "AcquisitionCost",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ActualUse",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustedMarketValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentFactor",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdministratorAddress",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorCivilStatus",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorContactNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorEmail",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorGender",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorName",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorTin",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AreaSqMeters",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentBasis",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AssessmentLevelPercent",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BackTaxesYearFrom",
                table: "TaxDeclarations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BackTaxesYearTo",
                table: "TaxDeclarations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseRcnValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BasicTaxRate",
                table: "TaxDeclarations",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BlockNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BoundaryEast",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BoundaryNorth",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BoundarySouth",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BoundaryWest",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BuildingAdditionalItemsJson",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuildingAgeYears",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BuildingClassification",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingDepreciatedMarketValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingDepreciationAccumulated",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingDepreciationPercent",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BuildingFloorBreakdownJson",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingFloorPlanSketchPath",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingOwnerAddress",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingOwnerEmail",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingOwnerPhone",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingOwnerSex",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingOwnerTin",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingPermitNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BuildingPin",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingReferenceName",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingTdNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingTotalAdditionsValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BuildingUseType",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CctDateRegistered",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CctTitleNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ceiling",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConditionWhenAcquired",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryOfOrigin",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateAcquired",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCompleted",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateConstructed",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOccupied",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateRegistered",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepreciatedValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepreciationRatePercent",
                table: "TaxDeclarations",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionBrand",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DistrictOrBarangay",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DoorMain",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DoorOthers",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DoorRoom",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EconomicLifeYears",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EffectivityQuarter",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EffectivityYear",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ElectricalSystem",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FloorLevel",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportationDateIssued",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportationPermitNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ImprovementMarketValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ImprovementQty",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ImprovementType",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImprovementUnitOfMeasure",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ImprovementUnitValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InstallationCost",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrator",
                table: "TaxDeclarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCct",
                table: "TaxDeclarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocallyManufactured",
                table: "TaxDeclarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LandAreaSqm",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LandBlockNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandLotNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandOwnerAddress",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandOwnerEmail",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandOwnerName",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandOwnerPhone",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandOwnerSex",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandOwnerTin",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandPin",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandSurveyNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandTdNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandTitleNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MachineryRegNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MarketValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MaterialsOrCode",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Municipality",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NoOfStoreys",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OccupancyPermitNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrNoAndDate",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalCost",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherFloorsSubtotal",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OwnerNameReflectedInCct",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Paintings",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PercentAdjustment",
                table: "TaxDeclarations",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PlumbingSewer",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousAssessedValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PreviousEffectivity",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousMarketValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PreviousOwner",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousPin",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvinceCity",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RatedCapacity",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegistrationStatus",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RemainingEconomicLife",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SefTaxRate",
                table: "TaxDeclarations",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StreetNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralBeams",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralColumns",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralExteriorWalls",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralFinishFront",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralFinishLeft",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralFinishRear",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralFinishRight",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralFloorFinish",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralFlooring",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralFoundation",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralInsideFinish",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralPartitionWalls",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralRoof",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralStairs",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralTrussRoof",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralType",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StructuralWindows",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubClassification",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierAddress",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierSeller",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SurveyNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TaxPeriodFromYear",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TaxPeriodToYear",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Taxability",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxabilityBasis",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleType",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToiletAndBathroom",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBuildingAreaSqm",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValueAdjustment",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "YearInstalled",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "YearOfOperation",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "YearsUsed",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
