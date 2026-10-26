using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

public enum LandHoldingRequestStatus
{
    [Display(Name = "Pending Verification")]
    Pending,

    [Display(Name = "Processing Certificate")]
    Processing,

    [Display(Name = "Approved & Issued")]
    Approved,

    [Display(Name = "Returned / Disapproved")]
    Rejected
}

public class LandHoldingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Tracking No.")]
    public string TrackingNumber { get; set; } = string.Empty;

    [Display(Name = "Tax Declaration Reference")]
    public Guid? TaxDeclarationId { get; set; }

    [Required(ErrorMessage = "Owner / Taxpayer Name is required.")]
    [Display(Name = "Owner / Taxpayer Name")]
    public string OwnerName { get; set; } = string.Empty;

    [Display(Name = "Spouse Name")]
    public string? SpouseName { get; set; }

    [Display(Name = "Contact No.")]
    public string? ContactNo { get; set; }

    [Required(ErrorMessage = "Tax Declaration Number (TDN) is required.")]
    [Display(Name = "TD No.")]
    public string? TaxDeclarationNumber { get; set; }

    [Display(Name = "PIN / Property Index Number")]
    public string? PropertyIndexNumber { get; set; }

    [Display(Name = "Location of Property")]
    public string? PropertyLocation { get; set; }

    // General Information
    [Display(Name = "Requester Name")]
    public string? RequesterName { get; set; }

    [Display(Name = "O.R. No.")]
    public string? ORNumber { get; set; }

    [Display(Name = "Date Issued")]
    public DateTime? ORDateIssued { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Purpose is required.")]
    [Display(Name = "Purpose/s")]
    public string? Purposes { get; set; }

    // Requirements Submitted
    [Display(Name = "ID of Owner")]
    public bool ReqIdOfOwner { get; set; }

    [Display(Name = "ID of Representative")]
    public bool ReqIdOfRepresentative { get; set; }

    [Display(Name = "Authorization Letter / SPA")]
    public bool ReqAuthorizationLetter { get; set; }

    [Display(Name = "Deed of Conveyance")]
    public bool ReqDeedOfConveyance { get; set; }

    [Display(Name = "Certificate of Indigency")]
    public bool ReqCertIndigency { get; set; }

    // Request Certificate Option/s
    [Display(Name = "Certificate of Total Landholdings")]
    public bool CertTotalLandholdings { get; set; }

    [Display(Name = "Certification Type of Tax Declaration")]
    public bool CertTypeTaxDeclaration { get; set; }

    [Display(Name = "Certificate of Aggregate Agricultural Landholdings")]
    public bool CertAggregateAgriLandholdings { get; set; }

    [Display(Name = "Certification w/ No Improvement (From Time of Death)")]
    public bool CertNoImprovementFromDeath { get; set; }

    [Display(Name = "Certificate of No Landholdings")]
    public bool CertNoLandholdings { get; set; }

    [Display(Name = "Property History")]
    public bool CertPropertyHistory { get; set; }

    [Display(Name = "Others (Specify)")]
    public string? CertOthersSpecify { get; set; }

    // Document To Be Requested Table (JSON & Fee Computation)
    [Display(Name = "Requested Documents JSON")]
    public string? RequestedDocumentsJson { get; set; }

    [Display(Name = "Total Fees Amount (₱)")]
    public decimal TotalFeesAmount { get; set; }

    [Display(Name = "Status")]
    public LandHoldingRequestStatus Status { get; set; } = LandHoldingRequestStatus.Pending;

    // File Attachments
    [ValidateNever]
    public string? AttachmentFileName { get; set; }

    [ValidateNever]
    public string? AttachmentFileSize { get; set; }

    [ValidateNever]
    public string? PhysicalStoragePath { get; set; }

    [ValidateNever]
    public string? WebRelativePath { get; set; }

    [Display(Name = "Date Submitted")]
    public DateTime RequestDate { get; set; } = DateTime.Now;

    [Display(Name = "Last Update Date")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;
}
