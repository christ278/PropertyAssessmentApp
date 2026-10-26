using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNLandImprovements")]
public class TDNLandImprovement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNLandId { get; set; }

    [ForeignKey(nameof(TDNLandId))]
    public virtual TDNLand TDNLand { get; set; } = null!;

    public int OrderNo { get; set; } = 1;

    public int? TypeId { get; set; }
    public string ProductClass { get; set; } = "COCONUT TREE";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? AreaPlanted { get; set; }

    public bool IsFruitBearing { get; set; } = true;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Quantity { get; set; }

    public int? AgeYears { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal MarketValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AssessmentLevel { get; set; } = 40m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AssessedValue { get; set; }

    public bool IsTaxable { get; set; } = true;
}
