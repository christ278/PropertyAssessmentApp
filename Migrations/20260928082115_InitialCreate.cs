using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Barangays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CityMunicipalityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barangays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CityMunicipalities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCity = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityMunicipalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Particular = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxDeclarations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    TaxDeclarationNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PropertyIndexNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerTin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerSex = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminPossessorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminPossessorSex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminPossessorAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminPossessorTin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminPossessorPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminPossessorEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateRegistered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SurveyNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlockNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsLocallyManufactured = table.Column<bool>(type: "bit", nullable: false),
                    MachineryRegNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportationPermitNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportationDateIssued = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupplierSeller = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupplierAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcquisitionCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OrNoAndDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuildingUseType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StreetNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Municipality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistrictOrBarangay = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProvinceCity = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    LandAreaSqm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BuildingReferenceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerTin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingOwnerSex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingTdNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoundaryNorth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BoundaryEast = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BoundaryWest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BoundarySouth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoOfStoreys = table.Column<int>(type: "int", nullable: false),
                    BuildingClassification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuildingPermitNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuildingAgeYears = table.Column<int>(type: "int", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OccupancyPermitNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOccupied = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FloorLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherFloorsSubtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalBuildingAreaSqm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StructuralFoundation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralColumns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralBeams = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralTrussRoof = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralRoof = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralExteriorWalls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralFinishFront = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralFinishRear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralFinishLeft = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralFinishRight = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralInsideFinish = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralPartitionWalls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralFlooring = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralFloorFinish = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralStairs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoorMain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoorRoom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoorOthers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ElectricalSystem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToiletAndBathroom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlumbingSewer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Paintings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ceiling = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaterialsOrCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescriptionBrand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RatedCapacity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionWhenAcquired = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAcquired = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EconomicLifeYears = table.Column<int>(type: "int", nullable: false),
                    YearInstalled = table.Column<int>(type: "int", nullable: false),
                    YearOfOperation = table.Column<int>(type: "int", nullable: false),
                    OriginalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InstallationCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YearsUsed = table.Column<int>(type: "int", nullable: false),
                    RemainingEconomicLife = table.Column<int>(type: "int", nullable: false),
                    DepreciationRatePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepreciatedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    SubClassification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaSqMeters = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImprovementType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImprovementQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImprovementUnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImprovementUnitValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImprovementMarketValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdjustmentFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PercentAdjustment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValueAdjustment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdjustedMarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssessmentLevelPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualUse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasicTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SefTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EffectivityYear = table.Column<int>(type: "int", nullable: false),
                    EffectivityQuarter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Taxability = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxabilityBasis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxPeriodFromYear = table.Column<int>(type: "int", nullable: false),
                    TaxPeriodToYear = table.Column<int>(type: "int", nullable: false),
                    PreviousTdNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousMarketValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousOwner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousAssessedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousEffectivity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppraisedByName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAppraised = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssessedByName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAssessed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecommendingApprovalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateRecommended = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Memoranda = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedByName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateApproved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntryInRollName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntryInRollPosition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntryInRollDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EncodedByName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateEncoded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDeclarations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxMappingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicantName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerContactNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PropertyIndexNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxDeclarationNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequesterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ORNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ORDateIssued = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Purposes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReqIdOfOwner = table.Column<bool>(type: "bit", nullable: false),
                    ReqAuthorizationLetter = table.Column<bool>(type: "bit", nullable: false),
                    ReqCertIndigency = table.Column<bool>(type: "bit", nullable: false),
                    ReqIdOfRepresentative = table.Column<bool>(type: "bit", nullable: false),
                    ReqDeedOfConveyance = table.Column<bool>(type: "bit", nullable: false),
                    CertTaxDeclaration = table.Column<bool>(type: "bit", nullable: false),
                    CertRecordsVerification = table.Column<bool>(type: "bit", nullable: false),
                    CertTaxDecTimeOfDeath = table.Column<bool>(type: "bit", nullable: false),
                    CertAnnotationBailBond = table.Column<bool>(type: "bit", nullable: false),
                    CertPropertyHistory = table.Column<bool>(type: "bit", nullable: false),
                    CertAnnotationMortgages = table.Column<bool>(type: "bit", nullable: false),
                    CertNoImprovement = table.Column<bool>(type: "bit", nullable: false),
                    CertCancellationAnnotation = table.Column<bool>(type: "bit", nullable: false),
                    CertSupportingDocuments = table.Column<bool>(type: "bit", nullable: false),
                    CertRoadRightOfWay = table.Column<bool>(type: "bit", nullable: false),
                    CertNoRoadRightOfWay = table.Column<bool>(type: "bit", nullable: false),
                    CertTaxMap = table.Column<bool>(type: "bit", nullable: false),
                    CertImageMap = table.Column<bool>(type: "bit", nullable: false),
                    CertPropertyIdVerification = table.Column<bool>(type: "bit", nullable: false),
                    CertFieldInspection = table.Column<bool>(type: "bit", nullable: false),
                    CertOthers = table.Column<bool>(type: "bit", nullable: false),
                    CertOthersSpecification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedDocumentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalFeesAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CityMunicipalityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BarangayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProvinceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CityMunicipalityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BarangayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StreetAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ParcelAreaSqm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssignedStaff = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentFileSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhysicalStoragePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebRelativePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxMappingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxMappingRequests_Barangays_BarangayId",
                        column: x => x.BarangayId,
                        principalTable: "Barangays",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxMappingRequests_CityMunicipalities_CityMunicipalityId",
                        column: x => x.CityMunicipalityId,
                        principalTable: "CityMunicipalities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxMappingRequests_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxpayerOwners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorporateName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxIdentNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RptinNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CityMunicipalityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BarangayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProvinceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CityMunicipalityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BarangayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxpayerOwners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxpayerOwners_Barangays_BarangayId",
                        column: x => x.BarangayId,
                        principalTable: "Barangays",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxpayerOwners_CityMunicipalities_CityMunicipalityId",
                        column: x => x.CityMunicipalityId,
                        principalTable: "CityMunicipalities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxpayerOwners_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxMappingRequests_BarangayId",
                table: "TaxMappingRequests",
                column: "BarangayId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxMappingRequests_CityMunicipalityId",
                table: "TaxMappingRequests",
                column: "CityMunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxMappingRequests_ProvinceId",
                table: "TaxMappingRequests",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxpayerOwners_BarangayId",
                table: "TaxpayerOwners",
                column: "BarangayId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxpayerOwners_CityMunicipalityId",
                table: "TaxpayerOwners",
                column: "CityMunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxpayerOwners_ProvinceId",
                table: "TaxpayerOwners",
                column: "ProvinceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPayments");

            migrationBuilder.DropTable(
                name: "TaxDeclarations");

            migrationBuilder.DropTable(
                name: "TaxMappingRequests");

            migrationBuilder.DropTable(
                name: "TaxpayerOwners");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropTable(
                name: "Barangays");

            migrationBuilder.DropTable(
                name: "CityMunicipalities");

            migrationBuilder.DropTable(
                name: "Provinces");
        }
    }
}
