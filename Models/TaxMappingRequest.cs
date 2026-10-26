using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

public enum TaxMappingRequestStatus
{
    [Display(Name = "Pending Verification")]
    Pending,

    [Display(Name = "Under Technical Review")]
    UnderReview,

    [Display(Name = "Field Survey In Progress")]
    SurveyInProgress,

    [Display(Name = "Approved & Map Updated")]
    Approved,

    [Display(Name = "Returned / Disapproved")]
    Rejected
}

public class TaxMappingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Request Reference No.")]
    public string RequestNumber { get; set; } = string.Empty;

    [Display(Name = "Tax Declaration Reference")]
    public Guid? TaxDeclarationId { get; set; }

    [Display(Name = "Owner / Applicant ID")]
    public Guid? OwnerId { get; set; }

    [Required(ErrorMessage = "Owner/Applicant Name is required.")]
    [Display(Name = "Owner / Taxpayer Name")]
    public string ApplicantName { get; set; } = string.Empty;

    [Display(Name = "Owner Contact Number")]
    public string? OwnerContactNo { get; set; }

    [Required(ErrorMessage = "Property Index No. (PIN) is required.")]
    [Display(Name = "Property Index No. (PIN)")]
    public string PropertyIndexNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tax Declaration No. (TDN) is required.")]
    [Display(Name = "Tax Declaration No. (TDN)")]
    public string TaxDeclarationNumber { get; set; } = string.Empty;

    // Requester & Receipt Details
    [Display(Name = "Requester Name")]
    public string? RequesterName { get; set; }

    [Display(Name = "O.R. No.")]
    public string? ORNumber { get; set; }

    [Display(Name = "Date Issued")]
    public DateTime? ORDateIssued { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Specific Purpose is required.")]
    [Display(Name = "Specific Purpose/s")]
    public string? Purposes { get; set; }

    // * REQUIREMENTS SUBMITTED
    [Display(Name = "ID of Owner")]
    public bool ReqIdOfOwner { get; set; }

    [Display(Name = "Authorization Letter / SPA")]
    public bool ReqAuthorizationLetter { get; set; }

    [Display(Name = "Certificate of Indigency")]
    public bool ReqCertIndigency { get; set; }

    [Display(Name = "ID of Representative")]
    public bool ReqIdOfRepresentative { get; set; }

    [Display(Name = "Deed of Conveyance")]
    public bool ReqDeedOfConveyance { get; set; }

    // * REQUEST CERTIFICATE OPTION/S
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

    [Display(Name = "Cert. of Road Right of Way")]
    public bool CertRoadRightOfWay { get; set; }

    [Display(Name = "Cert. of No Road Right of Way")]
    public bool CertNoRoadRightOfWay { get; set; }

    [Display(Name = "Tax Map")]
    public bool CertTaxMap { get; set; }

    [Display(Name = "Image Map")]
    public bool CertImageMap { get; set; }

    [Display(Name = "Property Identification Verification")]
    public bool CertPropertyIdVerification { get; set; }

    [Display(Name = "Field Inspection")]
    public bool CertFieldInspection { get; set; }

    [Display(Name = "Others (Specify Below)")]
    public bool CertOthers { get; set; }

    [Display(Name = "Other Requests Specification")]
    public string? CertOthersSpecification { get; set; }

    // Document To Be Requested Table (JSON & Fee Computation)
    [Display(Name = "Requested Documents JSON")]
    public string? RequestedDocumentsJson { get; set; }

    [Display(Name = "Total Fees Amount (₱)")]
    public decimal TotalFeesAmount { get; set; }

    [Display(Name = "Province")]
    public Guid? ProvinceId { get; set; }
    [ValidateNever]
    public virtual Province? Province { get; set; }

    [Display(Name = "City / Municipality")]
    public Guid? CityMunicipalityId { get; set; }
    [ValidateNever]
    public virtual CityMunicipality? CityMunicipality { get; set; }

    [Display(Name = "Barangay")]
    public Guid? BarangayId { get; set; }
    [ValidateNever]
    public virtual Barangay? Barangay { get; set; }

    [ValidateNever]
    public string ProvinceName { get; set; } = "South Cotabato";

    [ValidateNever]
    public string CityMunicipalityName { get; set; } = "General Santos City";

    [ValidateNever]
    public string BarangayName { get; set; } = "Brgy. Central";

    [Required(ErrorMessage = "Street Address is required.")]
    [Display(Name = "Street / Lot Address")]
    public string StreetAddress { get; set; } = string.Empty;

    [Display(Name = "Request Status")]
    public TaxMappingRequestStatus Status { get; set; } = TaxMappingRequestStatus.Pending;

    [Display(Name = "Survey / Parcel Area (sqm)")]
    public decimal ParcelAreaSqm { get; set; }

    [Display(Name = "Assigned Tax Mapper Staff")]
    public string AssignedStaff { get; set; } = "Fernando K. Valdez (Tax Mapper)";

    // File Upload & Storage Metadata (Configured Location Path)
    [ValidateNever]
    [Display(Name = "Attachment File Name")]
    public string? AttachmentFileName { get; set; }

    [ValidateNever]
    [Display(Name = "File Size")]
    public string? AttachmentFileSize { get; set; }

    [ValidateNever]
    [Display(Name = "Physical Storage Location Path")]
    public string? PhysicalStoragePath { get; set; }

    [ValidateNever]
    [Display(Name = "Web Relative URL Path")]
    public string? WebRelativePath { get; set; }

    [Display(Name = "Date Submitted")]
    public DateTime RequestDate { get; set; } = DateTime.Now;

    [Display(Name = "Last Update Date")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [ValidateNever]
    public string CompleteLocationAddress => $"{StreetAddress}, {BarangayName}, {CityMunicipalityName}, {ProvinceName}".Trim(',', ' ');
}
