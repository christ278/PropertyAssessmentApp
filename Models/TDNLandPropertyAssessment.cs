using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNLandPropertyAssessments")]
public class TDNLandPropertyAssessment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNLandId { get; set; }

    [ForeignKey(nameof(TDNLandId))]
    public virtual TDNLand TDNLand { get; set; } = null!;

    public int OrderNo { get; set; } = 1;

    [Required]
    [StringLength(50)]
    public string Kinds { get; set; } = "LAND";

    public int? ActualUseId { get; set; }
    public string ActualUseName { get; set; } = "RESIDENTIAL";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal MarketValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AssessmentLevelPercent { get; set; } = 20m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AssessmentValue { get; set; }
}
