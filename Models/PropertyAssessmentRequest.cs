using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

public enum PropertyAssessmentStatus
{
    [Display(Name = "Pending Verification")]
    Pending = 0,

    [Display(Name = "Receiving & Encoding")]
    Receiving = 1,

    [Display(Name = "GIS Replotting / Mapping")]
    Replotting = 2,

    [Display(Name = "Appraisal & Valuation")]
    Valuation = 3,

    [Display(Name = "Check and Review")]
    Review = 4,

    [Display(Name = "City Assessor Approval")]
    Approval = 5,

    [Display(Name = "Approved & TD Issued")]
    Approved = 6,

    [Display(Name = "Deferred / Returned")]
    Deferred = 7
}

public class PropertyAssessmentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Tracking No.")]
    public string TrackingNumber { get; set; } = string.Empty;

    public static readonly Dictionary<string, string> StandardTransactionCodes = new()
    {
        { "SD", "Subdivision" },
        { "CS", "Consolidation" },
        { "DC", "Discovery/New Declaration" },
        { "PC", "Reassessment Due to Physical Change" },
        { "RA", "Reassessment" },
        { "DT", "Reassessment Due to partial Destruction of the property" },
        { "TR", "Transfer" },
        { "RC", "Reclassification" },
        { "GR", "General Revision" },
        { "SP", "Special Project" },
        { "DEP", "Reassessment Due to depreciation" }
    };

    [Display(Name = "Transaction Code")]
    public string TransactionTypeCode { get; set; } = "DC";

    [Display(Name = "Transaction Type Name")]
    public string TransactionTypeName { get; set; } = "Discovery/New Declaration";

    [Display(Name = "Transaction Type")]
    public string TransactionTypeDisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(TransactionTypeCode) &&
                StandardTransactionCodes.TryGetValue(TransactionTypeCode.Trim().ToUpperInvariant(), out var desc))
            {
                return $"{desc} - {TransactionTypeCode.Trim().ToUpperInvariant()}";
            }
            return !string.IsNullOrWhiteSpace(TransactionTypeName) ? TransactionTypeName : (TransactionTypeCode ?? string.Empty);
        }
    }

    [Display(Name = "Property Kind")]
    public string Kind { get; set; } = "Land"; // Land, Building, Machinery

    [Display(Name = "Partition Count (Lots/TDNs)")]
    public int Partitions { get; set; } = 1;

    // * REFERENCE PROPERTY & PREVIOUS OWNER (MATCHING CAST)
    [Display(Name = "Reference Tax Declaration")]
    public Guid? RefTaxDeclarationId { get; set; }

    [Display(Name = "Previous TD No.")]
    public string? PrevTDN { get; set; }

    [Display(Name = "Previous PIN")]
    public string? PrevPIN { get; set; }

    [Display(Name = "Previous Owner / Declared Owner")]
    public string? PrevOwner { get; set; }

    [Display(Name = "Lot No. / Survey No.")]
    public string? LotNo { get; set; } = "N/A";

    [Display(Name = "Property Location Address")]
    public string? PropertyLocation { get; set; } = "N/A";

    // * NEW OWNER / DECLARANT INFORMATION
    [Required(ErrorMessage = "New Owner Name is required.")]
    [Display(Name = "New Owner / Buyer Name")]
    public string NewOwnerName { get; set; } = string.Empty;

    [Display(Name = "Owner Address")]
    public string? Address { get; set; }

    [Display(Name = "Owner TIN")]
    public string? OwnerTIN { get; set; }

    [Display(Name = "Contact / Telephone No.")]
    public string? ContactNo { get; set; }

    [Display(Name = "Email Address")]
    public string? EmailAddress { get; set; }

    [Display(Name = "Requestor / Representative Name")]
    public string? Requestor { get; set; }

    // * DOCUMENTS SUBMITTED CHECKLIST (MATCHING CAST CATEGORIES)
    // 1. Basic Requirements
    [Display(Name = "Certificate of Title (OCT / TCT / CLOA)")]
    public bool DocTitleTctCloa { get; set; }

    [Display(Name = "Deed of Absolute Sale / Conveyance")]
    public bool DocDeedOfSaleConveyance { get; set; }

    [Display(Name = "BIR Certificate Authorizing Registration (CAR)")]
    public bool DocCarBir { get; set; }

    [Display(Name = "Real Property Tax Clearance")]
    public bool DocTaxClearance { get; set; }

    [Display(Name = "Transfer Tax Official Receipt")]
    public bool DocTransferTaxReceipt { get; set; }

    // 2. Newly Discovered Lands
    [Display(Name = "Approved Survey Plan (DENR / LMB)")]
    public bool DocApprovedSurveyPlan { get; set; }

    [Display(Name = "Cadastral Map Blueprint")]
    public bool DocCadastralMap { get; set; }

    [Display(Name = "DENR / CENRO Alienable & Disposable Certification")]
    public bool DocDenrCert { get; set; }

    // 3. New Building/s
    [Display(Name = "Building Permit")]
    public bool DocBuildingPermit { get; set; }

    [Display(Name = "Certificate of Completion")]
    public bool DocCertCompletion { get; set; }

    [Display(Name = "Certificate of Occupancy")]
    public bool DocCertOccupancy { get; set; }

    // 4. For Machineries
    [Display(Name = "Commercial Invoices / Official Receipts")]
    public bool DocInvoiceReceipts { get; set; }

    [Display(Name = "Installation & Freight Cost Breakdown")]
    public bool DocInstallationCostBreakdown { get; set; }

    [Display(Name = "Machinery Specifications & Manuals")]
    public bool DocSpecsManual { get; set; }

    // 5. For Representative/s
    [Display(Name = "Special Power of Attorney (SPA) / Authorization Letter")]
    public bool DocSpaAuthLetter { get; set; }

    [Display(Name = "Valid Government ID of Owner & Representative")]
    public bool DocValidIdOwnerRep { get; set; }

    // 6. Others
    [Display(Name = "Other Documents")]
    public bool DocOthers { get; set; }

    [Display(Name = "Other Documents Specification")]
    public string? DocOthersSpecify { get; set; }

    // * ORDER OF PAYMENT & BILLING
    [Display(Name = "Requested Documents JSON")]
    public string? RequestedDocumentsJson { get; set; }

    [Display(Name = "Generated Partitions JSON")]
    public string? GeneratedPartitionsJson { get; set; }

    [Display(Name = "Selected Reference Property IDs JSON")]
    public string? SelectedRefPropertyIdsJson { get; set; }

    [Display(Name = "Total Fees Amount (₱)")]
    public decimal TotalFeesAmount { get; set; }

    [Display(Name = "Official Receipt No.")]
    public string? ORNumber { get; set; }

    [Display(Name = "OR Date Issued")]
    public DateTime? ORDateIssued { get; set; } = DateTime.Now;

    // * FILE ATTACHMENTS (ROUTED TO CAST PROPERTY ASSESSMENT LOCATION ~/UploadFile/PropertyAssessment/{TrackingNo})
    [ValidateNever]
    public string? AttachmentFileName { get; set; }

    [ValidateNever]
    public string? AttachmentFileSize { get; set; }

    [ValidateNever]
    public string? PhysicalStoragePath { get; set; }

    [ValidateNever]
    public string? WebRelativePath { get; set; }

    // * WORKFLOW STATUS & TIMESTAMPS
    [Display(Name = "Status")]
    public PropertyAssessmentStatus Status { get; set; } = PropertyAssessmentStatus.Receiving;

    [Display(Name = "Assigned Appraiser")]
    public string? AssignedAppraiser { get; set; }

    [Display(Name = "Appraisal Remarks")]
    public string? Remarks { get; set; }

    [Display(Name = "Date Filed")]
    public DateTime RequestDate { get; set; } = DateTime.Now;

    [Display(Name = "Last Updated")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;
}

