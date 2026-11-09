using System;
using System.Collections.Generic;

namespace PropertyAssessmentApp.DTOs;

public class MobileLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class MobileForwardWorkflowRequest
{
    public long FormWorkflowId { get; set; }
    public int NextStatusId { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class MobileAttachmentDTO
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileType { get; set; } // "image" | "document"
    public string? FileSize { get; set; }
    public string? Base64Data { get; set; }
    public string? Caption { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}

public class MobilePartitionSummaryDTO
{
    public int Index { get; set; }
    public string PartitionIndex { get; set; } = "A";
    public string Kind { get; set; } = "Land";
    public int TransactionTypeId { get; set; } // 1: Land, 2: Building, 3: Machinery
    public string FormType { get; set; } = "LAND";
    public string? SystemGeneratedTDN { get; set; }
    public string? ARPNo { get; set; }
    public string? PIN { get; set; }
    public string? PropertyTypeAccordUse { get; set; }
    public string? OtherPropertyTypeAccordUse { get; set; }
    public string? SurveyNo { get; set; }
    public string? LotNo { get; set; }
    public string? TitleNo { get; set; }
    public string? SectionNo { get; set; }
    public string? OwnerName { get; set; }
    public decimal AreaSqMeters { get; set; }
    public decimal MarketValue { get; set; }
    public decimal AssessedValue { get; set; }
    public string? SketchUrl { get; set; }
    public int PhotoCount { get; set; }
}

public class TransactionWorkFlowDTO
{
    public long FormworkflowId { get; set; }
    public int TransactionTypeId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string ARPNo { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Requestor { get; set; } = string.Empty;
    public int? AcceptedById { get; set; }
    public string AcceptedBy { get; set; } = string.Empty;
    public string TrackingNo { get; set; } = string.Empty;
    public int CurrentStatusId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsDeffered { get; set; }
    public bool IsCheckout { get; set; }
    public DateTime SubmittedDate { get; set; }
    public string ForwarderBy { get; set; } = string.Empty;
    public int ForwarderById { get; set; }
    public long? TdnId { get; set; }
    public bool HasPartition { get; set; }
    public int PartitionCount { get; set; }
    public List<MobilePartitionSummaryDTO> Partitions { get; set; } = new();
}

public class CAST_TDNBuildingDTO
{
    public long CAST_TDNBuildingId { get; set; }
    public long TDNId { get; set; }
    public string? STREET { get; set; }
    public int? BarangayID { get; set; }
    public string? TCTNo { get; set; }
    public string? LotNo { get; set; }
    public string? SurveyNo { get; set; }
    public string? LandArea { get; set; }
    public string? ARPNo { get; set; }
    public byte? BuildingMakeId { get; set; }
    public DateTime? DateConstructed { get; set; }
    public byte? ClassificationId { get; set; }
    public DateTime? DateCompleted { get; set; }
    public string? PermitNo { get; set; }
    public DateTime? DateOccupied { get; set; }
    public int? Age { get; set; }
    public int? StoreyNo { get; set; }
    public decimal? AreaPerFloor { get; set; }
    public decimal? TotalbldgArea { get; set; }
    public decimal? SubtotalValCompArea { get; set; }
    public decimal? TotalAddItem { get; set; }
    public decimal? DepreciationRate { get; set; }
    public decimal? DepreciationAccumulated { get; set; }
    public decimal? DepreciationMarketval { get; set; }
    public decimal? TotalMarketVal { get; set; }
    public decimal? TotalAssessedVal { get; set; }
    public string? StucturalType { get; set; }
    public string? OtherStructure { get; set; }
    public string? PropertyTypeAccordUse { get; set; }
    public string? OtherPropertyTypeAccordUse { get; set; }
    public string? MailingAddress { get; set; }
    public string? TIN { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailAddress { get; set; }
    public string? TransactionCode { get; set; }

    // 17 Structural Parts & Materials (CAST Reference)
    public string? StructuralFoundation { get; set; }
    public string? StructuralColumns { get; set; }
    public string? StructuralBeams { get; set; }
    public string? StructuralTrussRoof { get; set; }
    public string? StructuralRoof { get; set; }
    public string? StructuralExteriorWalls { get; set; }
    public string? StructuralFlooring { get; set; }
    public string? DoorMain { get; set; }
    public string? Ceiling { get; set; }
    public string? StructuralWindows { get; set; }
    public string? StructuralStairs { get; set; }
    public string? StructuralPartitionWalls { get; set; }
    public string? StructuralFloorFinish { get; set; }
    public string? StructuralFinishFront { get; set; }
    public string? ElectricalSystem { get; set; }
    public string? ToiletAndBathroom { get; set; }
    public string? PlumbingSewer { get; set; }
    public string? BuildingClassification { get; set; }
    public string? OccupancyPermitNo { get; set; }
}

public class TDNBuildingDTO
{
    public TDNDTO? TDN { get; set; }
    public CAST_TDNBuildingDTO? CAST_TDNBuildingDTO { get; set; }
}

public class TDNLandDataDTO
{
    public long TDNLandId { get; set; }
    public long TDNId { get; set; }
    public string? NorthBoundary { get; set; }
    public string? Nremarks { get; set; }
    public string? SouthBoundary { get; set; }
    public string? Sremarks { get; set; }
    public string? EastBoundary { get; set; }
    public string? Eremarks { get; set; }
    public string? WestBoundary { get; set; }
    public string? Wremarks { get; set; }
}

public class TDNLandDTO
{
    public TDNDTO? TDN { get; set; }
    public TDNLandDataDTO? TDNLand { get; set; }
}

public class TDNMachineDataDTO
{
    public long TDNMachineId { get; set; }
    public long TDNId { get; set; }
    public string? LandOwner { get; set; }
    public string? LandOwnerPIN { get; set; }
    public string? BuildingOwner { get; set; }
    public string? BuildingOwnerPIN { get; set; }
}

public class TDNMachineDetailDTO
{
    public long TDNMachineDetailId { get; set; }
    public long TDNMachineId { get; set; }
    public string? KindOfMachinery { get; set; }
    public string? BrandModel { get; set; }
    public string? Capacity { get; set; }
    public DateTime? AcquiredDate { get; set; }
    public DateTime? DateOfInstallation { get; set; }
    public DateTime? DateOfOperation { get; set; }
    public string? ConditionWhenAcquired { get; set; }
    public int? EconomicLifeEstimated { get; set; }
    public decimal? OriginalCost { get; set; }
    public decimal? MarketValue { get; set; }
    public int? AssessmentLevel { get; set; }
    public decimal? AssessedValue { get; set; }
    public decimal? DepreciatedValue { get; set; }
    public int? DepreciationTotalPercentage { get; set; }
}

public class TDNMachineDTO
{
    public TDNDTO? TDN { get; set; }
    public TDNMachineDataDTO? TDNMachine { get; set; }
    public List<TDNMachineDetailDTO>? TDNMachineDetail { get; set; }
}
