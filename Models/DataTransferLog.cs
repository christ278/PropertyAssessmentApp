using System;
using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class DataTransferLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string TaxDeclarationNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string PropertyIndexNumber { get; set; } = string.Empty;

    [StringLength(250)]
    public string OwnerName { get; set; } = string.Empty;

    [StringLength(50)]
    public string PropertyKind { get; set; } = "Land";

    public decimal MarketValue { get; set; }

    public decimal AssessedValue { get; set; }

    [Required]
    [StringLength(150)]
    public string MigratedBy { get; set; } = string.Empty;

    [StringLength(100)]
    public string? MigratedByRole { get; set; }

    public DateTime MigratedAt { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string Status { get; set; } = "Success";

    public string? Remarks { get; set; }
}
