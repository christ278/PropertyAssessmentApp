using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNBuildingFloors")]
public class TDNBuildingFloor
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNBuildingId { get; set; }

    [ForeignKey(nameof(TDNBuildingId))]
    public virtual TDNBuilding TDNBuilding { get; set; } = null!;

    public int FloorOrder { get; set; } = 1;

    [Required]
    [MaxLength(100)]
    public string FloorNumber { get; set; } = "1st Floor";

    [MaxLength(250)]
    public string Description { get; set; } = "Ground Level";

    public decimal FloorAreaSqm { get; set; }

    public decimal BaseUnitCost { get; set; }

    public decimal MarketValue { get; set; }
}
