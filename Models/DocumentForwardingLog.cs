using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public enum DivisionType
{
    [Display(Name = "Division 1: Tax Declaration & Land Holding Division")]
    Division1_TaxAssessmentAndLandHolding = 1,

    [Display(Name = "Division 2: GIS Tax Mapping Division")]
    Division2_GIS_TaxMapping = 2,

    [Display(Name = "Division 3: Records & Certification Division")]
    Division3_RecordsAndCertification = 3
}

public enum WorkflowStage
{
    [Display(Name = "Deferred / On-Hold")]
    Deferred = 0,

    // Division 1: Appraisal & Assessment
    [Display(Name = "Stage 1: Receiving")]
    ClerkReceiving = 1,

    [Display(Name = "Stage 2: Replotting")]
    AppraiserReview = 2,
    Replotting = 2,

    [Display(Name = "Stage 3: Inspection")]
    MapperVerification = 3,
    Inspection = 3,

    [Display(Name = "Stage 4: Check and Review")]
    CheckAndReview = 4,

    [Display(Name = "Stage 5: Repinning")]
    Repinning = 5,

    [Display(Name = "Stage 6: Recommending Approval")]
    RecommendingApproval = 6,

    [Display(Name = "Stage 7: Valuation & Examination")]
    ValuationAndExamination = 7,

    [Display(Name = "Stage 8: Assessor Head Approval")]
    AssessorApproval = 8,

    [Display(Name = "Stage 9: Printing")]
    Printing = 9,

    [Display(Name = "Stage 10: Releasing")]
    Releasing = 10,

    // Division 2: GIS & Tax Mapping
    [Display(Name = "Stage 15: Receiving (GIS)")]
    ReceivingGIS = 15,

    [Display(Name = "Stage 16: Replotting (GIS)")]
    ReplottingGIS = 16,

    [Display(Name = "Stage 17: Preparation (GIS)")]
    PreparationGIS = 17,

    [Display(Name = "Stage 18: Approval (GIS)")]
    ApprovalGIS = 18,

    [Display(Name = "Stage 19: Releasing (GIS)")]
    ReleasingGIS = 19,

    // Division 3: Records & Certification
    [Display(Name = "Stage 25: Receiving (Records)")]
    ReceivingRecords = 25,

    [Display(Name = "Stage 26: Replotting (Records)")]
    ReplottingRecords = 26,

    [Display(Name = "Stage 27: Preparation (Records)")]
    PreparationRecords = 27,

    [Display(Name = "Stage 28: Verification (Records)")]
    VerificationRecords = 28,

    [Display(Name = "Stage 29: Approval (Records)")]
    ApprovalRecords = 29,

    [Display(Name = "Stage 30: Releasing (Records)")]
    ReleasingRecords = 30,

    // Terminal / Completed States
    [Display(Name = "Completed / Issued")]
    Completed = 999,

    [Display(Name = "Returned / Needs Revision")]
    Returned = 1000
}

public class DocumentForwardingLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Division")]
    public DivisionType Division { get; set; }

    [Required]
    [Display(Name = "Request Type")]
    public string RequestType { get; set; } = string.Empty; // "TaxDeclarationRequest", "LandHoldingRequest", "TaxMappingRequest"

    [Required]
    [Display(Name = "Request ID")]
    public Guid RequestId { get; set; }

    [Required]
    [Display(Name = "Tracking Number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [Display(Name = "From Stage")]
    public WorkflowStage FromStage { get; set; }

    [Display(Name = "Target Stage")]
    public WorkflowStage ToStage { get; set; }

    [Display(Name = "Forwarded By")]
    public string ForwardedByName { get; set; } = string.Empty;

    [Display(Name = "Sender Role")]
    public string ForwardedByRole { get; set; } = string.Empty;

    [Display(Name = "Assigned / Destination Role")]
    public string AssignedToRole { get; set; } = string.Empty;

    [Display(Name = "Action Taken")]
    public string ActionTaken { get; set; } = "Forwarded"; // "Forwarded", "Returned", "Approved", "Disapproved"

    [Display(Name = "Routing Remarks / Instruction")]
    public string? Remarks { get; set; }

    [Display(Name = "Claimed / Assigned User ID")]
    public Guid? AssignedUserId { get; set; }

    [Display(Name = "Claimed / Assigned Staff Name")]
    public string? AssignedUserName { get; set; }

    [Display(Name = "Is Claimed Status")]
    public bool IsClaimed { get; set; } = false;

    [Display(Name = "Is Deferred Status")]
    public bool IsDeferred { get; set; } = false;

    [Display(Name = "Is Tagged For Approval")]
    public bool IsForApproval { get; set; } = false;

    [Display(Name = "Forwarding Date & Time")]
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
