using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("LandRefAdjustment")]
public class LandAdjustmentFactor
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Distance { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AdjustmentPercent { get; set; }

    public int RevisionYear { get; set; } = 2011;

    public bool IsActive { get; set; } = true;
}
