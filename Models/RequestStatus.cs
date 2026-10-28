using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

/// <summary>
/// Overall request status (Pending, Under Review, etc.) stored on a request record.
/// This is separate from the transaction-flow enums (e.g. TaxMappingRequestStatus),
/// which represent workflow stage IDs.
/// </summary>
public enum RequestStatus
{
    [Display(Name = "Pending")]
    Pending = 0,

    [Display(Name = "Under Review")]
    UnderReview = 1,

    [Display(Name = "Approved")]
    Approved = 2,

    [Display(Name = "Rejected")]
    Rejected = 3
}
