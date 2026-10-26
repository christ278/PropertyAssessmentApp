using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNBuildingAdditionalItems")]
public class TDNBuildingAdditionalItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNBuildingId { get; set; }

    [ForeignKey(nameof(TDNBuildingId))]
    public virtual TDNBuilding TDNBuilding { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string ItemDescription { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public decimal UnitRate { get; set; }

    public decimal MultiplierPercent { get; set; }

    [MaxLength(50)]
    public string RateType { get; set; } = "FixedCost";

    public decimal TotalValue { get; set; }

    public bool IsCustom { get; set; }

    public bool ForAssessment { get; set; } = true;

    [MaxLength(100)]
    public string? FloorNo { get; set; }
}
