using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

public enum PropertyTagType
{
    Mortgage = 1,
    Bailbond = 2,
    Release = 3,
    Cancellation = 4
}

public class PropertyTagging
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TaxDeclarationId { get; set; }

    [ForeignKey(nameof(TaxDeclarationId))]
    [ValidateNever]
    public virtual TaxDeclaration? TaxDeclaration { get; set; }

    [Required]
    [MaxLength(100)]
    public string TaxDeclarationNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PropertyIndexNumber { get; set; }

    [MaxLength(255)]
    public string? OwnerName { get; set; }

    [Required]
    public PropertyTagType TagType { get; set; } = PropertyTagType.Mortgage;

    // Mortgage ("Is Mortgage To") / Bailbond ("Is Bailbond To")
    [MaxLength(255)]
    public string? PartyName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    public DateTime? TagDate { get; set; }

    // Release fields
    [MaxLength(100)]
    public string? PerDocNo { get; set; }

    [MaxLength(50)]
    public string? PageNo { get; set; }

    [MaxLength(50)]
    public string? BookNo { get; set; }

    [MaxLength(50)]
    public string? SeriesOf { get; set; }

    public Guid? PostedByUserId { get; set; }

    [MaxLength(200)]
    public string? PostedByName { get; set; }

    // Cancellation fields
    [MaxLength(500)]
    public string? ReasonForCancellation { get; set; }

    public DateTime? DateCancelled { get; set; }

    [MaxLength(150)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
