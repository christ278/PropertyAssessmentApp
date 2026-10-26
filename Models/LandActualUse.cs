using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("refActualUse")]
public class LandActualUse
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ActualUseCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ActualUseName { get; set; } = string.Empty;

    public bool IsTaxExempt { get; set; } = false;

    public int? Rank { get; set; }

    public bool IsActive { get; set; } = true;
}
