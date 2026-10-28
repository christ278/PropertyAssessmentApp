using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNMachineries")]
public class TDNMachinery
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TaxDeclarationId { get; set; }

    [ForeignKey(nameof(TaxDeclarationId))]
    public virtual TaxDeclaration TaxDeclaration { get; set; } = null!;

    // Machinery Registration Details (Outside the Philippines / Imported & Local Manufacturer)
    public bool IsLocallyManufactured { get; set; }
    public string? MachineryRegNo { get; set; }
    public DateTime? MachineryDateRegistered { get; set; }
    public string? ImportationPermitNo { get; set; }
    public DateTime? ImportationDateIssued { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? SupplierSeller { get; set; }
    public string? SupplierAddress { get; set; }
    public decimal AcquisitionCost { get; set; }
    public string? OrNo { get; set; }
    public DateTime? OrDate { get; set; }
    public string? OrNoAndDate { get; set; }
    public DateTime? LocalDateIssued { get; set; }

    // Machinery Appraisal Details
    public string? DescriptionBrand { get; set; }
    public string? RatedCapacity { get; set; }
    public string? ConditionWhenAcquired { get; set; } = "New";
    public DateTime? DateAcquired { get; set; }
    public int EconomicLifeYears { get; set; } = 10;
    public int YearInstalled { get; set; } = DateTime.Now.Year;
    public int YearOfOperation { get; set; } = DateTime.Now.Year;
    public decimal OriginalCost { get; set; }
    public decimal InstallationCost { get; set; }
    public decimal TotalCost { get; set; }
    public int YearsUsed { get; set; }
    public int RemainingEconomicLife { get; set; } = 10;
    public decimal DepreciationRatePercent { get; set; }
    public decimal DepreciatedValue { get; set; }

    // Property Location & Parcel Identifiers (CAST)
    public string? StreetNo { get; set; }
    public string? Municipality { get; set; } = "General Santos City";
    public string? DistrictOrBarangay { get; set; } = "Brgy. Central";
    public string? ProvinceCity { get; set; } = "South Cotabato";
    public string? ARPNo { get; set; }
    public string? SectionNo { get; set; }
    public string? ParcelNo { get; set; }
    public string? LotNo { get; set; }

    // Administrator / Beneficial User / Possessor Demographics (CAST)
    public bool IsAdministrator { get; set; }
    public bool IsBeneficialUser { get; set; }
    public bool IsPossessor { get; set; }
    public string? AdministratorName { get; set; }
    public string? AdministratorSex { get; set; } = "Male";
    public string? AdministratorAddress { get; set; }
    public string? AdministratorTin { get; set; }
    public string? AdministratorPhone { get; set; }
    public string? AdministratorEmail { get; set; }

    // References (CAST Building Owner, Building Owner PIN, Land Owner, Land Owner PIN)
    public bool IsLandOwnerAlsoBuildingOwner { get; set; }
    public string? LandOwnerName { get; set; }
    public string? LandTdNo { get; set; }
    public string? LandPin { get; set; }
    public string? BuildingReferenceName { get; set; }
    public string? BuildingOwnerTin { get; set; }
    public string? BuildingOwnerPhone { get; set; }
    public string? BuildingOwnerAddress { get; set; }
    public string? BuildingOwnerSex { get; set; }
    public string? BuildingTdNo { get; set; }
    public string? BuildingPin { get; set; }

    // Appraisal & Assessment Computations
    public PropertyClassification Classification { get; set; } = PropertyClassification.Industrial;
    public string? ActualUse { get; set; } = "INDUSTRIAL";
    public decimal AssessmentLevelPercent { get; set; } = 75m;
    public decimal AssessedValue { get; set; }
    public decimal BasicTaxRate { get; set; } = 2.0m;
    public decimal SefTaxRate { get; set; } = 1.0m;
    public decimal AnnualTaxDue { get; set; }

    // Effectivity & Taxability
    public int EffectivityYear { get; set; } = DateTime.Now.Year;
    public string? EffectivityQuarter { get; set; } = "1st Quarter";
    public string? Taxability { get; set; } = "Taxable";
    public string? TaxabilityBasis { get; set; } = "RA 7160";
    public int? BackTaxesYearFrom { get; set; }
    public int? BackTaxesYearTo { get; set; }

    // Superseded Assessment
    public string? PreviousTdNo { get; set; }
    public string? PreviousPin { get; set; }
    public string? PreviousOwner { get; set; }
    public decimal PreviousMarketValue { get; set; }
    public decimal PreviousAssessedValue { get; set; }

    // Serialized JSON for dynamic multi-line breakdown in UI
    public string? MachineryAppraisalDetailsJson { get; set; }
    public string? MachineryPropertyAssessmentsJson { get; set; }

    // Navigation collections for normalized relational persistence
    public virtual ICollection<TDNMachineryDetail> TDNMachineryDetails { get; set; } = new List<TDNMachineryDetail>();
    public virtual ICollection<TDNMachineryPropertyAssessment> TDNMachineryPropertyAssessments { get; set; } = new List<TDNMachineryPropertyAssessment>();
}