public class AssessmentPartitionItem
{
    public string PartitionIndex { get; set; } = "A";
    public string BaseTDN { get; set; } = string.Empty;
    public Guid? BasePropertyId { get; set; }
    public string SystemGeneratedTDN { get; set; } = string.Empty;
    public string? PIN { get; set; }
    public string? PropertyTypeAccordUse { get; set; } = "Residential Condominium";
    public string? OtherPropertyTypeAccordUse { get; set; }
    public string? TitleNo { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public string SurveyNumber { get; set; } = string.Empty;
    public string BlockNumber { get; set; } = string.Empty;
    public string Kind { get; set; } = "Land";
    public string Classification { get; set; } = "Residential";
    public string ActualUse { get; set; } = "Residential";
    public decimal AreaSqMeters { get; set; }
    public decimal UnitValue { get; set; }
    public decimal MarketValue { get; set; }
    public decimal AssessmentLevelPercent { get; set; }
    public decimal AssessedValue { get; set; }
    public string NewOwnerName { get; set; } = string.Empty;
    public string? OwnerAddress { get; set; }
    public string? OwnerContactNo { get; set; }
    public string? OwnerTIN { get; set; }
    public string? OwnerGender { get; set; } = "Male";
    public string? OwnerCivilStatus { get; set; } = "Single";
    public string? OwnerEmail { get; set; }
    public string? DistrictOrBarangay { get; set; }
    public string? StreetLocation { get; set; }
    public string? CityMunicipality { get; set; }
    public string? Province { get; set; }
    public string? PropertyLocation { get; set; }

    public string DisplayPropertyLocation
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PropertyLocation)) return PropertyLocation;
            var parts = new[] { StreetLocation, DistrictOrBarangay, CityMunicipality, Province }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var combined = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(combined) ? "N/A" : combined;
        }
    }

    // Condominium CCT & Administrator Details (CAST Reference)
    public bool IsCct { get; set; }
    public string? CctTitleNo { get; set; }
    public DateTime? CctDateRegistered { get; set; }
    public string? OwnerNameReflectedInCct { get; set; }
    public bool IsAdministrator { get; set; }
    public string? AdministratorName { get; set; }
    public string? AdministratorAddress { get; set; }
    public string? AdministratorContactNo { get; set; }
    public string? AdministratorTin { get; set; }
    public string? AdministratorGender { get; set; } = "Male";
    public string? AdministratorCivilStatus { get; set; } = "Single";
    public string? AdministratorEmail { get; set; }

    // CAST Land Appraisal & Registration Specifics
    public string RegistrationStatus { get; set; } = "Titled";
    public string TitleType { get; set; } = "OCT";
    public DateTime? DateRegistered { get; set; }
    public string? CadastralSurveyNo { get; set; }
    public string? SectionNo { get; set; }
    public string? ParcelNo { get; set; }
    public string? ARPNo { get; set; }
    public string? Purok { get; set; }

    // CAST Land Boundaries & Boundary Remarks
    public string? BoundaryNorth { get; set; }
    public string? BoundaryEast { get; set; }
    public string? BoundarySouth { get; set; }
    public string? BoundaryWest { get; set; }
    public string? Nremarks { get; set; }
    public string? Eremarks { get; set; }
    public string? Sremarks { get; set; }
    public string? Wremarks { get; set; }

    // CAST 4 Land Calculation Schedules (JSON Strings)
    public string? LandAppraisalDetailsJson { get; set; }
    public string? LandImprovementsJson { get; set; }
    public string? LandAdjustmentsJson { get; set; }
    public string? LandPropertyAssessmentsJson { get; set; }

    // Building Floor Plan Sketch & Blueprint (CAST Reference)
    public string? BuildingFloorPlanSketchPath { get; set; }
    public string? BuildingFloorPlanSketchBase64 { get; set; }

    // Structural Characteristics & Construction Timeline (CAST Reference)
    public string StructuralType { get; set; } = "Type I";
    public string BuildingClassification { get; set; } = "Class A - Reinforced Concrete";
    public int NoOfStoreys { get; set; } = 1;
    public string BuildingPermitNo { get; set; } = string.Empty;
    public string OccupancyPermitNo { get; set; } = string.Empty;
    public int BuildingAgeYears { get; set; }
    public int BuildingAge
    {
        get => BuildingAgeYears;
        set => BuildingAgeYears = value;
    }
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateConstructed { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateCompleted { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateOccupied { get; set; }
    public decimal TotalBuildingAreaSqm { get; set; }

    // 17 Structural Materials & Finishes (CAST Reference)
    public string StructuralFoundation { get; set; } = "Reinforced Concrete (RC)";
    public string StructuralColumns { get; set; } = "Reinforced Concrete (RC)";
    public string StructuralBeams { get; set; } = "Reinforced Concrete (RC)";
    public string StructuralTrussRoof { get; set; } = "Steel Truss (S)";
    public string StructuralRoof { get; set; } = "Long Span G.I. Sheet (LGI)";
    public string StructuralExteriorWalls { get; set; } = "Concrete Hollow Block (CHB)";
    public string StructuralFlooring { get; set; } = "Reinforced Concrete Slab (RC)";
    public string DoorMain { get; set; } = "Wood Panel (WP)";
    public string Ceiling { get; set; } = "Hardiflex / Fiber Cement (Hx)";
    public string StructuralWindows { get; set; } = "Aluminum Casement / Sliding (AC)";
    public string StructuralStairs { get; set; } = "Reinforced Concrete (RC)";
    public string StructuralPartitionWalls { get; set; } = "Drywall Hardiflex (Hx)";
    public string StructuralFloorFinish { get; set; } = "Ceramic / Granite Tiles (T)";
    public string StructuralFinishFront { get; set; } = "Cement Plaster Paint Finish (CP)";
    public string ElectricalSystem { get; set; } = "Poly Vinyl Carbonate Conduit (EPVC)";
    public string ToiletAndBathroom { get; set; } = "Ceramic Tiled with Fixtures (CT)";
    public string PlumbingSewer { get; set; } = "PVC Pipes to Septic Tank (PVCP)";

    // Floor-by-Floor & Additions Matrix (CAST Reference)
    public string? BuildingFloorBreakdownJson { get; set; }
    public decimal BaseRcnValue { get; set; }
    public string? BuildingAdditionalItemsJson { get; set; }
    public decimal BuildingTotalAdditionsValue { get; set; }
    public decimal BuildingDepreciationPercent { get; set; }
    public decimal DepreciationRate
    {
        get => BuildingDepreciationPercent;
        set => BuildingDepreciationPercent = value;
    }
    public decimal BuildingDepreciationAccumulated { get; set; }
    public decimal BuildingDepreciatedMarketValue { get; set; }

    // Reference Land Information
    public string? LandOwnerName { get; set; }
    public string? LandTdNo { get; set; }
    public string? LandPin { get; set; }

    // Effectivity, Taxability & Back Taxes (CAST Reference)
    public string Taxability { get; set; } = "Taxable";
    public int? EffectivityYear { get; set; }
    public string? EffectivityQuarter { get; set; } = "1st Quarter";
    public string? AssessmentBasis { get; set; } = "Sec. 217 RA 7160";
    public int? BackTaxesYearFrom { get; set; }
    public int? BackTaxesYearTo { get; set; }
    public string? Memoranda { get; set; }
}
