using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNLandDetails")]
public class TDNLandDetail
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

    public int? SubClassId { get; set; }
    public string SubClassName { get; set; } = "1st Class";

    public int? ActualUseId { get; set; }
    public string ActualUseName { get; set; } = "RESIDENTIAL";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AreaSQM { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal AreaHAS { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal MarketValue { get; set; }

    public bool IsTaxable { get; set; } = true;
}
