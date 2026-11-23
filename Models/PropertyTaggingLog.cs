using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class PropertyTaggingLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaxDeclarationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string TaxDeclarationNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PropertyIndexNumber { get; set; }

    [MaxLength(255)]
    public string? OwnerName { get; set; }

    [Required]
    [MaxLength(50)]
    public string TagType { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Details { get; set; }

    [Required]
    [MaxLength(150)]
    public string Source { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? PerformedBy { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
}
