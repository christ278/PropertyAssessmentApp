using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PropertyAssessmentApp.Models;

/// <summary>
/// Model for generating a Real Property Tax (RPT) Report / Certification
/// Requires TDN No. and an authorized Report Type
/// </summary>
public class GenerateRPTReportViewModel
{
    [Required(ErrorMessage = "Tax Declaration Number (TDN) is required.")]
    [Display(Name = "Tax Declaration (TDN) No.")]
    public string TaxDeclarationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a Report Type.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Report Type.")]
    [Display(Name = "Report / Certification Type")]
    public int? RPTReportId { get; set; }

    [Display(Name = "Purpose of Certification")]
    public string? Purpose { get; set; } = "Official LGU Assessment / Legal Verification";

    [Display(Name = "Requestor / Authorized Representative")]
    public string? RequestedBy { get; set; }

    [Display(Name = "Official Receipt (O.R.) No.")]
    public string? ORNumber { get; set; }

    /// <summary>
    /// Dropdown list containing only the reports the current logged-in user has permission to access
    /// </summary>
    public List<SelectListItem> AccessibleReports { get; set; } = new();
}

/// <summary>
/// Detailed result model passed to the generated certificate view
/// </summary>
public class RPTReportResultViewModel
{
    public RPTReport Report { get; set; } = null!;
    public TaxDeclaration? TaxDeclaration { get; set; }
    public string TaxDeclarationNumber { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string? RequestedBy { get; set; }
    public string? ORNumber { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public string GeneratedBy { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Related properties for multi-property holdings (BIR / DAR certifications)
    /// </summary>
    public List<TaxDeclaration> RelatedProperties { get; set; } = new();
}
