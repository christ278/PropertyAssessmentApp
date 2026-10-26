using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("refImprovementType")]
public class LandImprovementType
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string TypeCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string TypeDescription { get; set; } = string.Empty;

    [StringLength(50)]
    public string Kind { get; set; } = "FRUIT BEARING";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal BaseValue { get; set; } = 50.00m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AssessmentLevel { get; set; } = 40.00m;

    public int RevisionYear { get; set; } = 2011;

    public bool IsActive { get; set; } = true;
}
