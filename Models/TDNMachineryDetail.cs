using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TDNMachineryDetails")]
public class TDNMachineryDetail
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TDNMachineryId { get; set; }

    [ForeignKey(nameof(TDNMachineryId))]
    public virtual TDNMachinery TDNMachinery { get; set; } = null!;

    public int OrderNo { get; set; } = 1;

    [StringLength(100)]
    public string? Classification { get; set; }

    [StringLength(200)]
    public string? KindOfMachinery { get; set; }

    [StringLength(200)]
    public string? BrandModel { get; set; }

    [StringLength(100)]
    public string? Capacity { get; set; }

    public DateTime? AcquiredDate { get; set; }

    [StringLength(100)]
    public string? ConditionWhenAcquired { get; set; } = "NEW";

    public int? EconomicLifeEstimated { get; set; }

    public int? RemainingEconomicLife { get; set; }

    public int? YearInstalled { get; set; }

    public int? YearOfOperation { get; set; }

    public DateTime? DateOfInstallation { get; set; }

    public DateTime? DateOfOperation { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OriginalCost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal InstallationCost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalCost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RCN { get; set; }

    public int? NoOfYearsUsed { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal MarketValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DepreciationTotalPercentage { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DepreciationTotalValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DepreciatedValue { get; set; }
}
