using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

public enum TaxDeclarationRequestStatus
{
    [Display(Name = "Receiving (Records)")]
    Receiving= 25,

    [Display(Name = "Replotting (Records)")]
    Replotting = 26,

    [Display(Name = "Preparation (Records)")]
    Preparation = 27,

    [Display(Name = "Verification (Records)")]
    Verification = 28,

    [Display(Name = "Approval (Records)")]
    Approved = 29,

    [Display(Name = "Releasing (Records)")]
    Released = 30
}

public class TaxDeclarationRequest
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

    [Display(Name = "Contact No.")]
    public string? ContactNo { get; set; }

    [Required(ErrorMessage = "Tax Declaration Number (TDN) is required.")]
    [Display(Name = "TD No.")]
    public string? TaxDeclarationNumber { get; set; }

    [Display(Name = "Lot No.")]
    public string? LotNo { get; set; }

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
    [Display(Name = "Tax Declaration")]
    public bool CertTaxDeclaration { get; set; }

    [Display(Name = "Records Verification")]
    public bool CertRecordsVerification { get; set; }

    [Display(Name = "Tax Declaration (From the Time of Death)")]
    public bool CertTaxDecTimeOfDeath { get; set; }

    [Display(Name = "Annotation of Bail Bond")]
    public bool CertAnnotationBailBond { get; set; }

    [Display(Name = "Certificate of Property History")]
    public bool CertPropertyHistory { get; set; }

    [Display(Name = "Annotation of Mortgages")]
    public bool CertAnnotationMortgages { get; set; }

    [Display(Name = "Certificate of No Improvement")]
    public bool CertNoImprovement { get; set; }

    [Display(Name = "Cancellation of Annotation")]
    public bool CertCancellationAnnotation { get; set; }

    [Display(Name = "Supporting Documents")]
    public bool CertSupportingDocuments { get; set; }

    // Document To Be Requested Table (JSON & Fee Computation)
    [Display(Name = "Requested Documents JSON")]
    public string? RequestedDocumentsJson { get; set; }

    [Display(Name = "Total Fees Amount (₱)")]
    public decimal TotalFeesAmount { get; set; }

    [Display(Name = "Status")]
    public TaxDeclarationRequestStatus Status { get; set; } = TaxDeclarationRequestStatus.Pending;

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
