using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNBuildings")]
public class TDNBuilding
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TaxDeclarationId { get; set; }

    [ForeignKey(nameof(TaxDeclarationId))]
    public virtual TaxDeclaration TaxDeclaration { get; set; } = null!;

    // -------------------------------------------------------------
    // LOCATION OF PROPERTY
    // -------------------------------------------------------------
    [Display(Name = "No. / Street")]
    public string? StreetNo { get; set; }

    [Display(Name = "Municipality")]
    public string? Municipality { get; set; } = "General Santos City";

    [Display(Name = "Barangay / District")]
    public string? DistrictOrBarangay { get; set; } = "Brgy. Central";

    [Display(Name = "Province / City")]
    public string? ProvinceCity { get; set; } = "South Cotabato";

    // -------------------------------------------------------------
    // LAND REFERENCE (WHERE ERECTED)
    // -------------------------------------------------------------
    [Display(Name = "Landowner Name")]
    public string? LandOwnerName { get; set; }

    [Display(Name = "Landowner TIN")]
    public string? LandOwnerTin { get; set; }

    [Display(Name = "Landowner Phone")]
    public string? LandOwnerPhone { get; set; }

    [Display(Name = "Landowner Email")]
    public string? LandOwnerEmail { get; set; }

    [Display(Name = "Landowner Address")]
    public string? LandOwnerAddress { get; set; }

    [Display(Name = "Landowner Sex")]
    public string? LandOwnerSex { get; set; }

    [Display(Name = "Land TD No.")]
    public string? LandTdNo { get; set; }

    [Display(Name = "Land PIN")]
    public string? LandPin { get; set; }

    [Display(Name = "Land Title No.")]
    public string? LandTitleNo { get; set; }

    [Display(Name = "Land Survey No.")]
    public string? LandSurveyNo { get; set; }

    [Display(Name = "Land Lot No.")]
    public string? LandLotNo { get; set; }

    [Display(Name = "Land Block No.")]
    public string? LandBlockNo { get; set; }

    [Display(Name = "Land Area (sq.m.)")]
    public decimal LandAreaSqm { get; set; }

    // -------------------------------------------------------------
    // CONDOMINIUM TITLE (CCT) & ADMINISTRATION
    // -------------------------------------------------------------
    [Display(Name = "Certificate of Condominium Title (CCT)")]
    public bool IsCct { get; set; } = false;

    [Display(Name = "CCT Title No.")]
    public string? CctTitleNo { get; set; }

    [Display(Name = "CCT Date Registered")]
    public DateTime? CctDateRegistered { get; set; }

    [Display(Name = "Owner Reflected in CCT")]
    public string? OwnerNameReflectedInCct { get; set; }

    [Display(Name = "Is Administrator")]
    public bool IsAdministrator { get; set; } = false;

    [Display(Name = "Administrator Name")]
    public string? AdministratorName { get; set; }

    [Display(Name = "Administrator Address")]
    public string? AdministratorAddress { get; set; }

    [Display(Name = "Administrator Contact No.")]
    public string? AdministratorContactNo { get; set; }

    [Display(Name = "Administrator TIN")]
    public string? AdministratorTin { get; set; }

    [Display(Name = "Administrator Gender")]
    public string? AdministratorGender { get; set; } = "Male";

    [Display(Name = "Administrator Civil Status")]
    public string? AdministratorCivilStatus { get; set; } = "Single";

    [Display(Name = "Administrator Email")]
    public string? AdministratorEmail { get; set; }

    // -------------------------------------------------------------
    // STRUCTURAL TYPE, PERMITS & TIMELINE
    // -------------------------------------------------------------
    [Display(Name = "Structural Type")]
    public string? StructuralType { get; set; } = "Type I";

    [Display(Name = "Building Classification")]
    public string? BuildingClassification { get; set; } = "Class A - Reinforced Concrete";

    [Display(Name = "Building Use Type")]
    public string? BuildingUseType { get; set; } = "Residential Condominium";

    [Display(Name = "Other Building Use Type")]
    public string? OtherBuildingUseType { get; set; }

    [Display(Name = "No. of Storey/s")]
    public int NoOfStoreys { get; set; } = 1;

    [Display(Name = "Floor Level")]
    public string? FloorLevel { get; set; } = "Ground Floor";

    [Display(Name = "Building Permit No.")]
    public string? BuildingPermitNo { get; set; }

    [Display(Name = "Building Age (Years)")]
    public int BuildingAgeYears { get; set; }

    [Display(Name = "Date Constructed / Started")]
    public DateTime? DateConstructed { get; set; }

    [Display(Name = "Date Completed")]
    public DateTime? DateCompleted { get; set; }

    [Display(Name = "Occupancy Permit No.")]
    public string? OccupancyPermitNo { get; set; }

    [Display(Name = "Date Occupied")]
    public DateTime? DateOccupied { get; set; }

    // -------------------------------------------------------------
    // VALUATION, DEPRECIATION & ASSESSED VALUE ROLL
    // -------------------------------------------------------------
    [Display(Name = "TOTAL FLOOR AREA")]
    public decimal TotalFloorArea { get; set; }

    [Display(Name = "BASE RCN")]
    public decimal BaseRcnValue { get; set; }

    [Display(Name = "Replacement Cost New (RCN)")]
    public decimal ReplacementCostNew { get; set; }

    [Display(Name = "Accumulated Depr. (%)")]
    public decimal BuildingDepreciationPercent { get; set; }

    [Display(Name = "Accumulated Depr. (Php)")]
    public decimal BuildingDepreciationAccumulated { get; set; }

    [Display(Name = "Depreciated Core MV")]
    public decimal DepreciatedMarketValue { get; set; }

    [Display(Name = "Additional Improvements")]
    public decimal TotalAdditionalItemsValue { get; set; }

    [Display(Name = "Grand Combined FMV")]
    public decimal MarketValue { get; set; }

    [Display(Name = "Assessment Level (%)")]
    public decimal AssessmentLevelPercent { get; set; } = 20m;

    [Display(Name = "Assessed Value (AV)")]
    public decimal AssessedValue { get; set; }

    [Display(Name = "Basic RPT Rate (%)")]
    public decimal BasicTaxRate { get; set; } = 2.0m;

    [Display(Name = "SEF Rate (%)")]
    public decimal SefTaxRate { get; set; } = 1.0m;

    [Display(Name = "Annual Tax Due")]
    public decimal AnnualTaxDue { get; set; }

    // -------------------------------------------------------------
    // EFFECTIVITY, TAXABILITY & BACK TAXES
    // -------------------------------------------------------------
    [Display(Name = "EFFECTIVITY YEAR")]
    public int EffectivityYear { get; set; } = DateTime.Now.Year;

    [Display(Name = "EFFECTIVITY QUARTER")]
    public string? EffectivityQuarter { get; set; } = "1st Quarter";

    [Display(Name = "TAXABILITY STATUS")]
    public string? Taxability { get; set; } = "Taxable";

    [Display(Name = "BASIS OF ASSESSMENT")]
    public string? AssessmentBasis { get; set; } = "Sec. 217 RA 7160";

    [Display(Name = "BACK TAXES FROM YEAR")]
    public int? BackTaxesYearFrom { get; set; }

    [Display(Name = "BACK TAXES TO YEAR")]
    public int? BackTaxesYearTo { get; set; }

    // JSON snapshots for fast offline rendering / backwards compatibility
    public string? FloorBreakdownJson { get; set; }
    public string? AdditionalItemsJson { get; set; }

    // Navigation Collections
    public virtual ICollection<TDNBuildingStructuralMaterial> StructuralMaterials { get; set; } = new List<TDNBuildingStructuralMaterial>();
    public virtual ICollection<TDNBuildingFloor> FloorLevels { get; set; } = new List<TDNBuildingFloor>();
    public virtual ICollection<TDNBuildingAdditionalItem> AdditionalItems { get; set; } = new List<TDNBuildingAdditionalItem>();
}
