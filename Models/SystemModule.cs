using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("SystemModule")]
public class SystemModule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ModuleId { get; set; }

    [Required]
    [StringLength(50)]
    public string ModuleCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ModuleName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Category { get; set; }

    [StringLength(50)]
    public string? ControllerName { get; set; }

    [StringLength(50)]
    public string? ActionName { get; set; }

    [StringLength(50)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
