using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

[Table("TDNPropertyImages")]
public class TDNPropertyImage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TaxDeclarationId { get; set; }

    [ForeignKey(nameof(TaxDeclarationId))]
    [ValidateNever]
    public virtual TaxDeclaration? TaxDeclaration { get; set; }

    [MaxLength(100)]
    [Display(Name = "Tax Declaration Number (TDN)")]
    public string TaxDeclarationNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Property Kind")]
    public string PropertyKind { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Display(Name = "Image File Path")]
    public string ImagePath { get; set; } = string.Empty;

    [MaxLength(255)]
    [Display(Name = "Original File Name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [MaxLength(255)]
    [Display(Name = "Caption / Remarks")]
    public string? Caption { get; set; }

    public int SortOrder { get; set; } = 1;

    public DateTime UploadedAt { get; set; } = DateTime.Now;
}
