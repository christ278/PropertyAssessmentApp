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
    public string? PropertyTypeAccordUse { get; set; }
    public string? OtherPropertyTypeAccordUse { get; set; }
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
    public string? RegistrationStatus { get; set; }
    public string? TitleType { get; set; }
    public DateTime? DateRegistered { get; set; }
    public string? Phase { get; set; }
    public string? Location { get; set; }
    public string? SurveyorName { get; set; }
    public string? SurveyNo { get; set; }
    public string? PropLocationStreet { get; set; }
    public string? TransactionCode { get; set; }
    public string? TrackingNo { get; set; }
    public long? FormWorkflowId { get; set; }

    // Partition Support
    public int? PartitionItemIndex { get; set; }
    public string? PartitionIndex { get; set; }

    // Extended CAST Fields (Location, Basis & Back Taxes)
    public string? DistrictOrBarangay { get; set; }
    public string? Purok { get; set; }
    public string? CityMunicipality { get; set; }
    public string? Province { get; set; }
    public string? OwnerTIN { get; set; }
    public string? OwnerContactNo { get; set; }
    public string? OwnerEmail { get; set; }
    public string? OwnerGender { get; set; }
    public string? OwnerCivilStatus { get; set; }
    public bool IsAdministrator { get; set; }
    public bool IsBeneficialUser { get; set; }
    public bool IsPossessor { get; set; }
    public string? AdministratorName { get; set; }
    public string? AdministratorAddress { get; set; }
    public string? AdministratorContactNo { get; set; }
    public string? AdministratorTin { get; set; }
    public string? AdministratorEmail { get; set; }
    public string? AdministratorGender { get; set; }
    public string? AssessmentBasis { get; set; }
    public int? BackTaxesYearFrom { get; set; }
    public int? BackTaxesYearTo { get; set; }
    public decimal? UnitValue { get; set; }
    public string? LandAppraisalDetailsJson { get; set; }
    public string? LandImprovementsJson { get; set; }
    public string? LandAdjustmentsJson { get; set; }
    public string? LandPropertyAssessmentsJson { get; set; }
    public string? FloorBreakdownJson { get; set; }
    public string? AdditionalItemsJson { get; set; }
    public string? MachineryAppraisalDetailsJson { get; set; }
    public string? MachineryPropertyAssessmentsJson { get; set; }
    public string? Classification { get; set; }
    public string? SubClassification { get; set; }
    public string? ActualUse { get; set; }
    public decimal? AssessmentLevelPercent { get; set; }

    // Floor Plan / Site Sketch File & Attachments
    public string? FloorPlanSketchPath { get; set; }
    // Mobile-captured sketch, sent only during bulk sync (saved to disk by the form update endpoints)
    public string? FloorPlanSketchBase64 { get; set; }
    public string? FloorPlanSketchFileName { get; set; }
    public string? AttachmentsJson { get; set; }
    public List<MobileAttachmentDTO>? Attachments { get; set; } = new();

    // Client-side mapping flag
    public bool isAlreadyExist { get; set; }
    public string? MigratedBy { get; set; }
    public DateTime? MigratedDate { get; set; }
}
