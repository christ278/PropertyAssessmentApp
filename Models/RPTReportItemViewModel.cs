using System;
using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

/// <summary>
/// ViewModel representing an RPT Report item with user authorization status
/// </summary>
public class RPTReportItemViewModel
{
    public int RPTReportId { get; set; }

    [Display(Name = "Report Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }

    [Display(Name = "Updated By")]
    public string? UpdatedBy { get; set; }

    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Indicates whether the currently logged-in user's role has permission to view/generate this report
    /// </summary>
    [Display(Name = "Access Permission")]
    public bool HasAccess { get; set; }
}
