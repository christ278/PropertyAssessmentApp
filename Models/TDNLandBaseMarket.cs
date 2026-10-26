using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNLandBaseMarkets")]
public class TDNLandBaseMarket
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNLandId { get; set; }

    [ForeignKey(nameof(TDNLandId))]
    public virtual TDNLand TDNLand { get; set; } = null!;

    public int OrderNo { get; set; } = 1;

    public int? ClassificationId { get; set; }
    public string ClassificationName { get; set; } = "RESIDENTIAL";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal BaseMarketValue { get; set; }

    public int? AdjustmentFactorId { get; set; }
    public string AdjustmentFactor { get; set; } = "Corner Lot (+10%)";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PercentAdjustment { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ValueAdjustment { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AdjustedMarketValue { get; set; }
}
