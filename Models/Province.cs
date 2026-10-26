using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class Province
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Province Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Province Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Region")]
    public string Region { get; set; } = "Region XII";

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;
}
