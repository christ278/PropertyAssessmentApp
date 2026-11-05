using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

/// <summary>
/// Real Property Tax (RPT) Reports & Certifications Directory
/// </summary>
[Table("RPTReports")]
public class RPTReport
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "Report ID")]
    public int RPTReportId { get; set; }

    [Required]
    [StringLength(250)]
    [Display(Name = "Report Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(100)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; } = "System";

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(100)]
    [Display(Name = "Updated By")]
    public string? UpdatedBy { get; set; }

    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }
}
