using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class CityMunicipality
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProvinceId { get; set; }

    [Required]
    [Display(Name = "City / Municipality Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "City / Municipality Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "ZIP Code")]
    public string ZipCode { get; set; } = string.Empty;

    [Display(Name = "Is City")]
    public bool IsCity { get; set; } = true;

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;
}
