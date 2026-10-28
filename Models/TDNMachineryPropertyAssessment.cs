using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNMachineryPropertyAssessments")]
public class TDNMachineryPropertyAssessment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNMachineryId { get; set; }

    [ForeignKey(nameof(TDNMachineryId))]
    public virtual TDNMachinery TDNMachinery { get; set; } = null!;

    public int OrderNo { get; set; } = 1;

    public int? ActualUseId { get; set; }

    [Required]
    [StringLength(100)]
    public string ActualUseName { get; set; } = "INDUSTRIAL";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal MarketValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AssessmentLevelPercent { get; set; } = 75m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AssessmentValue { get; set; }
}
