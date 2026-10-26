using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNBuildingStructuralMaterials")]
public class TDNBuildingStructuralMaterial
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNBuildingId { get; set; }

    [ForeignKey(nameof(TDNBuildingId))]
    public virtual TDNBuilding TDNBuilding { get; set; } = null!;

    public int PartNumber { get; set; }

    [Required]
    [MaxLength(150)]
    public string PartName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string MaterialSelected { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? SpecificDescription { get; set; }
}
