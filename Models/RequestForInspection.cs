using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

/// <summary>
/// 10-stage inspection workflow statuses matching CAST RequestForInspectionStatus
/// </summary>
public enum RequestForInspectionStatus
{
    [Display(Name = "Deferred / Returned")]
    Deferred = 0,

    [Display(Name = "1. Receiving")]
    Receiving = 1,

    [Display(Name = "2. Replotting")]
    Replotting = 2,

    [Display(Name = "3. Field Inspection")]
    Inspection = 3,

    [Display(Name = "4. Check and Review")]
    CheckAndReview = 4,

    [Display(Name = "5. Repinning")]
    Repinning = 5,

    [Display(Name = "6. Recommending Approval")]
    RecommendingApproval = 6,

    [Display(Name = "7. Valuation & Examination")]
    ValuationAndExamination = 7,

    [Display(Name = "8. Approval")]
    Approval = 8,

    [Display(Name = "9. Printing")]
    Printing = 9,

    [Display(Name = "10. Releasing")]
    Releasing = 10
}

public class RequestForInspection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Tracking No.")]
    public string TrackingNumber { get; set; } = string.Empty;

    [Display(Name = "Transaction Code")]
    public string? TransactionCode { get; set; }

    [Display(Name = "Transaction No.")]
    public string? TransactionNo { get; set; }

    // * INSPECTION PURPOSE / REASONS (MATCHING CAST)
    [Display(Name = "Depreciation")]
    public bool IsDepreciation { get; set; }

    [Display(Name = "Reassessment")]
    public bool IsReassessment { get; set; }

    [Display(Name = "Cancellation")]
    public bool IsCancelled { get; set; }

    [Display(Name = "Other Purposes")]
    public bool IsOther { get; set; }

    [Display(Name = "Specific Purpose Details")]
    public string? OtherPurposeDetails { get; set; }

    // * REFERENCE PROPERTY DETAILS
    [Display(Name = "Reference Tax Declaration")]
    public Guid? TaxDeclarationId { get; set; }

    [Required(ErrorMessage = "Tax Declaration No. (TDN/ARP) is required.")]
    [Display(Name = "Tax Declaration No. (TDN / ARP)")]
    public string TaxDeclarationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Property Index No. (PIN) is required.")]
    [Display(Name = "Property Index No. (PIN)")]
    public string PropertyIndexNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Registered Owner Name is required.")]
    [Display(Name = "Registered Owner Name")]
    public string OwnerName { get; set; } = string.Empty;

    [Display(Name = "Property Kind")]
    public string Kind { get; set; } = "Land"; // Land, Building, Machinery

    [Display(Name = "Lot No. / Survey No.")]
    public string? LotNo { get; set; } = "N/A";

    [Display(Name = "Property Location Address")]
    public string? PropertyLocation { get; set; } = "N/A";

    // * CONTACT & REPRESENTATIVE DETAILS
    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; }

    [Display(Name = "Contact / Mobile No.")]
    public string? ContactNumber { get; set; }

    [Display(Name = "Email Address")]
    public string? EmailAddress { get; set; }

    [Display(Name = "Authorized Representative")]
    public string? Representative { get; set; }

    // * ORDER OF PAYMENT & BILLING
    [Display(Name = "Requested Documents JSON")]
    public string? RequestedDocumentsJson { get; set; }

    [Display(Name = "Total Fees Amount (₱)")]
    public decimal TotalFeesAmount { get; set; }

    [Display(Name = "Official Receipt No.")]
    public string? ORNumber { get; set; }

    [Display(Name = "OR Date Issued")]
    public DateTime? ORDateIssued { get; set; } = DateTime.Now;

    // * FILE ATTACHMENTS (ROUTED TO CAST INSPECTION LOCATION ~/UploadFile/Inspection)
    [ValidateNever]
    public string? AttachmentFileName { get; set; }

    [ValidateNever]
    public string? AttachmentFileSize { get; set; }

    [ValidateNever]
    public string? PhysicalStoragePath { get; set; }

    [ValidateNever]
    public string? WebRelativePath { get; set; }

    // * WORKFLOW STATUS & TIMESTAMPS
    [Display(Name = "Inspection Workflow Status")]
    public RequestForInspectionStatus Status { get; set; } = RequestForInspectionStatus.Receiving;

    [Display(Name = "Current Assigned Staff")]
    public string? AssignedStaff { get; set; }

    [Display(Name = "Technical Remarks")]
    public string? Remarks { get; set; }

    [Display(Name = "Date Filed")]
    public DateTime RequestDate { get; set; } = DateTime.Now;

    [Display(Name = "Last Updated")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;
}
