using System;
using System.Collections.Generic;

namespace PropertyAssessmentApp.DTOs;

public class ApiResponseDTO<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<T> Data { get; set; } = new();
}

public class TDNDTO
{
    public long TDNId { get; set; }
    public long? RPTIN { get; set; }
    public string? LGUCode { get; set; }
    public string? DistrictMunicipalityCode { get; set; }
    public string? BarangayCode { get; set; }
    public string? SectionNo { get; set; }
    public string? ParcelNo { get; set; }
    public string? PostScript { get; set; }
    public string? MultipleOwnerTag { get; set; }
    public bool? IsMultiClaimants { get; set; }
    public bool? IsNotSameOwnerWithBuilding { get; set; }
    public int? BarangayId { get; set; }
    public int? OwnerId { get; set; }
    public int RevisionYear { get; set; }
    public string? DeclarationNo { get; set; }
    public string? TDN { get; set; }
    public string? TDN1 { get; set; }
    public string? ARPNo { get; set; }
    public string? PIN { get; set; }
    public string? DisplayName { get; set; }
    public string? OwnerAddress { get; set; }
    public string? PropAdminName { get; set; }
    public string? PropAdminAddress { get; set; }
    public string? PropAdminContactNo { get; set; }
    public bool? IsPending { get; set; }
    public bool? IsApproved { get; set; }
    public DateTime? DateApproved { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? DateCancelled { get; set; }
    public bool? IsReleased { get; set; }
    public bool? IsMortgaged { get; set; }
    public bool? IsBailbonded { get; set; }
    public bool? IsUnderCourtLetigation { get; set; }
    public byte? TDNModeId { get; set; }
    public byte? PropertyTypeId { get; set; }
    public string? Kind { get; set; } // Land, Building, Machinery
    public bool? IsVOS { get; set; }
    public int? ClassificationId { get; set; }
    public int? ActualUseId { get; set; }
    public string? ProvincialAssessorName { get; set; }
    public string? CityMunAssessorName { get; set; }
    public DateTime? DateApprovedByAssessor { get; set; }
    public string? RecomApprovalName { get; set; }
    public string? RecommendingApprovalName { get; set; }
    public DateTime? DateRecommendedApproval { get; set; }
    public string? AppraiseAssessedBy { get; set; }
    public DateTime? DateAppraised { get; set; }
    public string? PrevOwner { get; set; }
    public int? YearEffectivity { get; set; }
    public int? QuarterEffectivity { get; set; }
    public DateTime? DateRegistration { get; set; }
    public string? Memoranda { get; set; }
    public decimal? TotalMarketValue { get; set; }
    public decimal? TotalTaxableMarketValue { get; set; }
    public decimal? TotalAssessedValue { get; set; }
    public decimal? TotalTaxableAssessedValue { get; set; }
    public decimal? TotalArea { get; set; }
    public string? Unit { get; set; }
    public string? TitleNo { get; set; }
    public bool Taxability { get; set; } = true;
    public bool? Exempted { get; set; }
    public string? LotNo { get; set; }
    public string? CadastralSurveyNo { get; set; }
    public string? CLOANo { get; set; }
    public string? TCTNo { get; set; }
    public DateTime? TCTDate { get; set; }
    public string? OCTNo { get; set; }
    public string? BlockNo { get; set; }
    public string? Phase { get; set; }
    public string? Location { get; set; }
    public string? SurveyorName { get; set; }

    // Client-side mapping flag
    public bool isAlreadyExist { get; set; }
    public string? MigratedBy { get; set; }
    public DateTime? MigratedDate { get; set; }
}
