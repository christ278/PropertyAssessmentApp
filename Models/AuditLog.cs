using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(120)]
    [Display(Name = "Entity / Module")]
    public string EntityName { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Record ID")]
    public string RecordId { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Display(Name = "Action")]
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted

    [StringLength(100)]
    [Display(Name = "Changed By (Username)")]
    public string? ChangedBy { get; set; }

    [StringLength(150)]
    [Display(Name = "Changed By (Full Name)")]
    public string? ChangedByName { get; set; }

    [StringLength(64)]
    [Display(Name = "IP Address")]
    public string? IpAddress { get; set; }

    [Display(Name = "Timestamp")]
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    [Display(Name = "Changed Fields (JSON)")]
    public string? ChangedColumnsJson { get; set; }

    [Display(Name = "Previous Values (JSON)")]
    public string? OldValuesJson { get; set; }

    [Display(Name = "New Values (JSON)")]
    public string? NewValuesJson { get; set; }
}
