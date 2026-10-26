using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("refSubClassification")]
public class LandSubClassification
{
    [Key]
    public int Id { get; set; }

    public int ClassificationId { get; set; }

    [Required]
    [StringLength(100)]
    public string SubClass { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitValue { get; set; }

    public int RevisionYear { get; set; } = 2011;

    public bool IsActive { get; set; } = true;
}
