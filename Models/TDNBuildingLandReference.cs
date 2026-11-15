using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNBuildingLandReferences")]
public class TDNBuildingLandReference
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNBuildingId { get; set; }

    [ForeignKey(nameof(TDNBuildingId))]
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public virtual TDNBuilding? TDNBuilding { get; set; }

    [Required]
    public Guid LandTaxDeclarationId { get; set; }

    [ForeignKey(nameof(LandTaxDeclarationId))]
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public virtual TaxDeclaration? LandTaxDeclaration { get; set; }

    public string? LandTdNo { get; set; }
    public string? LandPin { get; set; }
    public string? LandOwnerName { get; set; }
    public string? LotNo { get; set; }
    public string? Location { get; set; }
}
