using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("LandRefClassification")]
public class LandClassification
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ClassificationCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ClassificationName { get; set; } = string.Empty;

    public int? Ranking { get; set; }

    [StringLength(20)]
    public string? Prefix { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 1;
}
