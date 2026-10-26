using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNLands")]
public class TDNLand
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TaxDeclarationId { get; set; }

    [ForeignKey(nameof(TaxDeclarationId))]
    public virtual TaxDeclaration TaxDeclaration { get; set; } = null!;

    // Property Registration Details (CAST)
    public string RegistrationStatus { get; set; } = "Titled";
    public string TitleType { get; set; } = "OCT";
    public string? TitleNo { get; set; }
    public DateTime? DateRegistered { get; set; }
    public string? SurveyNo { get; set; }
    public string? CadastralSurveyNo { get; set; }
    public string? LotNo { get; set; }
    public string? BlockNo { get; set; }
    public string? SectionNo { get; set; }
    public string? ParcelNo { get; set; }
    public string? ARPNo { get; set; }

    // Property Location
    public string? StreetNo { get; set; }
    public string? Purok { get; set; }
    public string? Municipality { get; set; } = "General Santos City";
    public string? DistrictOrBarangay { get; set; } = "Brgy. Central";
    public string? ProvinceCity { get; set; } = "South Cotabato";

    // Property Boundaries & Remarks (CAST)
    public string? BoundaryNorth { get; set; } = "Lot 101";
    public string? BoundaryEast { get; set; } = "Road Lot";
    public string? BoundaryWest { get; set; } = "Creek";
    public string? BoundarySouth { get; set; } = "Lot 103";
    public string? Nremarks { get; set; }
    public string? Eremarks { get; set; }
    public string? Sremarks { get; set; }
    public string? Wremarks { get; set; }

    // Appraisal & Assessment Summary Computations
    public PropertyClassification Classification { get; set; } = PropertyClassification.Residential;
    public string? SubClassification { get; set; } = "First Class Residential";
    public string? ActualUse { get; set; } = "Residential Use";
    public decimal AreaSqMeters { get; set; } = 150m;
    public decimal AreaHectares => AreaSqMeters / 10000m;
    public decimal UnitValue { get; set; } = 3500m;
    public decimal MarketValue { get; set; } = 525000m;

    // Improvements
    public string? ImprovementType { get; set; } = "Fence / Pavement";
    public decimal ImprovementQty { get; set; }
    public string? ImprovementUnitOfMeasure { get; set; } = "sq.m.";
    public decimal ImprovementUnitValue { get; set; }
    public decimal ImprovementMarketValue { get; set; }

    // Adjustments
    public string? AdjustmentFactor { get; set; } = "Location & Elevation";
    public decimal PercentAdjustment { get; set; }
    public decimal ValueAdjustment { get; set; }
    public decimal AdjustedMarketValue { get; set; } = 525000m;

    // Assessment Roll & Taxes
    public decimal AssessmentLevelPercent { get; set; } = 20m;
    public decimal AssessedValue { get; set; } = 105000m;
    public decimal BasicTaxRate { get; set; } = 2.0m;
    public decimal SefTaxRate { get; set; } = 1.0m;
    public decimal AnnualTaxDue { get; set; } = 3150m;

    // Effectivity & Taxability
    public int EffectivityYear { get; set; } = DateTime.Now.Year;
    public string? EffectivityQuarter { get; set; } = "1st Quarter";
    public string? Taxability { get; set; } = "Taxable";
    public string? TaxabilityBasis { get; set; } = "RA 7160 Local Government Code";
    public int? BackTaxesYearFrom { get; set; }
    public int? BackTaxesYearTo { get; set; }

    // Superseded Assessment
    public string? PreviousTdNo { get; set; }
    public string? PreviousPin { get; set; }
    public string? PreviousOwner { get; set; }
    public decimal PreviousMarketValue { get; set; }
    public decimal PreviousAssessedValue { get; set; }

    // Serialized JSON for dynamic multi-line breakdown in UI
    public string? LandAppraisalDetailsJson { get; set; }
    public string? LandImprovementsJson { get; set; }
    public string? LandAdjustmentsJson { get; set; }
    public string? LandPropertyAssessmentsJson { get; set; }

    // Navigation collections for normalized relational persistence
    public virtual ICollection<TDNLandDetail> TDNLandDetails { get; set; } = new List<TDNLandDetail>();
    public virtual ICollection<TDNLandImprovement> TDNLandImprovements { get; set; } = new List<TDNLandImprovement>();
    public virtual ICollection<TDNLandBaseMarket> TDNLandBaseMarkets { get; set; } = new List<TDNLandBaseMarket>();
    public virtual ICollection<TDNLandPropertyAssessment> TDNLandPropertyAssessments { get; set; } = new List<TDNLandPropertyAssessment>();
}
