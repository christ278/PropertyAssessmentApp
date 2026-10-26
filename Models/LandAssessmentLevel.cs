using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("LandRefAssessmentLevel")]
public class LandAssessmentLevel
{
    [Key]
    public int Id { get; set; }

    public int? ActualUseId { get; set; }

    [Required]
    [StringLength(100)]
    public string ActualUseName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AssessmentLevel { get; set; }

    public int RevisionYear { get; set; } = 2011;

    public bool IsActive { get; set; } = true;
}
