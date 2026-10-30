using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

/// <summary>
/// SQL-backed model for the official BLGF / MRPAAO Schedule of Building Physical Depreciation (% Depreciation)
/// across Building Classifications I, II-A, II-B, III-A, III-B, IV-A, IV-B, V-A, V-B and General Revision (GR) Cycles.
/// </summary>
[Table("BuildingDepreciationSchedules")]
public class BuildingDepreciationScheduleItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "General Revision Cycle")]
    public string GeneralRevisionCycle { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Display(Name = "Building Age Label")]
    public string BuildingAgeLabel { get; set; } = string.Empty;

    [Display(Name = "Age Lower Bound (Years)")]
    public int AgeLowerBound { get; set; }

    [Display(Name = "Age Upper Bound (Years)")]
    public int AgeUpperBound { get; set; }

    [Required]
    [MaxLength(20)]
    [Display(Name = "Classification Code")]
    public string ClassificationCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Display(Name = "Structural Type")]
    public string StructuralType { get; set; } = string.Empty;

    [Display(Name = "Estimated Economic Life (Years)")]
    public int EstimatedEconomicLifeYears { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Estimated Residual Value (%)")]
    public decimal EstimatedResidualValuePercent { get; set; }

    /// <summary>
    /// Explicit % depreciation value from the schedule cell, or null when the cell is "—" (capped at Residual Value floor).
    /// </summary>
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Schedule Cell Depreciation (%)")]
    public decimal? DepreciationPercent { get; set; }

    /// <summary>
    /// Effective % depreciation applied in valuation: either <see cref="DepreciationPercent"/> or (100 - <see cref="EstimatedResidualValuePercent"/>) when capped.
    /// </summary>
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Effective Depreciation (%)")]
    public decimal EffectiveDepreciationPercent { get; set; }

    [Display(Name = "Is Residual Floor Cap (—)")]
    public bool IsResidualFloorCap { get; set; }

    [Display(Name = "Revision Year")]
    public int RevisionYear { get; set; } = 2024;

    [Display(Name = "Sort Order")]
    public int SortOrder { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Navigation back to TDNBuildings using this schedule entry
    public virtual ICollection<TDNBuilding> TDNBuildings { get; set; } = new List<TDNBuilding>();
}
