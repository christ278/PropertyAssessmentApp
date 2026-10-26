using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class Barangay
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CityMunicipalityId { get; set; }

    [Required]
    [Display(Name = "Barangay Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Barangay Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "District / Zone")]
    public string District { get; set; } = "District 1";

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;
}
