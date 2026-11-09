using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.DTOs;
using PropertyAssessmentApp.Filters;
using PropertyAssessmentApp.Helpers;
using PropertyAssessmentApp.Hubs;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers.Api;

[ApiController]
[Route("api/mobile")]
[JwtAuthorize]
public class MobileApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentWorkflowService _workflowService;
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly IWorkflowNotifier _workflowNotifier;
    private readonly IWebHostEnvironment _env;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter _mobileJsonFormatter =
        new(new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        });

    public override OkObjectResult Ok(object? value)
    {
        var result = base.Ok(value);
        result.Formatters.Clear();
        result.Formatters.Add(_mobileJsonFormatter);
        return result;
    }

    public MobileApiController(
        ApplicationDbContext context,
        IDocumentWorkflowService workflowService,
        IHubContext<WorkflowHub> hubContext,
        IWorkflowNotifier workflowNotifier,
        IWebHostEnvironment env)
    {
        _context = context;
        _workflowService = workflowService;
        _hubContext = hubContext;
        _workflowNotifier = workflowNotifier;
        _env = env;
    }

    // Helper: Generate a deterministic positive 64-bit integer from a Guid
    private static long GuidToNumericId(Guid guid)
    {
        byte[] bytes = guid.ToByteArray();
        long val = BitConverter.ToInt64(bytes, 0);
        return Math.Abs(val == 0 ? 1 : val);
    }

    // Strict check: Only Property Assessment and Field Inspection transactions are allowed on mobile
    private static bool IsAllowedMobileTransactionType(string? requestType)
    {
        if (string.IsNullOrWhiteSpace(requestType)) return false;
        return requestType.Equals("PropertyAssessmentRequest", StringComparison.OrdinalIgnoreCase) ||
               requestType.Equals("RequestForInspection", StringComparison.OrdinalIgnoreCase);
    }

    private static int ParsePropertyKindId(string? kind, int fallback = 2)
    {
        if (string.IsNullOrWhiteSpace(kind)) return fallback;
        if (kind.Contains("Land", StringComparison.OrdinalIgnoreCase)) return 1;
        if (kind.Contains("Machin", StringComparison.OrdinalIgnoreCase)) return 3;
        if (kind.Contains("Build", StringComparison.OrdinalIgnoreCase)) return 2;
        return fallback;
    }

    // =========================================================================
    // 1. DASHBOARD & TRANSACTION FEED (STRICTLY PROPERTY ASSESSMENT & FIELD INSPECTION)
    // =========================================================================

    // GET: /api/mobile/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var jwtUser = HttpContext.Items["JwtUser"] as JwtPayload;
        int divisionId = jwtUser?.DivisionId ?? 1;

        int pendingInspections = await _context.RequestForInspections
            .CountAsync(r => r.Status != RequestForInspectionStatus.Releasing && r.Status != RequestForInspectionStatus.Deferred);

        int pendingAssessments = await _context.PropertyAssessmentRequests
            .CountAsync(p => p.Status != PropertyAssessmentStatus.Releasing && p.Status != PropertyAssessmentStatus.Deferred);

        var today = DateTime.Today;
        int completedToday = await _context.DocumentForwardingLogs
            .CountAsync(l => l.Timestamp >= today &&
                             (l.RequestType == "PropertyAssessmentRequest" || l.RequestType == "RequestForInspection") &&
                             (l.ToStage == WorkflowStage.Releasing || l.ToStage == WorkflowStage.Completed || l.ActionTaken == "Approved"));

        return Ok(new
        {
            success = true,
            data = new
            {
                inspectorName = jwtUser?.Name ?? "Assessor Staff",
                divisionId = divisionId,
                pendingInspectionsCount = pendingInspections,
                pendingAssessmentsCount = pendingAssessments,
                completedTodayCount = completedToday,
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }
        });
    }

        private static List<MobilePartitionSummaryDTO> BuildPartitionSummaries(List<AssessmentPartitionItem> partitions, string defaultOwner)
    {
        var result = new List<MobilePartitionSummaryDTO>();
        for (int i = 0; i < partitions.Count; i++)
        {
            var p = partitions[i];
            int pKindId = ParsePropertyKindId(p.Kind, 1);
            string fType = pKindId switch { 1 => "LAND", 3 => "MACHINERY", _ => "BUILDING" };
            int pPhotoCount = 0;
            if (!string.IsNullOrWhiteSpace(p.AttachmentsJson))
            {
                try
                {
                    var atts = JsonSerializer.Deserialize<List<MobileAttachmentDTO>>(p.AttachmentsJson, _jsonOptions);
                    pPhotoCount = atts?.Count ?? 0;
                }
                catch { }
            }

            result.Add(new MobilePartitionSummaryDTO
            {
                Index = i,
                PartitionIndex = !string.IsNullOrWhiteSpace(p.PartitionIndex) ? p.PartitionIndex : ((char)('A' + i)).ToString(),
                Kind = !string.IsNullOrWhiteSpace(p.Kind) ? p.Kind : "Land",
                TransactionTypeId = pKindId,
                FormType = fType,
                SystemGeneratedTDN = p.SystemGeneratedTDN,
                ARPNo = !string.IsNullOrWhiteSpace(p.ARPNo) ? p.ARPNo : p.SystemGeneratedTDN,
                PIN = p.PIN,
                PropertyTypeAccordUse = p.PropertyTypeAccordUse,
                OtherPropertyTypeAccordUse = p.OtherPropertyTypeAccordUse,
                SurveyNo = p.SurveyNumber,
                LotNo = p.LotNumber,
                TitleNo = p.TitleNo,
                SectionNo = p.SectionNo,
                OwnerName = !string.IsNullOrWhiteSpace(p.NewOwnerName) ? p.NewOwnerName : defaultOwner,
                AreaSqMeters = p.AreaSqMeters > 0 ? p.AreaSqMeters : p.TotalBuildingAreaSqm,
                MarketValue = p.MarketValue,
                AssessedValue = p.AssessedValue,
                SketchUrl = p.BuildingFloorPlanSketchPath,
                PhotoCount = pPhotoCount
            });
        }
        return result;
    }

    private static List<MobileAttachmentDTO> ParseAttachments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<MobileAttachmentDTO>();
        try
        {
            return JsonSerializer.Deserialize<List<MobileAttachmentDTO>>(json, _jsonOptions) ?? new();
        }
        catch
        {
            return new List<MobileAttachmentDTO>();
        }
    }

    private static int ResolvePartitionTargetIndex(List<AssessmentPartitionItem> partitions, TDNDTO? dtoTdn)
    {
        if (dtoTdn == null || partitions.Count == 0) return 0;

        if (dtoTdn.PartitionItemIndex.HasValue && dtoTdn.PartitionItemIndex.Value >= 0 && dtoTdn.PartitionItemIndex.Value < partitions.Count)
        {
            return dtoTdn.PartitionItemIndex.Value;
        }

        if (!string.IsNullOrWhiteSpace(dtoTdn.PartitionIndex))
        {
            int idx = partitions.FindIndex(p => string.Equals(p.PartitionIndex, dtoTdn.PartitionIndex.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) return idx;
        }

        string? targetTdn = dtoTdn.ARPNo ?? dtoTdn.DeclarationNo ?? dtoTdn.TDN;
        if (!string.IsNullOrWhiteSpace(targetTdn))
        {
            int idx = partitions.FindIndex(p => string.Equals(p.SystemGeneratedTDN, targetTdn.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) return idx;
        }

        if (!string.IsNullOrWhiteSpace(dtoTdn.PIN))
        {
            int idx = partitions.FindIndex(p => string.Equals(p.PIN, dtoTdn.PIN.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) return idx;
        }

        return 0;
    }

    // GET: /api/mobile/transactions
    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(int? propertyTypeId = null, string trackingNo = "", int page = 1, int pageSize = 50)
    {
        var jwtUser = HttpContext.Items["JwtUser"] as JwtPayload;
        Guid.TryParse(jwtUser?.Uid, out var userId);

        // 1. Fetch personal queue for user and strictly filter to PropertyAssessmentRequest & RequestForInspection
        var queue = _workflowService.GetPersonalQueueForUser(userId)
            .Where(q => IsAllowedMobileTransactionType(q.RequestType))
            .ToList();

        // 2. If personal queue has no Property Assessment / Field Inspection items, fall back to Division 1 Pool
        if (queue.Count == 0)
        {
            queue = _workflowService.GetPoolQueueForDivision(DivisionType.Division1_TaxAssessmentAndLandHolding)
                .Where(q => IsAllowedMobileTransactionType(q.RequestType))
                .ToList();
        }

        // Filter by tracking number / owner / reference if specified
        if (!string.IsNullOrWhiteSpace(trackingNo))
        {
            string clean = trackingNo.Trim().ToLower();
            queue = queue.Where(q => (q.TrackingNumber ?? "").ToLower().Contains(clean) ||
                                     (q.OwnerOrApplicant ?? "").ToLower().Contains(clean) ||
                                     (q.ReferenceNumber ?? "").ToLower().Contains(clean)).ToList();
        }

        // Preload PropertyAssessmentRequests, RequestForInspections, and TaxDeclarations for accurate kind & TDN resolution
        var allRpas = await _context.PropertyAssessmentRequests
            .AsNoTracking()
            .Select(r => new { r.Id, r.TrackingNumber, r.Kind, r.RefTaxDeclarationId, r.PrevTDN, r.PrevPIN, r.NewOwnerName, r.PrevOwner, r.Requestor, r.RequestDate, r.GeneratedPartitionsJson })
            .ToListAsync();

        var allRfis = await _context.RequestForInspections
            .AsNoTracking()
            .Select(r => new { r.Id, r.TrackingNumber, r.Kind, r.TaxDeclarationId, r.TaxDeclarationNumber, r.PropertyIndexNumber, r.OwnerName, r.Representative, r.RequestDate, r.GeneratedPartitionsJson })
            .ToListAsync();

        var allTds = await _context.TaxDeclarations
            .AsNoTracking()
            .Select(t => new { t.Id, t.TaxDeclarationNumber, t.PropertyIndexNumber, t.Kind })
            .ToListAsync();

        var list = queue.Select(item =>
        {
            long numericWfId = GuidToNumericId(item.RequestId);
            int transType = 2; // 1: Land, 2: Building, 3: Machinery
            long? resolvedTdnId = null;
            string cleanRequestType = item.RequestType.Equals("RequestForInspection", StringComparison.OrdinalIgnoreCase)
                ? "Field Inspection"
                : "Property Assessment";
            string ownerName = item.OwnerOrApplicant;
            string requestorName = item.OwnerOrApplicant;
            string arpOrPinRef = item.ReferenceNumber;
            DateTime submittedDate = item.DateSubmitted;
            bool hasPart = false;
            int partitionCount = 0;
            var partitionSummaries = new List<MobilePartitionSummaryDTO>();

            if (item.RequestType.Equals("PropertyAssessmentRequest", StringComparison.OrdinalIgnoreCase))
            {
                var rpa = allRpas.FirstOrDefault(r => r.Id == item.RequestId);
                if (rpa != null)
                {
                    var partitions = PartitionJsonHelper.ParseList(rpa.GeneratedPartitionsJson);
                    var firstPart = partitions.FirstOrDefault();
                    hasPart = partitions.Count > 0;
                    partitionCount = partitions.Count;
                    partitionSummaries = BuildPartitionSummaries(partitions, ownerName);

                    ownerName = !string.IsNullOrWhiteSpace(rpa.NewOwnerName) ? rpa.NewOwnerName
                              : !string.IsNullOrWhiteSpace(rpa.PrevOwner) ? rpa.PrevOwner
                              : !string.IsNullOrWhiteSpace(firstPart?.NewOwnerName) ? firstPart!.NewOwnerName
                              : !string.IsNullOrWhiteSpace(rpa.Requestor) ? rpa.Requestor
                              : item.OwnerOrApplicant;

                    requestorName = !string.IsNullOrWhiteSpace(rpa.Requestor) ? rpa.Requestor : ownerName;
                    submittedDate = rpa.RequestDate != default ? rpa.RequestDate : item.DateSubmitted;

                    arpOrPinRef = !string.IsNullOrWhiteSpace(firstPart?.SystemGeneratedTDN) ? firstPart!.SystemGeneratedTDN
                                : !string.IsNullOrWhiteSpace(firstPart?.PIN) ? firstPart!.PIN
                                : !string.IsNullOrWhiteSpace(rpa.PrevTDN) ? rpa.PrevTDN
                                : !string.IsNullOrWhiteSpace(rpa.PrevPIN) ? rpa.PrevPIN
                                : string.Empty;

                    transType = ParsePropertyKindId(rpa.Kind, 2);
                    if (firstPart != null && !string.IsNullOrWhiteSpace(firstPart.Kind))
                    {
                        transType = ParsePropertyKindId(firstPart.Kind, transType);
                    }

                    var matchedTd = allTds.FirstOrDefault(t =>
                        (rpa.RefTaxDeclarationId.HasValue && t.Id == rpa.RefTaxDeclarationId.Value) ||
                        (firstPart != null && !string.IsNullOrWhiteSpace(firstPart.SystemGeneratedTDN) && t.TaxDeclarationNumber.Equals(firstPart.SystemGeneratedTDN, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(rpa.PrevTDN) && t.TaxDeclarationNumber.Equals(rpa.PrevTDN, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(rpa.PrevPIN) && t.PropertyIndexNumber.Equals(rpa.PrevPIN, StringComparison.OrdinalIgnoreCase)));

                    if (matchedTd != null)
                    {
                        resolvedTdnId = GuidToNumericId(matchedTd.Id);
                    }
                }
            }
            else if (item.RequestType.Equals("RequestForInspection", StringComparison.OrdinalIgnoreCase))
            {
                var rfi = allRfis.FirstOrDefault(r => r.Id == item.RequestId);
                if (rfi != null)
                {
                    var partitions = PartitionJsonHelper.ParseList(rfi.GeneratedPartitionsJson);
                    var firstPart = partitions.FirstOrDefault();
                    hasPart = partitions.Count > 0;
                    partitionCount = partitions.Count;
                    partitionSummaries = BuildPartitionSummaries(partitions, ownerName);

                    ownerName = !string.IsNullOrWhiteSpace(rfi.OwnerName) ? rfi.OwnerName
                              : !string.IsNullOrWhiteSpace(firstPart?.NewOwnerName) ? firstPart!.NewOwnerName
                              : !string.IsNullOrWhiteSpace(rfi.Representative) ? rfi.Representative
                              : item.OwnerOrApplicant;

                    requestorName = !string.IsNullOrWhiteSpace(rfi.Representative) ? rfi.Representative : ownerName;
                    submittedDate = rfi.RequestDate != default ? rfi.RequestDate : item.DateSubmitted;

                    arpOrPinRef = !string.IsNullOrWhiteSpace(firstPart?.SystemGeneratedTDN) ? firstPart!.SystemGeneratedTDN
                                : !string.IsNullOrWhiteSpace(firstPart?.PIN) ? firstPart!.PIN
                                : !string.IsNullOrWhiteSpace(rfi.TaxDeclarationNumber) ? rfi.TaxDeclarationNumber
                                : !string.IsNullOrWhiteSpace(rfi.PropertyIndexNumber) ? rfi.PropertyIndexNumber
                                : string.Empty;

                    transType = ParsePropertyKindId(rfi.Kind, 2);
                    if (firstPart != null && !string.IsNullOrWhiteSpace(firstPart.Kind))
                    {
                        transType = ParsePropertyKindId(firstPart.Kind, transType);
                    }

                    var matchedTd = allTds.FirstOrDefault(t =>
                        (rfi.TaxDeclarationId.HasValue && t.Id == rfi.TaxDeclarationId.Value) ||
                        (firstPart != null && !string.IsNullOrWhiteSpace(firstPart.SystemGeneratedTDN) && t.TaxDeclarationNumber.Equals(firstPart.SystemGeneratedTDN, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(rfi.TaxDeclarationNumber) && t.TaxDeclarationNumber.Equals(rfi.TaxDeclarationNumber, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(rfi.PropertyIndexNumber) && t.PropertyIndexNumber.Equals(rfi.PropertyIndexNumber, StringComparison.OrdinalIgnoreCase)));

                    if (matchedTd != null)
                    {
                        resolvedTdnId = GuidToNumericId(matchedTd.Id);
                    }
                }
            }

            return new TransactionWorkFlowDTO
            {
                FormworkflowId = numericWfId,
                TrackingNo = item.TrackingNumber,
                TransactionTypeId = transType,
                RequestType = cleanRequestType,
                ARPNo = arpOrPinRef,
                Owner = ownerName,
                Requestor = requestorName,
                AcceptedBy = item.AssignedUserName ?? "Staff",
                CurrentStatusId = (int)item.CurrentStage,
                CurrentStatus = item.CurrentStageName,
                Remarks = item.LastRemarks ?? string.Empty,
                IsComplete = item.CurrentStage == WorkflowStage.Releasing || item.CurrentStage == WorkflowStage.Completed,
                IsDeffered = item.IsDeferred,
                IsCheckout = item.IsClaimed,
                SubmittedDate = submittedDate,
                ForwarderBy = item.LastForwardedBy ?? "Office Staff",
                TdnId = resolvedTdnId,
                HasPartition = hasPart,
                PartitionCount = partitionCount,
                Partitions = partitionSummaries
            };
        }).ToList();

        // Filter by propertyTypeId if requested (1: Land, 2: Building, 3: Machinery)
        if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
        {
            list = list.Where(l => l.TransactionTypeId == propertyTypeId.Value).ToList();
        }

        int totalRecords = list.Count;
        var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            success = true,
            totalRecords = totalRecords,
            page = page,
            pageSize = pageSize,
            data = pagedList
        });
    }

        private object BuildFormDataForPartition(
        TaxDeclaration baseTd,
        AssessmentPartitionItem part,
        int partIndex,
        string trackingNo,
        long formWorkflowId)
    {
        var partTd = new TaxDeclaration
        {
            Id = baseTd.Id,
            TaxDeclarationNumber = !string.IsNullOrWhiteSpace(part.SystemGeneratedTDN) ? part.SystemGeneratedTDN : baseTd.TaxDeclarationNumber,
            PropertyIndexNumber = !string.IsNullOrWhiteSpace(part.PIN) ? part.PIN : baseTd.PropertyIndexNumber,
            OwnerName = !string.IsNullOrWhiteSpace(part.NewOwnerName) ? part.NewOwnerName : baseTd.OwnerName,
            OwnerAddress = !string.IsNullOrWhiteSpace(part.OwnerAddress) ? part.OwnerAddress : baseTd.OwnerAddress,
            AdministratorName = !string.IsNullOrWhiteSpace(part.AdministratorName) ? part.AdministratorName : baseTd.AdministratorName,
            AdministratorAddress = !string.IsNullOrWhiteSpace(part.AdministratorAddress) ? part.AdministratorAddress : baseTd.AdministratorAddress,
            AdministratorContactNo = !string.IsNullOrWhiteSpace(part.AdministratorContactNo) ? part.AdministratorContactNo : baseTd.AdministratorContactNo,
            StreetNo = !string.IsNullOrWhiteSpace(part.StreetLocation) ? part.StreetLocation : baseTd.StreetNo,
            DistrictOrBarangay = !string.IsNullOrWhiteSpace(part.DistrictOrBarangay) ? part.DistrictOrBarangay : baseTd.DistrictOrBarangay,
            Municipality = !string.IsNullOrWhiteSpace(part.CityMunicipality) ? part.CityMunicipality : baseTd.Municipality,
            LotNo = !string.IsNullOrWhiteSpace(part.LotNumber) ? part.LotNumber : baseTd.LotNo,
            SurveyNo = !string.IsNullOrWhiteSpace(part.SurveyNumber) ? part.SurveyNumber : baseTd.SurveyNo,
            CadastralSurveyNo = !string.IsNullOrWhiteSpace(part.CadastralSurveyNo) ? part.CadastralSurveyNo : baseTd.CadastralSurveyNo,
            SectionNo = !string.IsNullOrWhiteSpace(part.SectionNo) ? part.SectionNo : baseTd.SectionNo,
            ParcelNo = !string.IsNullOrWhiteSpace(part.ParcelNo) ? part.ParcelNo : baseTd.ParcelNo,
            TitleNo = !string.IsNullOrWhiteSpace(part.TitleNo) ? part.TitleNo : baseTd.TitleNo,
            ARPNo = !string.IsNullOrWhiteSpace(part.ARPNo) ? part.ARPNo : baseTd.ARPNo,
            AreaSqMeters = part.AreaSqMeters > 0 ? part.AreaSqMeters : (part.TotalBuildingAreaSqm > 0 ? part.TotalBuildingAreaSqm : baseTd.AreaSqMeters),
            BuildingUseType = !string.IsNullOrWhiteSpace(part.PropertyTypeAccordUse) ? part.PropertyTypeAccordUse : baseTd.BuildingUseType,
            MarketValue = part.MarketValue > 0 ? part.MarketValue : baseTd.MarketValue,
            AssessedValue = part.AssessedValue > 0 ? part.AssessedValue : baseTd.AssessedValue,
            FloorPlanSketchPath = part.BuildingFloorPlanSketchPath ?? baseTd.FloorPlanSketchPath,
            Memoranda = baseTd.Memoranda,
            TransactionCode = baseTd.TransactionCode,
            EffectivityYear = baseTd.EffectivityYear,
            EffectivityQuarter = baseTd.EffectivityQuarter,
            Taxability = baseTd.Taxability
        };

        var attachments = ParseAttachments(part.AttachmentsJson);
        string pKind = !string.IsNullOrWhiteSpace(part.Kind) ? part.Kind.Trim() : "Land";
        int kindId = ParsePropertyKindId(pKind, 1);
        string? sketch = part.BuildingFloorPlanSketchPath ?? baseTd.FloorPlanSketchPath;
        string pLetter = !string.IsNullOrWhiteSpace(part.PartitionIndex) ? part.PartitionIndex : ((char)('A' + partIndex)).ToString();

        if (kindId == 1) // Land
        {
            var l = partTd.EnsureLandDetails();
            l.BoundaryNorth = part.BoundaryNorth ?? baseTd.LandDetails?.BoundaryNorth;
            l.BoundarySouth = part.BoundarySouth ?? baseTd.LandDetails?.BoundarySouth;
            l.BoundaryEast = part.BoundaryEast ?? baseTd.LandDetails?.BoundaryEast;
            l.BoundaryWest = part.BoundaryWest ?? baseTd.LandDetails?.BoundaryWest;
            l.Nremarks = part.Nremarks ?? baseTd.LandDetails?.Nremarks;
            l.Sremarks = part.Sremarks ?? baseTd.LandDetails?.Sremarks;
            l.Eremarks = part.Eremarks ?? baseTd.LandDetails?.Eremarks;
            l.Wremarks = part.Wremarks ?? baseTd.LandDetails?.Wremarks;
            return BuildLandDTO(partTd, trackingNo, formWorkflowId, sketch, attachments, partIndex, pLetter);
        }
        else if (kindId == 3) // Machinery
        {
            var m = partTd.EnsureMachineryDetails();
            m.OriginalCost = part.AcquisitionCost > 0 ? part.AcquisitionCost : (baseTd.MachineryDetails?.OriginalCost ?? 0);
            m.LandOwnerName = part.LandOwnerName ?? baseTd.MachineryDetails?.LandOwnerName;
            m.LandPin = part.LandPin ?? baseTd.MachineryDetails?.LandPin;
            m.BuildingReferenceName = part.BuildingReferenceName ?? baseTd.MachineryDetails?.BuildingReferenceName;
            m.BuildingPin = part.BuildingPin ?? baseTd.MachineryDetails?.BuildingPin;
            return BuildMachineryDTO(partTd, trackingNo, formWorkflowId, sketch, attachments, partIndex, pLetter);
        }
        else // Building
        {
            var b = partTd.EnsureBuildingDetails();
            b.TotalFloorArea = part.TotalBuildingAreaSqm > 0 ? part.TotalBuildingAreaSqm : (part.AreaSqMeters > 0 ? part.AreaSqMeters : (baseTd.BuildingDetails?.TotalFloorArea ?? 0));
            b.BuildingUseType = !string.IsNullOrWhiteSpace(part.PropertyTypeAccordUse) ? part.PropertyTypeAccordUse : (baseTd.BuildingDetails?.BuildingUseType ?? "Residential Condominium");
            b.OtherBuildingUseType = part.OtherPropertyTypeAccordUse ?? baseTd.BuildingDetails?.OtherBuildingUseType;
            b.LandSurveyNo = !string.IsNullOrWhiteSpace(part.SurveyNumber) ? part.SurveyNumber : baseTd.BuildingDetails?.LandSurveyNo;
            b.LandLotNo = !string.IsNullOrWhiteSpace(part.LotNumber) ? part.LotNumber : baseTd.BuildingDetails?.LandLotNo;
            b.LandTitleNo = !string.IsNullOrWhiteSpace(part.TitleNo) ? part.TitleNo : baseTd.BuildingDetails?.LandTitleNo;
            b.LandAreaSqm = part.AreaSqMeters > 0 ? part.AreaSqMeters : (baseTd.BuildingDetails?.LandAreaSqm ?? 0);
            b.NoOfStoreys = part.NoOfStoreys > 0 ? part.NoOfStoreys : (baseTd.BuildingDetails?.NoOfStoreys ?? 1);
            b.BuildingPermitNo = part.BuildingPermitNo ?? baseTd.BuildingDetails?.BuildingPermitNo;
            b.BuildingAgeYears = part.BuildingAgeYears > 0 ? part.BuildingAgeYears : (baseTd.BuildingDetails?.BuildingAgeYears ?? 0);
            b.StructuralType = part.StructuralType ?? baseTd.BuildingDetails?.StructuralType;
            b.BuildingDepreciationPercent = part.BuildingDepreciationPercent > 0 ? part.BuildingDepreciationPercent : (baseTd.BuildingDetails?.BuildingDepreciationPercent ?? 0);
            b.BuildingDepreciationAccumulated = part.BuildingDepreciationAccumulated > 0 ? part.BuildingDepreciationAccumulated : (baseTd.BuildingDetails?.BuildingDepreciationAccumulated ?? 0);
            b.DepreciatedMarketValue = part.BuildingDepreciatedMarketValue > 0 ? part.BuildingDepreciatedMarketValue : (baseTd.BuildingDetails?.DepreciatedMarketValue ?? 0);
            return BuildBuildingDTO(partTd, trackingNo, formWorkflowId, sketch, attachments, partIndex, pLetter);
        }
    }

    // GET: /api/mobile/transaction/download-details
    [HttpGet("transaction/download-details")]
    public async Task<IActionResult> DownloadTransactionDetails([FromQuery] long? formWorkflowId, [FromQuery] string trackingNo = "", [FromQuery] int? partitionIndex = null)
    {
        string cleanTracking = (trackingNo ?? "").Trim();

        // Strictly resolve ONLY PropertyAssessmentRequest or RequestForInspection
        var allRpas = await _context.PropertyAssessmentRequests.AsNoTracking().ToListAsync();
        var matchedRpa = allRpas.FirstOrDefault(p =>
            (formWorkflowId.HasValue && formWorkflowId.Value > 0 && GuidToNumericId(p.Id) == formWorkflowId.Value) ||
            (!string.IsNullOrEmpty(cleanTracking) && p.TrackingNumber.Equals(cleanTracking, StringComparison.OrdinalIgnoreCase)));

        RequestForInspection? matchedRfi = null;
        if (matchedRpa == null)
        {
            var allRfis = await _context.RequestForInspections.AsNoTracking().ToListAsync();
            matchedRfi = allRfis.FirstOrDefault(r =>
                (formWorkflowId.HasValue && formWorkflowId.Value > 0 && GuidToNumericId(r.Id) == formWorkflowId.Value) ||
                (!string.IsNullOrEmpty(cleanTracking) && r.TrackingNumber.Equals(cleanTracking, StringComparison.OrdinalIgnoreCase)));
        }

        if (matchedRpa == null && matchedRfi == null)
        {
            return BadRequest(new
            {
                success = false,
                message = "Only Property Assessment (RPA) and Field Inspection (RFI) transactions can be synced in the mobile app."
            });
        }

        TaxDeclaration td;
        long resolvedWfId;
        string resolvedTrackingNo;
        string requestCategory;
        bool hasPartition = false;
        int partitionCount = 0;
        List<AssessmentPartitionItem> parts;

        if (matchedRpa != null)
        {
            parts = PartitionJsonHelper.ParseList(matchedRpa.GeneratedPartitionsJson);
            hasPartition = parts.Count > 0;
            partitionCount = parts.Count;
            resolvedWfId = GuidToNumericId(matchedRpa.Id);
            resolvedTrackingNo = matchedRpa.TrackingNumber;
            requestCategory = "Property Assessment";
            td = await BuildOrFindTaxDeclarationForRpaAsync(matchedRpa);
        }
        else
        {
            parts = PartitionJsonHelper.ParseList(matchedRfi!.GeneratedPartitionsJson);
            hasPartition = parts.Count > 0;
            partitionCount = parts.Count;
            resolvedWfId = GuidToNumericId(matchedRfi!.Id);
            resolvedTrackingNo = matchedRfi.TrackingNumber;
            requestCategory = "Field Inspection";
            td = await BuildOrFindTaxDeclarationForRfiAsync(matchedRfi);
        }

        long resolvedTdnId = GuidToNumericId(td.Id);

        // Build packages for ALL partitions
        var partitionDetailsList = new List<object>();
        for (int i = 0; i < parts.Count; i++)
        {
            var p = parts[i];
            var pForm = BuildFormDataForPartition(td, p, i, resolvedTrackingNo, resolvedWfId);
            var pAttachments = ParseAttachments(p.AttachmentsJson);
            int pKindId = ParsePropertyKindId(p.Kind, 1);
            string pFormType = pKindId switch { 1 => "LAND", 3 => "MACHINERY", _ => "BUILDING" };
            string? pSketch = p.BuildingFloorPlanSketchPath ?? td.FloorPlanSketchPath;

            partitionDetailsList.Add(new
            {
                index = i,
                partitionIndex = !string.IsNullOrWhiteSpace(p.PartitionIndex) ? p.PartitionIndex : ((char)('A' + i)).ToString(),
                kind = !string.IsNullOrWhiteSpace(p.Kind) ? p.Kind : "Land",
                transactionTypeId = pKindId,
                formType = pFormType,
                systemGeneratedTDN = p.SystemGeneratedTDN,
                arpNo = !string.IsNullOrWhiteSpace(p.ARPNo) ? p.ARPNo : (p.SystemGeneratedTDN ?? td.TaxDeclarationNumber),
                pin = p.PIN,
                propertyTypeAccordUse = p.PropertyTypeAccordUse,
                otherPropertyTypeAccordUse = p.OtherPropertyTypeAccordUse,
                surveyNo = p.SurveyNumber,
                lotNo = p.LotNumber,
                titleNo = p.TitleNo,
                sectionNo = p.SectionNo,
                ownerName = !string.IsNullOrWhiteSpace(p.NewOwnerName) ? p.NewOwnerName : td.OwnerName,
                areaSqMeters = p.AreaSqMeters > 0 ? p.AreaSqMeters : p.TotalBuildingAreaSqm,
                marketValue = p.MarketValue,
                assessedValue = p.AssessedValue,
                sketchUrl = pSketch,
                sketchFileName = !string.IsNullOrWhiteSpace(pSketch) ? Path.GetFileName(pSketch) : null,
                attachments = pAttachments,
                formData = pForm
            });
        }

        int targetIndex = 0;
        if (partitionIndex.HasValue && partitionIndex.Value >= 0 && partitionIndex.Value < parts.Count)
        {
            targetIndex = partitionIndex.Value;
        }

        dynamic? selectedPart = partitionDetailsList.Count > 0 ? partitionDetailsList[targetIndex] : null;

        string formType = td.Kind switch
        {
            PropertyKind.Land => "LAND",
            PropertyKind.Machinery => "MACHINERY",
            _ => "BUILDING"
        };
        int transactionTypeId = td.Kind switch
        {
            PropertyKind.Land => 1,
            PropertyKind.Machinery => 3,
            _ => 2
        };

        object fallbackFormData = formType switch
        {
            "LAND" => BuildLandDTO(td, resolvedTrackingNo, resolvedWfId, td.FloorPlanSketchPath, new(), 0, "A"),
            "MACHINERY" => BuildMachineryDTO(td, resolvedTrackingNo, resolvedWfId, td.FloorPlanSketchPath, new(), 0, "A"),
            _ => BuildBuildingDTO(td, resolvedTrackingNo, resolvedWfId, td.FloorPlanSketchPath, new(), 0, "A")
        };

        return Ok(new
        {
            success = true,
            hasPartition = hasPartition,
            partitionCount = partitionCount,
            hasMultiplePartitions = partitionCount > 1,
            selectedPartitionIndex = targetIndex,
            formWorkflowId = resolvedWfId,
            trackingNo = resolvedTrackingNo,
            requestType = requestCategory,
            transactionTypeId = selectedPart != null ? selectedPart.transactionTypeId : transactionTypeId,
            formType = selectedPart != null ? selectedPart.formType : formType,
            owner = selectedPart != null ? selectedPart.ownerName : td.OwnerName,
            arpNo = selectedPart != null ? (!string.IsNullOrWhiteSpace(selectedPart.arpNo) ? selectedPart.arpNo : (selectedPart.systemGeneratedTDN ?? td.TaxDeclarationNumber)) : (!string.IsNullOrWhiteSpace(td.ARPNo) ? td.ARPNo : td.TaxDeclarationNumber),
            tdnId = resolvedTdnId,
            sketchUrl = selectedPart != null ? selectedPart.sketchUrl : td.FloorPlanSketchPath,
            sketchFileName = selectedPart != null ? selectedPart.sketchFileName : (string.IsNullOrWhiteSpace(td.FloorPlanSketchPath) ? null : Path.GetFileName(td.FloorPlanSketchPath)),
            attachments = selectedPart != null ? selectedPart.attachments : new List<MobileAttachmentDTO>(),
            formData = selectedPart != null ? selectedPart.formData : fallbackFormData,
            partitions = partitionDetailsList,
            downloadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    // =========================================================================
    // 2. FETCH FORM DETAILS (BUILDING, LAND, MACHINERY)
    // =========================================================================

    // GET: /api/mobile/form/building
    [HttpGet("form/building")]
    public async Task<IActionResult> GetBuildingForm([FromQuery] long? tdnId, [FromQuery] long? castTdnBuildingId = null)
    {
        var td = await FindTaxDeclaration(tdnId, PropertyKind.Building);
        if (td == null)
        {
            return NotFound(new { success = false, message = "Building assessment record not found." });
        }

        var dto = BuildBuildingDTO(td);
        return Ok(new { success = true, data = dto });
    }

    // GET: /api/mobile/form/land
    [HttpGet("form/land")]
    public async Task<IActionResult> GetLandForm([FromQuery] long? tdnId, [FromQuery] long? tdnLandId = null)
    {
        var td = await FindTaxDeclaration(tdnId, PropertyKind.Land);
        if (td == null)
        {
            return NotFound(new { success = false, message = "Land appraisal record not found." });
        }

        var dto = BuildLandDTO(td);
        return Ok(new { success = true, data = dto });
    }

    // GET: /api/mobile/form/machinery
    [HttpGet("form/machinery")]
    public async Task<IActionResult> GetMachineryForm([FromQuery] long? tdnId, [FromQuery] long? tdnMachineryId = null)
    {
        var td = await FindTaxDeclaration(tdnId, PropertyKind.Machinery);
        if (td == null)
        {
            return NotFound(new { success = false, message = "Machinery assessment record not found." });
        }

        var dto = BuildMachineryDTO(td);
        return Ok(new { success = true, data = dto });
    }

    // =========================================================================
    // 3. UPDATE PROPERTY FORM DETAILS (STRICTLY PROPERTY ASSESSMENT & FIELD INSPECTION)
    // =========================================================================

    // POST: /api/mobile/form/update-building
    [HttpPost("form/update-building")]
    public async Task<IActionResult> UpdateBuildingForm([FromBody] TDNBuildingDTO dto)
    {
        if (dto == null || dto.TDN == null)
        {
            return BadRequest(new { success = false, message = "Invalid building payload." });
        }

        var (matchedRpa, matchedRfi, isForbiddenOtherType) = await FindParentMobileTransactionAsync(
            dto.TDN.FormWorkflowId, dto.TDN.TrackingNo, dto.TDN.ARPNo, dto.TDN.PIN);

        if (isForbiddenOtherType)
        {
            return BadRequest(new
            {
                success = false,
                message = "Only Property Assessment (RPA) and Field Inspection (RFI) transactions can be synced in the mobile app."
            });
        }

        bool hasPartition = false;
        if (matchedRpa != null)
        {
            hasPartition = PartitionJsonHelper.ParseList(matchedRpa.GeneratedPartitionsJson).Count > 0;
        }
        else if (matchedRfi != null)
        {
            hasPartition = PartitionJsonHelper.ParseList(matchedRfi.GeneratedPartitionsJson).Count > 0;
        }

        if (!hasPartition)
        {
            return BadRequest(new
            {
                success = false,
                message = "Cannot edit transaction: This transaction has no generated partitions. Partitions must be generated in the Office Web Portal first before field edits can be submitted."
            });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var td = await FindOrMatchTaxDeclaration(dto.TDN.TDNId, dto.TDN.ARPNo, dto.TDN.PIN, PropertyKind.Building);
            if (td == null)
            {
                td = new TaxDeclaration
                {
                    Kind = PropertyKind.Building,
                    TaxDeclarationNumber = dto.TDN.DeclarationNo ?? dto.TDN.ARPNo ?? matchedRpa?.PrevTDN ?? matchedRfi?.TaxDeclarationNumber ?? "TDN-BLDG-" + DateTime.Now.Ticks,
                    PropertyIndexNumber = dto.TDN.PIN ?? matchedRpa?.PrevPIN ?? matchedRfi?.PropertyIndexNumber ?? "PIN-" + DateTime.Now.Ticks
                };
                _context.TaxDeclarations.Add(td);
            }

            ApplyCoreTDNToEntity(dto.TDN, td);

            var bldg = td.EnsureBuildingDetails();
            if (dto.CAST_TDNBuildingDTO != null)
            {
                var b = dto.CAST_TDNBuildingDTO;
                bldg.StreetNo = b.STREET ?? bldg.StreetNo;
                bldg.TotalFloorArea = b.TotalbldgArea ?? bldg.TotalFloorArea;
                bldg.NoOfStoreys = b.StoreyNo ?? bldg.NoOfStoreys;
                bldg.BuildingPermitNo = b.PermitNo ?? bldg.BuildingPermitNo;
                bldg.BuildingAgeYears = b.Age ?? bldg.BuildingAgeYears;
                bldg.BuildingDepreciationPercent = b.DepreciationRate ?? bldg.BuildingDepreciationPercent;
                bldg.BuildingDepreciationAccumulated = b.DepreciationAccumulated ?? bldg.BuildingDepreciationAccumulated;
                bldg.DepreciatedMarketValue = b.DepreciationMarketval ?? bldg.DepreciatedMarketValue;
                bldg.TotalAdditionalItemsValue = b.TotalAddItem ?? bldg.TotalAdditionalItemsValue;
                bldg.MarketValue = b.TotalMarketVal ?? td.MarketValue;
                bldg.AssessedValue = b.TotalAssessedVal ?? td.AssessedValue;
                bldg.StructuralType = b.StucturalType ?? b.OtherStructure ?? bldg.StructuralType;
                bldg.LandLotNo = b.LotNo ?? bldg.LandLotNo;
                bldg.LandSurveyNo = b.SurveyNo ?? bldg.LandSurveyNo;
                bldg.LandTitleNo = b.TCTNo ?? bldg.LandTitleNo;
                bldg.LandOwnerTin = b.TIN ?? bldg.LandOwnerTin;
                bldg.LandOwnerPhone = b.MobileNo ?? bldg.LandOwnerPhone;
                bldg.LandOwnerEmail = b.EmailAddress ?? bldg.LandOwnerEmail;
                bldg.LandOwnerAddress = b.MailingAddress ?? bldg.LandOwnerAddress;
                if (!string.IsNullOrWhiteSpace(b.PropertyTypeAccordUse))
                {
                    bldg.BuildingUseType = b.PropertyTypeAccordUse;
                    td.BuildingUseType = b.PropertyTypeAccordUse;
                }
                if (!string.IsNullOrWhiteSpace(b.OtherPropertyTypeAccordUse)) bldg.OtherBuildingUseType = b.OtherPropertyTypeAccordUse;
                if (!string.IsNullOrWhiteSpace(b.LotNo)) td.LotNo = b.LotNo;
                if (!string.IsNullOrWhiteSpace(b.SurveyNo)) td.SurveyNo = b.SurveyNo;
                if (!string.IsNullOrWhiteSpace(b.TCTNo)) td.TitleNo = b.TCTNo;
                if (!string.IsNullOrWhiteSpace(b.ARPNo)) td.ARPNo = b.ARPNo;
            }

            // Sync updates directly into the parent PropertyAssessmentRequest or RequestForInspection
            SyncEntityChangesToParentTransaction(matchedRpa, matchedRfi, td, bldg, null, null, dto.TDN);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "BUILDING_UPDATED_VIA_MOBILE",
                arpNo = td.TaxDeclarationNumber,
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Building assessment synced to Property Assessment / Field Inspection transaction.",
                tdnId = GuidToNumericId(td.Id),
                castTdnBuildingId = GuidToNumericId(bldg.Id)
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = $"Failed to update Building: {ex.Message}" });
        }
    }

    // POST: /api/mobile/form/update-land
    [HttpPost("form/update-land")]
    public async Task<IActionResult> UpdateLandForm([FromBody] TDNLandDTO dto)
    {
        if (dto == null || dto.TDN == null)
        {
            return BadRequest(new { success = false, message = "Invalid land payload." });
        }

        var (matchedRpa, matchedRfi, isForbiddenOtherType) = await FindParentMobileTransactionAsync(
            dto.TDN.FormWorkflowId, dto.TDN.TrackingNo, dto.TDN.ARPNo, dto.TDN.PIN);

        if (isForbiddenOtherType)
        {
            return BadRequest(new
            {
                success = false,
                message = "Only Property Assessment (RPA) and Field Inspection (RFI) transactions can be synced in the mobile app."
            });
        }

        bool hasPartition = false;
        if (matchedRpa != null)
        {
            hasPartition = PartitionJsonHelper.ParseList(matchedRpa.GeneratedPartitionsJson).Count > 0;
        }
        else if (matchedRfi != null)
        {
            hasPartition = PartitionJsonHelper.ParseList(matchedRfi.GeneratedPartitionsJson).Count > 0;
        }

        if (!hasPartition)
        {
            return BadRequest(new
            {
                success = false,
                message = "Cannot edit transaction: This transaction has no generated partitions. Partitions must be generated in the Office Web Portal first before field edits can be submitted."
            });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var td = await FindOrMatchTaxDeclaration(dto.TDN.TDNId, dto.TDN.ARPNo, dto.TDN.PIN, PropertyKind.Land);
            if (td == null)
            {
                td = new TaxDeclaration
                {
                    Kind = PropertyKind.Land,
                    TaxDeclarationNumber = dto.TDN.DeclarationNo ?? dto.TDN.ARPNo ?? matchedRpa?.PrevTDN ?? matchedRfi?.TaxDeclarationNumber ?? "TDN-LAND-" + DateTime.Now.Ticks,
                    PropertyIndexNumber = dto.TDN.PIN ?? matchedRpa?.PrevPIN ?? matchedRfi?.PropertyIndexNumber ?? "PIN-" + DateTime.Now.Ticks
                };
                _context.TaxDeclarations.Add(td);
            }

            ApplyCoreTDNToEntity(dto.TDN, td);

            var land = td.EnsureLandDetails();
            if (dto.TDNLand != null)
            {
                var l = dto.TDNLand;
                land.BoundaryNorth = l.NorthBoundary ?? land.BoundaryNorth;
                land.BoundarySouth = l.SouthBoundary ?? land.BoundarySouth;
                land.BoundaryEast = l.EastBoundary ?? land.BoundaryEast;
                land.BoundaryWest = l.WestBoundary ?? land.BoundaryWest;
                land.Nremarks = l.Nremarks ?? land.Nremarks;
                land.Sremarks = l.Sremarks ?? land.Sremarks;
                land.Eremarks = l.Eremarks ?? land.Eremarks;
                land.Wremarks = l.Wremarks ?? land.Wremarks;
            }

            // Sync updates directly into the parent PropertyAssessmentRequest or RequestForInspection
            SyncEntityChangesToParentTransaction(matchedRpa, matchedRfi, td, null, land, null, dto.TDN);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "LAND_UPDATED_VIA_MOBILE",
                arpNo = td.TaxDeclarationNumber,
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Land appraisal synced to Property Assessment / Field Inspection transaction.",
                tdnId = GuidToNumericId(td.Id),
                tdnLandId = GuidToNumericId(land.Id)
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = $"Failed to update Land: {ex.Message}" });
        }
    }

    // POST: /api/mobile/form/update-machinery
    [HttpPost("form/update-machinery")]
    public async Task<IActionResult> UpdateMachineryForm([FromBody] TDNMachineDTO dto)
    {
        if (dto == null || dto.TDN == null)
        {
            return BadRequest(new { success = false, message = "Invalid machinery payload." });
        }

        var (matchedRpa, matchedRfi, isForbiddenOtherType) = await FindParentMobileTransactionAsync(
            dto.TDN.FormWorkflowId, dto.TDN.TrackingNo, dto.TDN.ARPNo, dto.TDN.PIN);

        if (isForbiddenOtherType)
        {
            return BadRequest(new
            {
                success = false,
                message = "Only Property Assessment (RPA) and Field Inspection (RFI) transactions can be synced in the mobile app."
            });
        }

        bool hasPartition = false;
        if (matchedRpa != null)
        {
            hasPartition = PartitionJsonHelper.ParseList(matchedRpa.GeneratedPartitionsJson).Count > 0;
        }
        else if (matchedRfi != null)
        {
            hasPartition = PartitionJsonHelper.ParseList(matchedRfi.GeneratedPartitionsJson).Count > 0;
        }

        if (!hasPartition)
        {
            return BadRequest(new
            {
                success = false,
                message = "Cannot edit transaction: This transaction has no generated partitions. Partitions must be generated in the Office Web Portal first before field edits can be submitted."
            });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var td = await FindOrMatchTaxDeclaration(dto.TDN.TDNId, dto.TDN.ARPNo, dto.TDN.PIN, PropertyKind.Machinery);
            if (td == null)
            {
                td = new TaxDeclaration
                {
                    Kind = PropertyKind.Machinery,
                    TaxDeclarationNumber = dto.TDN.DeclarationNo ?? dto.TDN.ARPNo ?? matchedRpa?.PrevTDN ?? matchedRfi?.TaxDeclarationNumber ?? "TDN-MACH-" + DateTime.Now.Ticks,
                    PropertyIndexNumber = dto.TDN.PIN ?? matchedRpa?.PrevPIN ?? matchedRfi?.PropertyIndexNumber ?? "PIN-" + DateTime.Now.Ticks
                };
                _context.TaxDeclarations.Add(td);
            }

            ApplyCoreTDNToEntity(dto.TDN, td);

            var machine = td.EnsureMachineryDetails();
            if (dto.TDNMachine != null)
            {
                machine.LandPin = dto.TDNMachine.LandOwnerPIN ?? machine.LandPin;
                machine.BuildingPin = dto.TDNMachine.BuildingOwnerPIN ?? machine.BuildingPin;
                machine.LandOwnerName = dto.TDNMachine.LandOwner ?? machine.LandOwnerName;
                machine.BuildingReferenceName = dto.TDNMachine.BuildingOwner ?? machine.BuildingReferenceName;
            }

            if (dto.TDNMachineDetail != null && dto.TDNMachineDetail.Count > 0)
            {
                var first = dto.TDNMachineDetail[0];
                machine.DescriptionBrand = first.BrandModel ?? machine.DescriptionBrand;
                machine.RatedCapacity = first.Capacity ?? machine.RatedCapacity;
                if (first.OriginalCost.HasValue && first.OriginalCost.Value > 0) machine.OriginalCost = first.OriginalCost.Value;
                if (first.EconomicLifeEstimated.HasValue) machine.EconomicLifeYears = first.EconomicLifeEstimated.Value;
                if (!string.IsNullOrWhiteSpace(first.ConditionWhenAcquired)) machine.ConditionWhenAcquired = first.ConditionWhenAcquired;
                if (first.AcquiredDate.HasValue) machine.DateAcquired = first.AcquiredDate.Value;
                if (first.DateOfInstallation.HasValue) machine.YearInstalled = first.DateOfInstallation.Value.Year;
                if (first.DateOfOperation.HasValue) machine.YearOfOperation = first.DateOfOperation.Value.Year;
            }

            // Sync updates directly into the parent PropertyAssessmentRequest or RequestForInspection
            SyncEntityChangesToParentTransaction(matchedRpa, matchedRfi, td, null, null, machine, dto.TDN);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "MACHINERY_UPDATED_VIA_MOBILE",
                arpNo = td.TaxDeclarationNumber,
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Machinery assessment synced to Property Assessment / Field Inspection transaction.",
                tdnId = GuidToNumericId(td.Id),
                tdnMachineId = GuidToNumericId(machine.Id)
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = $"Failed to update Machinery: {ex.Message}" });
        }
    }

    // =========================================================================
    // 3.1 ATTACHMENT UPLOAD (MULTIPLE FILES FOR BUILDING, LAND, MACHINERY)
    // =========================================================================

    // POST: /api/mobile/upload-attachment
    [HttpPost("upload-attachment")]
    public async Task<IActionResult> UploadAttachment([FromForm] Microsoft.AspNetCore.Http.IFormFile? file, [FromForm] string? trackingNo, [FromForm] string? caption, [FromForm] string? fileType)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No file received." });
        }

        string cleanTracking = string.IsNullOrWhiteSpace(trackingNo) ? "general" : trackingNo.Trim();
        string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "InspectionAttachments", cleanTracking);
        Directory.CreateDirectory(uploadFolder);

        string ext = Path.GetExtension(file.FileName);
        string uniqueName = $"{Guid.NewGuid():N}_{Regex.Replace(Path.GetFileNameWithoutExtension(file.FileName), @"[^a-zA-Z0-9_\-]", "_")}{ext}";
        string fullPath = Path.Combine(uploadFolder, uniqueName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        string fileUrl = $"/uploads/InspectionAttachments/{cleanTracking}/{uniqueName}";
        bool isImage = ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                       ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                       ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                       ext.Equals(".webp", StringComparison.OrdinalIgnoreCase);

        var attachment = new MobileAttachmentDTO
        {
            Id = Guid.NewGuid().ToString("N"),
            FileName = file.FileName,
            FileUrl = fileUrl,
            FileType = !string.IsNullOrWhiteSpace(fileType) ? fileType : (isImage ? "image" : "document"),
            FileSize = $"{Math.Round(file.Length / 1024.0, 1)} KB",
            Caption = caption ?? string.Empty,
            UploadedAt = DateTime.Now
        };

        return Ok(new
        {
            success = true,
            message = "File uploaded successfully.",
            data = attachment
        });
    }

    // =========================================================================
    // 4. WORKFLOW FORWARDING (STRICTLY PROPERTY ASSESSMENT & FIELD INSPECTION)
    // =========================================================================

    // POST: /api/mobile/workflow/forward
    [HttpPost("workflow/forward")]
    public async Task<IActionResult> ForwardWorkflow([FromBody] MobileForwardWorkflowRequest req)
    {
        if (req == null || req.FormWorkflowId <= 0 || req.NextStatusId <= 0)
        {
            return BadRequest(new { success = false, message = "FormWorkflowId and NextStatusId are required." });
        }

        var jwtUser = HttpContext.Items["JwtUser"] as JwtPayload;
        string actorName = jwtUser?.Name ?? "Mobile Field Appraiser";
        string actorRole = "Property Appraiser";

        // 1. Check PropertyAssessmentRequests (evaluated in memory because GuidToNumericId is a C# method)
        var allRpas = await _context.PropertyAssessmentRequests.ToListAsync();
        var par = allRpas.FirstOrDefault(p => GuidToNumericId(p.Id) == req.FormWorkflowId);
        if (par != null)
        {
            par.AssignedAppraiser = actorName;
            par.Remarks = req.Remarks;
            par.UpdatedDate = DateTime.Now;

            _workflowService.ForwardDocument(
                par.Id,
                "PropertyAssessmentRequest",
                DivisionType.Division1_TaxAssessmentAndLandHolding,
                (WorkflowStage)req.NextStatusId,
                actorName,
                actorRole,
                req.Remarks ?? "Completed field assessment and synced via FairTrack Mobile.",
                "Forwarded via Mobile App Assessment",
                par.ORNumber,
                par.ORDateIssued);

            await _workflowNotifier.NotifyTransactionForwardedAsync(new
            {
                requestId = par.Id,
                trackingNumber = par.TrackingNumber,
                remarks = req.Remarks
            });

            return Ok(new
            {
                success = true,
                message = "Property Assessment workflow forwarded successfully from mobile.",
                trackingNo = par.TrackingNumber,
                newStatusId = req.NextStatusId,
                newFormWorkflowId = req.FormWorkflowId
            });
        }

        // 2. Check RequestForInspections (evaluated in memory)
        var allRfis = await _context.RequestForInspections.ToListAsync();
        var rfi = allRfis.FirstOrDefault(r => GuidToNumericId(r.Id) == req.FormWorkflowId);
        if (rfi != null)
        {
            rfi.AssignedStaff = actorName;
            rfi.Remarks = req.Remarks;
            rfi.UpdatedDate = DateTime.Now;

            _workflowService.ForwardDocument(
                rfi.Id,
                "RequestForInspection",
                DivisionType.Division1_TaxAssessmentAndLandHolding,
                (WorkflowStage)req.NextStatusId,
                actorName,
                actorRole,
                req.Remarks ?? "Completed field inspection and synced via FairTrack Mobile.",
                "Forwarded via Mobile App Inspection",
                rfi.ORNumber,
                rfi.ORDateIssued);

            await _workflowNotifier.NotifyTransactionForwardedAsync(new
            {
                requestId = rfi.Id,
                trackingNumber = rfi.TrackingNumber,
                remarks = req.Remarks
            });

            return Ok(new
            {
                success = true,
                message = "Field Inspection workflow forwarded successfully from mobile.",
                trackingNo = rfi.TrackingNumber,
                newStatusId = req.NextStatusId,
                newFormWorkflowId = req.FormWorkflowId
            });
        }

        // Reject any other transaction type
        return BadRequest(new
        {
            success = false,
            message = "Only Property Assessment (RPA) and Field Inspection (RFI) transactions can be synced and forwarded from the mobile app."
        });
    }

    // =========================================================================
    // PRIVATE HELPERS FOR PROPERTY ASSESSMENT & FIELD INSPECTION SYNC
    // =========================================================================

    private async Task<(PropertyAssessmentRequest? rpa, RequestForInspection? rfi, bool isForbiddenOtherType)> FindParentMobileTransactionAsync(
        long? formWorkflowId,
        string? trackingNo,
        string? arpNo,
        string? pin)
    {
        string cleanTracking = (trackingNo ?? "").Trim();
        string cleanArp = (arpNo ?? "").Trim();
        string cleanPin = (pin ?? "").Trim();

        var allRpas = await _context.PropertyAssessmentRequests.ToListAsync();
        var matchedRpa = allRpas.FirstOrDefault(p =>
            (formWorkflowId.HasValue && formWorkflowId.Value > 0 && GuidToNumericId(p.Id) == formWorkflowId.Value) ||
            (!string.IsNullOrEmpty(cleanTracking) && p.TrackingNumber.Equals(cleanTracking, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && p.TrackingNumber.Equals(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && !string.IsNullOrEmpty(p.PrevTDN) && p.PrevTDN.Equals(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanPin) && !string.IsNullOrEmpty(p.PrevPIN) && p.PrevPIN.Equals(cleanPin, StringComparison.OrdinalIgnoreCase)));

        if (matchedRpa != null)
        {
            return (matchedRpa, null, false);
        }

        var allRfis = await _context.RequestForInspections.ToListAsync();
        var matchedRfi = allRfis.FirstOrDefault(r =>
            (formWorkflowId.HasValue && formWorkflowId.Value > 0 && GuidToNumericId(r.Id) == formWorkflowId.Value) ||
            (!string.IsNullOrEmpty(cleanTracking) && r.TrackingNumber.Equals(cleanTracking, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && r.TrackingNumber.Equals(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && !string.IsNullOrEmpty(r.TaxDeclarationNumber) && r.TaxDeclarationNumber.Equals(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanPin) && !string.IsNullOrEmpty(r.PropertyIndexNumber) && r.PropertyIndexNumber.Equals(cleanPin, StringComparison.OrdinalIgnoreCase)));

        if (matchedRfi != null)
        {
            return (null, matchedRfi, false);
        }

        // Check if the trackingNo or formWorkflowId belongs to a non-mobile request type (TaxMapping, TaxDeclarationRequest, LandHoldingRequest)
        if (!string.IsNullOrEmpty(cleanTracking))
        {
            bool isOtherModule =
                await _context.TaxMappingRequests.AnyAsync(t => t.RequestNumber == cleanTracking) ||
                await _context.TaxDeclarationRequests.AnyAsync(t => t.TrackingNumber == cleanTracking) ||
                await _context.LandHoldingRequests.AnyAsync(t => t.TrackingNumber == cleanTracking);

            if (isOtherModule)
            {
                return (null, null, true);
            }
        }

        return (null, null, false);
    }

    private async Task<TaxDeclaration> BuildOrFindTaxDeclarationForRpaAsync(PropertyAssessmentRequest rpa)
    {
        var partitions = PartitionJsonHelper.ParseList(rpa.GeneratedPartitionsJson);
        var firstPart = partitions.FirstOrDefault();

        TaxDeclaration? refTd = null;
        if (rpa.RefTaxDeclarationId.HasValue)
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.Id == rpa.RefTaxDeclarationId.Value);
        }

        if (refTd == null && firstPart != null && !string.IsNullOrWhiteSpace(firstPart.SystemGeneratedTDN))
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == firstPart.SystemGeneratedTDN);
        }

        if (refTd == null && !string.IsNullOrWhiteSpace(rpa.PrevTDN))
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == rpa.PrevTDN);
        }

        var kindStr = firstPart?.Kind ?? rpa.Kind ?? refTd?.Kind.ToString() ?? "Building";
        var parsedKind = Enum.TryParse<PropertyKind>(kindStr, true, out var pk) ? pk : (refTd?.Kind ?? PropertyKind.Building);

        var td = refTd ?? new TaxDeclaration
        {
            Id = rpa.Id,
            Kind = parsedKind,
            TaxDeclarationNumber = !string.IsNullOrWhiteSpace(firstPart?.SystemGeneratedTDN)
                ? firstPart.SystemGeneratedTDN
                : (!string.IsNullOrWhiteSpace(rpa.PrevTDN) ? rpa.PrevTDN : rpa.TrackingNumber),
            PropertyIndexNumber = !string.IsNullOrWhiteSpace(firstPart?.PIN)
                ? firstPart.PIN
                : (!string.IsNullOrWhiteSpace(rpa.PrevPIN) ? rpa.PrevPIN : "N/A"),
            TransactionCode = rpa.TransactionTypeCode,
            OwnerName = !string.IsNullOrWhiteSpace(firstPart?.NewOwnerName) ? firstPart.NewOwnerName : rpa.NewOwnerName,
            OwnerAddress = !string.IsNullOrWhiteSpace(firstPart?.OwnerAddress) ? firstPart.OwnerAddress : (rpa.Address ?? string.Empty),
            OwnerTin = !string.IsNullOrWhiteSpace(firstPart?.OwnerTIN) ? firstPart.OwnerTIN : (rpa.OwnerTIN ?? string.Empty),
            OwnerPhone = !string.IsNullOrWhiteSpace(firstPart?.OwnerContactNo) ? firstPart.OwnerContactNo : (rpa.ContactNo ?? string.Empty),
            StreetNo = !string.IsNullOrWhiteSpace(firstPart?.StreetLocation) ? firstPart.StreetLocation : (rpa.PropertyLocation ?? string.Empty),
            LotNo = !string.IsNullOrWhiteSpace(firstPart?.LotNumber) ? firstPart.LotNumber : rpa.LotNo,
            SurveyNo = firstPart?.SurveyNumber,
            TitleNo = firstPart?.TitleNo,
            AreaSqMeters = firstPart?.AreaSqMeters ?? 0m,
            MarketValue = firstPart?.MarketValue ?? 0m,
            AssessedValue = firstPart?.AssessedValue ?? 0m,
            Memoranda = (!string.IsNullOrWhiteSpace(firstPart?.Memoranda) ? firstPart.Memoranda : rpa.Remarks) ?? string.Empty,
            EffectivityYear = firstPart?.EffectivityYear ?? DateTime.Now.Year
        };

        td.Kind = parsedKind;
        if (firstPart != null)
        {
            OverlayPartitionOntoTaxDeclaration(firstPart, td);
        }

        return td;
    }

    private async Task<TaxDeclaration> BuildOrFindTaxDeclarationForRfiAsync(RequestForInspection rfi)
    {
        var partitions = PartitionJsonHelper.ParseList(rfi.GeneratedPartitionsJson);
        var firstPart = partitions.FirstOrDefault();

        TaxDeclaration? refTd = null;
        if (rfi.TaxDeclarationId.HasValue)
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.Id == rfi.TaxDeclarationId.Value);
        }

        if (refTd == null && firstPart != null && !string.IsNullOrWhiteSpace(firstPart.SystemGeneratedTDN))
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == firstPart.SystemGeneratedTDN);
        }

        if (refTd == null && !string.IsNullOrWhiteSpace(rfi.TaxDeclarationNumber))
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == rfi.TaxDeclarationNumber);
        }

        var kindStr = firstPart?.Kind ?? rfi.Kind ?? refTd?.Kind.ToString() ?? "Building";
        var parsedKind = Enum.TryParse<PropertyKind>(kindStr, true, out var pk) ? pk : (refTd?.Kind ?? PropertyKind.Building);

        var td = refTd ?? new TaxDeclaration
        {
            Id = rfi.Id,
            Kind = parsedKind,
            TaxDeclarationNumber = !string.IsNullOrWhiteSpace(firstPart?.SystemGeneratedTDN)
                ? firstPart.SystemGeneratedTDN
                : (!string.IsNullOrWhiteSpace(rfi.TaxDeclarationNumber) ? rfi.TaxDeclarationNumber : rfi.TrackingNumber),
            PropertyIndexNumber = !string.IsNullOrWhiteSpace(firstPart?.PIN)
                ? firstPart.PIN
                : (!string.IsNullOrWhiteSpace(rfi.PropertyIndexNumber) ? rfi.PropertyIndexNumber : "N/A"),
            TransactionCode = rfi.TransactionCode ?? "INSP",
            OwnerName = !string.IsNullOrWhiteSpace(firstPart?.NewOwnerName) ? firstPart.NewOwnerName : rfi.OwnerName,
            OwnerAddress = !string.IsNullOrWhiteSpace(firstPart?.OwnerAddress) ? firstPart.OwnerAddress : (rfi.PropertyLocation ?? string.Empty),
            OwnerPhone = !string.IsNullOrWhiteSpace(firstPart?.OwnerContactNo) ? firstPart.OwnerContactNo : (rfi.ContactNumber ?? string.Empty),
            StreetNo = !string.IsNullOrWhiteSpace(firstPart?.StreetLocation) ? firstPart.StreetLocation : (rfi.PropertyLocation ?? string.Empty),
            LotNo = !string.IsNullOrWhiteSpace(firstPart?.LotNumber) ? firstPart.LotNumber : rfi.LotNo,
            SurveyNo = firstPart?.SurveyNumber,
            TitleNo = firstPart?.TitleNo,
            AreaSqMeters = firstPart?.AreaSqMeters ?? 0m,
            MarketValue = firstPart?.MarketValue ?? 0m,
            AssessedValue = firstPart?.AssessedValue ?? 0m,
            Memoranda = (!string.IsNullOrWhiteSpace(firstPart?.Memoranda) ? firstPart.Memoranda : rfi.Remarks) ?? string.Empty,
            EffectivityYear = firstPart?.EffectivityYear ?? DateTime.Now.Year
        };

        td.Kind = parsedKind;
        if (firstPart != null)
        {
            OverlayPartitionOntoTaxDeclaration(firstPart, td);
        }

        return td;
    }

    private static void OverlayPartitionOntoTaxDeclaration(AssessmentPartitionItem part, TaxDeclaration td)
    {
        if (!string.IsNullOrWhiteSpace(part.SystemGeneratedTDN)) td.TaxDeclarationNumber = part.SystemGeneratedTDN;
        if (!string.IsNullOrWhiteSpace(part.NewOwnerName)) td.OwnerName = part.NewOwnerName;
        if (!string.IsNullOrWhiteSpace(part.OwnerAddress)) td.OwnerAddress = part.OwnerAddress;
        if (!string.IsNullOrWhiteSpace(part.StreetLocation)) td.StreetNo = part.StreetLocation;
        if (!string.IsNullOrWhiteSpace(part.LotNumber)) td.LotNo = part.LotNumber;
        if (!string.IsNullOrWhiteSpace(part.SurveyNumber)) td.SurveyNo = part.SurveyNumber;
        if (!string.IsNullOrWhiteSpace(part.TitleNo)) td.TitleNo = part.TitleNo;
        if (part.AreaSqMeters > 0) td.AreaSqMeters = part.AreaSqMeters;
        if (part.MarketValue > 0) td.MarketValue = part.MarketValue;
        if (part.AssessedValue > 0) td.AssessedValue = part.AssessedValue;
        if (!string.IsNullOrWhiteSpace(part.Memoranda)) td.Memoranda = part.Memoranda;

        if (td.Kind == PropertyKind.Building)
        {
            var b = td.EnsureBuildingDetails();
            if (part.NoOfStoreys > 0) b.NoOfStoreys = part.NoOfStoreys;
            if (part.TotalBuildingAreaSqm > 0) b.TotalFloorArea = part.TotalBuildingAreaSqm;
            if (!string.IsNullOrWhiteSpace(part.BuildingPermitNo)) b.BuildingPermitNo = part.BuildingPermitNo;
            if (part.BuildingAgeYears > 0) b.BuildingAgeYears = part.BuildingAgeYears;
            if (!string.IsNullOrWhiteSpace(part.StructuralType)) b.StructuralType = part.StructuralType;
            if (part.MarketValue > 0) b.MarketValue = part.MarketValue;
            if (part.AssessedValue > 0) b.AssessedValue = part.AssessedValue;
        }
        else if (td.Kind == PropertyKind.Land)
        {
            var l = td.EnsureLandDetails();
            if (!string.IsNullOrWhiteSpace(part.BoundaryNorth)) l.BoundaryNorth = part.BoundaryNorth;
            if (!string.IsNullOrWhiteSpace(part.BoundarySouth)) l.BoundarySouth = part.BoundarySouth;
            if (!string.IsNullOrWhiteSpace(part.BoundaryEast)) l.BoundaryEast = part.BoundaryEast;
            if (!string.IsNullOrWhiteSpace(part.BoundaryWest)) l.BoundaryWest = part.BoundaryWest;
            if (!string.IsNullOrWhiteSpace(part.Nremarks)) l.Nremarks = part.Nremarks;
            if (!string.IsNullOrWhiteSpace(part.Sremarks)) l.Sremarks = part.Sremarks;
            if (!string.IsNullOrWhiteSpace(part.Eremarks)) l.Eremarks = part.Eremarks;
            if (!string.IsNullOrWhiteSpace(part.Wremarks)) l.Wremarks = part.Wremarks;
        }
    }

    private void SyncEntityChangesToParentTransaction(
        PropertyAssessmentRequest? rpa,
        RequestForInspection? rfi,
        TaxDeclaration td,
        TDNBuilding? bldg,
        TDNLand? land,
        TDNMachinery? machine,
        TDNDTO? dtoTdn = null)
    {
        string trackingNo = rpa?.TrackingNumber ?? rfi?.TrackingNumber ?? dtoTdn?.TrackingNo ?? "general";
        var processedAttachments = ProcessAndSaveAttachments(dtoTdn?.Attachments, trackingNo);

        if (rpa != null)
        {
            if (!string.IsNullOrWhiteSpace(td.OwnerName)) rpa.NewOwnerName = td.OwnerName;
            if (!string.IsNullOrWhiteSpace(td.OwnerAddress)) rpa.Address = td.OwnerAddress;
            if (!string.IsNullOrWhiteSpace(td.LotNo)) rpa.LotNo = td.LotNo;
            if (!string.IsNullOrWhiteSpace(td.StreetNo)) rpa.PropertyLocation = td.StreetNo;
            if (!string.IsNullOrWhiteSpace(td.Memoranda)) rpa.Remarks = td.Memoranda;
            rpa.UpdatedDate = DateTime.Now;

            var partitions = PartitionJsonHelper.ParseList(rpa.GeneratedPartitionsJson);
            if (partitions.Count > 0)
            {
                int targetIdx = ResolvePartitionTargetIndex(partitions, dtoTdn);
                UpdatePartitionFromEntities(partitions[targetIdx], td, bldg, land, machine, dtoTdn, processedAttachments);
                rpa.GeneratedPartitionsJson = JsonSerializer.Serialize(partitions, _jsonOptions);
            }
        }

        if (rfi != null)
        {
            if (!string.IsNullOrWhiteSpace(td.OwnerName)) rfi.OwnerName = td.OwnerName;
            if (!string.IsNullOrWhiteSpace(td.LotNo)) rfi.LotNo = td.LotNo;
            if (!string.IsNullOrWhiteSpace(td.StreetNo)) rfi.PropertyLocation = td.StreetNo;
            if (!string.IsNullOrWhiteSpace(td.Memoranda)) rfi.Remarks = td.Memoranda;
            rfi.UpdatedDate = DateTime.Now;

            var partitions = PartitionJsonHelper.ParseList(rfi.GeneratedPartitionsJson);
            if (partitions.Count > 0)
            {
                int targetIdx = ResolvePartitionTargetIndex(partitions, dtoTdn);
                UpdatePartitionFromEntities(partitions[targetIdx], td, bldg, land, machine, dtoTdn, processedAttachments);
                rfi.GeneratedPartitionsJson = JsonSerializer.Serialize(partitions, _jsonOptions);
            }
        }
    }

    private static void UpdatePartitionFromEntities(
        AssessmentPartitionItem part,
        TaxDeclaration td,
        TDNBuilding? bldg,
        TDNLand? land,
        TDNMachinery? machine,
        TDNDTO? dtoTdn = null,
        List<MobileAttachmentDTO>? attachments = null)
    {
        if (!string.IsNullOrWhiteSpace(td.OwnerName)) part.NewOwnerName = td.OwnerName;
        if (!string.IsNullOrWhiteSpace(td.OwnerAddress)) part.OwnerAddress = td.OwnerAddress;
        if (!string.IsNullOrWhiteSpace(td.StreetNo)) part.StreetLocation = td.StreetNo;
        if (!string.IsNullOrWhiteSpace(td.LotNo)) part.LotNumber = td.LotNo;
        if (!string.IsNullOrWhiteSpace(td.SurveyNo)) part.SurveyNumber = td.SurveyNo;
        if (!string.IsNullOrWhiteSpace(td.TitleNo)) part.TitleNo = td.TitleNo;
        if (!string.IsNullOrWhiteSpace(td.PropertyIndexNumber)) part.PIN = td.PropertyIndexNumber;
        if (!string.IsNullOrWhiteSpace(td.ARPNo)) part.ARPNo = td.ARPNo;
        if (!string.IsNullOrWhiteSpace(td.SectionNo)) part.SectionNo = td.SectionNo;
        if (!string.IsNullOrWhiteSpace(td.BuildingUseType)) part.PropertyTypeAccordUse = td.BuildingUseType;
        if (td.AreaSqMeters > 0) part.AreaSqMeters = td.AreaSqMeters;
        if (td.MarketValue > 0) part.MarketValue = td.MarketValue;
        if (td.AssessedValue > 0) part.AssessedValue = td.AssessedValue;
        if (!string.IsNullOrWhiteSpace(td.Memoranda)) part.Memoranda = td.Memoranda;
        if (!string.IsNullOrWhiteSpace(td.StructuralFoundation)) part.StructuralFoundation = td.StructuralFoundation;
        if (!string.IsNullOrWhiteSpace(td.StructuralColumns)) part.StructuralColumns = td.StructuralColumns;
        if (!string.IsNullOrWhiteSpace(td.StructuralBeams)) part.StructuralBeams = td.StructuralBeams;
        if (!string.IsNullOrWhiteSpace(td.StructuralTrussRoof)) part.StructuralTrussRoof = td.StructuralTrussRoof;
        if (!string.IsNullOrWhiteSpace(td.StructuralRoof)) part.StructuralRoof = td.StructuralRoof;
        if (!string.IsNullOrWhiteSpace(td.StructuralExteriorWalls)) part.StructuralExteriorWalls = td.StructuralExteriorWalls;
        if (!string.IsNullOrWhiteSpace(td.StructuralFlooring)) part.StructuralFlooring = td.StructuralFlooring;
        if (!string.IsNullOrWhiteSpace(td.DoorMain)) part.DoorMain = td.DoorMain;
        if (!string.IsNullOrWhiteSpace(td.Ceiling)) part.Ceiling = td.Ceiling;
        if (!string.IsNullOrWhiteSpace(td.StructuralWindows)) part.StructuralWindows = td.StructuralWindows;
        if (!string.IsNullOrWhiteSpace(td.StructuralStairs)) part.StructuralStairs = td.StructuralStairs;
        if (!string.IsNullOrWhiteSpace(td.StructuralPartitionWalls)) part.StructuralPartitionWalls = td.StructuralPartitionWalls;
        if (!string.IsNullOrWhiteSpace(td.StructuralFloorFinish)) part.StructuralFloorFinish = td.StructuralFloorFinish;
        if (!string.IsNullOrWhiteSpace(td.StructuralFinishFront)) part.StructuralFinishFront = td.StructuralFinishFront;
        if (!string.IsNullOrWhiteSpace(td.ElectricalSystem)) part.ElectricalSystem = td.ElectricalSystem;
        if (!string.IsNullOrWhiteSpace(td.ToiletAndBathroom)) part.ToiletAndBathroom = td.ToiletAndBathroom;
        if (!string.IsNullOrWhiteSpace(td.PlumbingSewer)) part.PlumbingSewer = td.PlumbingSewer;

        if (bldg != null)
        {
            if (bldg.NoOfStoreys > 0) part.NoOfStoreys = bldg.NoOfStoreys;
            if (bldg.TotalFloorArea > 0)
            {
                part.TotalBuildingAreaSqm = bldg.TotalFloorArea;
                part.AreaSqMeters = bldg.TotalFloorArea;
            }
            if (!string.IsNullOrWhiteSpace(bldg.BuildingPermitNo)) part.BuildingPermitNo = bldg.BuildingPermitNo;
            if (bldg.BuildingAgeYears > 0) part.BuildingAgeYears = bldg.BuildingAgeYears;
            if (!string.IsNullOrWhiteSpace(bldg.StructuralType)) part.StructuralType = bldg.StructuralType;
            if (!string.IsNullOrWhiteSpace(bldg.BuildingUseType)) part.PropertyTypeAccordUse = bldg.BuildingUseType;
            if (!string.IsNullOrWhiteSpace(bldg.OtherBuildingUseType)) part.OtherPropertyTypeAccordUse = bldg.OtherBuildingUseType;
            if (!string.IsNullOrWhiteSpace(bldg.LandLotNo)) part.LotNumber = bldg.LandLotNo;
            if (!string.IsNullOrWhiteSpace(bldg.LandSurveyNo)) part.SurveyNumber = bldg.LandSurveyNo;
            if (!string.IsNullOrWhiteSpace(bldg.LandTitleNo)) part.TitleNo = bldg.LandTitleNo;
            if (bldg.BuildingDepreciationPercent > 0) part.BuildingDepreciationPercent = bldg.BuildingDepreciationPercent;
            if (bldg.BuildingDepreciationAccumulated > 0) part.BuildingDepreciationAccumulated = bldg.BuildingDepreciationAccumulated;
            if (bldg.DepreciatedMarketValue > 0) part.BuildingDepreciatedMarketValue = bldg.DepreciatedMarketValue;
        }

        if (land != null)
        {
            if (!string.IsNullOrWhiteSpace(land.BoundaryNorth)) part.BoundaryNorth = land.BoundaryNorth;
            if (!string.IsNullOrWhiteSpace(land.BoundarySouth)) part.BoundarySouth = land.BoundarySouth;
            if (!string.IsNullOrWhiteSpace(land.BoundaryEast)) part.BoundaryEast = land.BoundaryEast;
            if (!string.IsNullOrWhiteSpace(land.BoundaryWest)) part.BoundaryWest = land.BoundaryWest;
            if (!string.IsNullOrWhiteSpace(land.Nremarks)) part.Nremarks = land.Nremarks;
            if (!string.IsNullOrWhiteSpace(land.Sremarks)) part.Sremarks = land.Sremarks;
            if (!string.IsNullOrWhiteSpace(land.Eremarks)) part.Eremarks = land.Eremarks;
            if (!string.IsNullOrWhiteSpace(land.Wremarks)) part.Wremarks = land.Wremarks;
        }

        if (machine != null)
        {
            if (machine.OriginalCost > 0) part.AcquisitionCost = machine.OriginalCost;
            if (!string.IsNullOrWhiteSpace(machine.LandOwnerName)) part.LandOwnerName = machine.LandOwnerName;
            if (!string.IsNullOrWhiteSpace(machine.LandPin)) part.LandPin = machine.LandPin;
            if (!string.IsNullOrWhiteSpace(machine.BuildingReferenceName)) part.BuildingReferenceName = machine.BuildingReferenceName;
            if (!string.IsNullOrWhiteSpace(machine.BuildingPin)) part.BuildingPin = machine.BuildingPin;
        }

        if (dtoTdn != null)
        {
            if (!string.IsNullOrWhiteSpace(dtoTdn.PIN)) part.PIN = dtoTdn.PIN;
            if (!string.IsNullOrWhiteSpace(dtoTdn.ARPNo)) part.ARPNo = dtoTdn.ARPNo;
            if (!string.IsNullOrWhiteSpace(dtoTdn.SectionNo)) part.SectionNo = dtoTdn.SectionNo;
            if (!string.IsNullOrWhiteSpace(dtoTdn.SurveyNo)) part.SurveyNumber = dtoTdn.SurveyNo;
            if (!string.IsNullOrWhiteSpace(dtoTdn.CadastralSurveyNo) && string.IsNullOrWhiteSpace(part.SurveyNumber)) part.SurveyNumber = dtoTdn.CadastralSurveyNo;
            if (!string.IsNullOrWhiteSpace(dtoTdn.LotNo)) part.LotNumber = dtoTdn.LotNo;
            if (!string.IsNullOrWhiteSpace(dtoTdn.TitleNo)) part.TitleNo = dtoTdn.TitleNo;
            if (!string.IsNullOrWhiteSpace(dtoTdn.TCTNo) && string.IsNullOrWhiteSpace(part.TitleNo)) part.TitleNo = dtoTdn.TCTNo;
            if (!string.IsNullOrWhiteSpace(dtoTdn.PropertyTypeAccordUse)) part.PropertyTypeAccordUse = dtoTdn.PropertyTypeAccordUse;
            if (!string.IsNullOrWhiteSpace(dtoTdn.OtherPropertyTypeAccordUse)) part.OtherPropertyTypeAccordUse = dtoTdn.OtherPropertyTypeAccordUse;
            if (!string.IsNullOrWhiteSpace(dtoTdn.DistrictOrBarangay)) part.DistrictOrBarangay = dtoTdn.DistrictOrBarangay;
            if (!string.IsNullOrWhiteSpace(dtoTdn.CityMunicipality)) part.CityMunicipality = dtoTdn.CityMunicipality;
            if (!string.IsNullOrWhiteSpace(dtoTdn.Province)) part.Province = dtoTdn.Province;
            if (!string.IsNullOrWhiteSpace(dtoTdn.AssessmentBasis)) part.AssessmentBasis = dtoTdn.AssessmentBasis;
            if (dtoTdn.BackTaxesYearFrom.HasValue) part.BackTaxesYearFrom = dtoTdn.BackTaxesYearFrom.Value;
            if (dtoTdn.BackTaxesYearTo.HasValue) part.BackTaxesYearTo = dtoTdn.BackTaxesYearTo.Value;
            if (dtoTdn.UnitValue.HasValue && dtoTdn.UnitValue.Value > 0) part.UnitValue = dtoTdn.UnitValue.Value;
            if (dtoTdn.TotalArea.HasValue && dtoTdn.TotalArea.Value > 0)
            {
                part.AreaSqMeters = dtoTdn.TotalArea.Value;
                part.TotalBuildingAreaSqm = dtoTdn.TotalArea.Value;
            }
            if (!string.IsNullOrWhiteSpace(dtoTdn.FloorPlanSketchPath))
            {
                part.BuildingFloorPlanSketchPath = dtoTdn.FloorPlanSketchPath;
                td.FloorPlanSketchPath = dtoTdn.FloorPlanSketchPath;
            }
        }

        if (attachments != null && attachments.Count > 0)
        {
            part.AttachmentsJson = JsonSerializer.Serialize(attachments, _jsonOptions);
        }
    }

    private List<MobileAttachmentDTO> ProcessAndSaveAttachments(List<MobileAttachmentDTO>? attachments, string trackingNo)
    {
        if (attachments == null || attachments.Count == 0) return new List<MobileAttachmentDTO>();

        var result = new List<MobileAttachmentDTO>();
        string cleanTracking = string.IsNullOrWhiteSpace(trackingNo) ? "general" : trackingNo.Trim();
        string targetDir = Path.Combine(_env.WebRootPath, "uploads", "InspectionAttachments", cleanTracking);

        foreach (var att in attachments)
        {
            if (!string.IsNullOrWhiteSpace(att.Base64Data))
            {
                try
                {
                    if (!System.IO.Directory.Exists(targetDir)) System.IO.Directory.CreateDirectory(targetDir);

                    string raw = att.Base64Data;
                    if (raw.Contains(",")) raw = raw.Substring(raw.IndexOf(",") + 1);
                    byte[] bytes = Convert.FromBase64String(raw);

                    string safeExt = Path.GetExtension(att.FileName);
                    if (string.IsNullOrWhiteSpace(safeExt))
                    {
                        safeExt = att.FileType == "document" ? ".pdf" : ".jpg";
                    }

                    string fileName = $"{Guid.NewGuid():N}_{Regex.Replace(Path.GetFileNameWithoutExtension(att.FileName ?? "att"), @"[^a-zA-Z0-9_\-]", "_")}{safeExt}";
                    string filePath = Path.Combine(targetDir, fileName);

                    System.IO.File.WriteAllBytes(filePath, bytes);
                    att.FileUrl = $"/uploads/InspectionAttachments/{cleanTracking}/{fileName}";
                    att.Base64Data = null; // Do not persist base64 data in db json
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving base64 attachment: {ex.Message}");
                }
            }

            result.Add(att);
        }

        return result;
    }

    private async Task<TaxDeclaration?> FindTaxDeclaration(long? numericId, PropertyKind defaultKind)
    {
        if (numericId.HasValue && numericId.Value > 0)
        {
            var all = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .ToListAsync();

            var matched = all.FirstOrDefault(t => GuidToNumericId(t.Id) == numericId.Value);
            if (matched != null) return matched;
        }

        return await _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
            .Where(t => t.Kind == defaultKind)
            .OrderByDescending(t => t.DateEncoded)
            .FirstOrDefaultAsync();
    }

    private async Task<TaxDeclaration?> FindOrMatchTaxDeclaration(long? numericId, string? arpNo, string? pin, PropertyKind kind)
    {
        if (numericId.HasValue && numericId.Value > 0)
        {
            var all = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ToListAsync();

            var match = all.FirstOrDefault(t => GuidToNumericId(t.Id) == numericId.Value);
            if (match != null) return match;
        }

        if (!string.IsNullOrWhiteSpace(arpNo))
        {
            var match = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == arpNo.Trim());
            if (match != null) return match;
        }

        if (!string.IsNullOrWhiteSpace(pin))
        {
            var match = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .FirstOrDefaultAsync(t => t.PropertyIndexNumber == pin.Trim());
            if (match != null) return match;
        }

        return null;
    }

    private static void ApplyCoreTDNToEntity(TDNDTO dto, TaxDeclaration td)
    {
        if (!string.IsNullOrWhiteSpace(dto.DisplayName)) td.OwnerName = dto.DisplayName;
        if (!string.IsNullOrWhiteSpace(dto.OwnerAddress)) td.OwnerAddress = dto.OwnerAddress;
        if (!string.IsNullOrWhiteSpace(dto.PropAdminName)) td.AdministratorName = dto.PropAdminName;
        if (!string.IsNullOrWhiteSpace(dto.PropAdminAddress)) td.AdministratorAddress = dto.PropAdminAddress;
        if (!string.IsNullOrWhiteSpace(dto.PropAdminContactNo)) td.AdministratorContactNo = dto.PropAdminContactNo;
        if (!string.IsNullOrWhiteSpace(dto.Memoranda)) td.Memoranda = dto.Memoranda;
        if (dto.TotalMarketValue.HasValue && dto.TotalMarketValue.Value > 0) td.MarketValue = dto.TotalMarketValue.Value;
        if (dto.TotalAssessedValue.HasValue && dto.TotalAssessedValue.Value > 0) td.AssessedValue = dto.TotalAssessedValue.Value;
        if (!string.IsNullOrWhiteSpace(dto.TransactionCode)) td.TransactionCode = dto.TransactionCode;
        if (dto.YearEffectivity.HasValue) td.EffectivityYear = dto.YearEffectivity.Value;
        if (dto.QuarterEffectivity.HasValue) td.EffectivityQuarter = $"{dto.QuarterEffectivity.Value}th Quarter";
        if (dto.TotalArea.HasValue && dto.TotalArea.Value > 0) td.AreaSqMeters = dto.TotalArea.Value;
        if (!string.IsNullOrWhiteSpace(dto.LotNo)) td.LotNo = dto.LotNo;
        if (!string.IsNullOrWhiteSpace(dto.PropLocationStreet)) td.StreetNo = dto.PropLocationStreet;
        td.Taxability = dto.Taxability ? "Taxable" : "Exempt";
        if (!string.IsNullOrWhiteSpace(dto.PIN)) td.PropertyIndexNumber = dto.PIN;
        if (!string.IsNullOrWhiteSpace(dto.ARPNo)) td.ARPNo = dto.ARPNo;
        if (!string.IsNullOrWhiteSpace(dto.SectionNo)) td.SectionNo = dto.SectionNo;
        if (!string.IsNullOrWhiteSpace(dto.SurveyNo)) td.SurveyNo = dto.SurveyNo;
        if (!string.IsNullOrWhiteSpace(dto.CadastralSurveyNo) && string.IsNullOrWhiteSpace(td.SurveyNo)) td.SurveyNo = dto.CadastralSurveyNo;
        if (!string.IsNullOrWhiteSpace(dto.TitleNo)) td.TitleNo = dto.TitleNo;
        if (!string.IsNullOrWhiteSpace(dto.TCTNo) && string.IsNullOrWhiteSpace(td.TitleNo)) td.TitleNo = dto.TCTNo;
        if (!string.IsNullOrWhiteSpace(dto.PropertyTypeAccordUse)) td.BuildingUseType = dto.PropertyTypeAccordUse;
    }

    private TDNDTO BuildTDNDTO(TaxDeclaration td, string? trackingNo = null, long? formWorkflowId = null, string? sketchPath = null, List<MobileAttachmentDTO>? attachments = null, int? partitionItemIndex = null, string? partitionIndex = null)
    {
        int effQtrInt = 1;
        if (!string.IsNullOrEmpty(td.EffectivityQuarter))
        {
            var digits = new string(td.EffectivityQuarter.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int q)) effQtrInt = q;
        }

        return new TDNDTO
        {
            TDNId = GuidToNumericId(td.Id),
            TrackingNo = trackingNo,
            FormWorkflowId = formWorkflowId,
            PartitionItemIndex = partitionItemIndex,
            PartitionIndex = partitionIndex,
            DeclarationNo = td.TaxDeclarationNumber,
            ARPNo = !string.IsNullOrWhiteSpace(td.ARPNo) ? td.ARPNo : td.TaxDeclarationNumber,
            PIN = td.PropertyIndexNumber,
            DisplayName = td.OwnerName,
            OwnerAddress = td.OwnerAddress,
            PropAdminName = td.AdministratorName,
            PropAdminAddress = td.AdministratorAddress,
            PropAdminContactNo = td.AdministratorContactNo,
            PropLocationStreet = td.StreetNo,
            Location = $"{td.DistrictOrBarangay}, {td.Municipality}",
            CadastralSurveyNo = td.CadastralSurveyNo,
            SurveyNo = td.SurveyNo,
            LotNo = td.LotNo,
            SectionNo = td.SectionNo,
            ParcelNo = td.ParcelNo,
            TCTNo = td.TitleNo,
            TitleNo = td.TitleNo,
            DistrictOrBarangay = td.DistrictOrBarangay,
            CityMunicipality = td.Municipality,
            Province = "South Cotabato",
            AssessmentBasis = td.AssessmentBasis,
            BackTaxesYearFrom = td.BackTaxesYearFrom,
            BackTaxesYearTo = td.BackTaxesYearTo,
            PropertyTypeAccordUse = td.BuildingDetails?.BuildingUseType ?? td.BuildingUseType,
            OtherPropertyTypeAccordUse = td.BuildingDetails?.OtherBuildingUseType,
            TransactionCode = td.TransactionCode,
            YearEffectivity = td.EffectivityYear,
            QuarterEffectivity = effQtrInt,
            Memoranda = td.Memoranda,
            Taxability = string.Equals(td.Taxability, "Taxable", StringComparison.OrdinalIgnoreCase),
            TotalMarketValue = td.MarketValue,
            TotalAssessedValue = td.AssessedValue,
            TotalArea = td.BuildingDetails?.TotalFloorArea > 0 ? td.BuildingDetails.TotalFloorArea : td.AreaSqMeters,
            Unit = "sq.m.",
            FloorPlanSketchPath = sketchPath ?? td.FloorPlanSketchPath,
            Attachments = attachments ?? new()
        };
    }

    private TDNBuildingDTO BuildBuildingDTO(TaxDeclaration td, string? trackingNo = null, long? formWorkflowId = null, string? sketchPath = null, List<MobileAttachmentDTO>? attachments = null, int? partitionItemIndex = null, string? partitionIndex = null)
    {
        var b = td.BuildingDetails ?? td.EnsureBuildingDetails();
        return new TDNBuildingDTO
        {
            TDN = BuildTDNDTO(td, trackingNo, formWorkflowId, sketchPath, attachments, partitionItemIndex, partitionIndex),
            CAST_TDNBuildingDTO = new CAST_TDNBuildingDTO
            {
                CAST_TDNBuildingId = GuidToNumericId(b.Id),
                TDNId = GuidToNumericId(td.Id),
                STREET = b.StreetNo ?? td.StreetNo,
                LotNo = b.LandLotNo ?? td.LotNo,
                SurveyNo = b.LandSurveyNo ?? td.SurveyNo ?? td.CadastralSurveyNo,
                TCTNo = b.LandTitleNo ?? td.TitleNo,
                LandArea = b.LandAreaSqm > 0 ? b.LandAreaSqm.ToString("F2") : td.AreaSqMeters.ToString("F2"),
                ARPNo = !string.IsNullOrWhiteSpace(td.ARPNo) ? td.ARPNo : td.TaxDeclarationNumber,
                PermitNo = b.BuildingPermitNo,
                Age = b.BuildingAgeYears,
                StoreyNo = b.NoOfStoreys,
                AreaPerFloor = b.NoOfStoreys > 0 ? Math.Round(b.TotalFloorArea / b.NoOfStoreys, 2) : b.TotalFloorArea,
                TotalbldgArea = b.TotalFloorArea > 0 ? b.TotalFloorArea : td.AreaSqMeters,
                SubtotalValCompArea = b.ReplacementCostNew,
                TotalAddItem = b.TotalAdditionalItemsValue,
                DepreciationRate = b.BuildingDepreciationPercent,
                DepreciationAccumulated = b.BuildingDepreciationAccumulated,
                DepreciationMarketval = b.DepreciatedMarketValue,
                TotalMarketVal = b.MarketValue > 0 ? b.MarketValue : td.MarketValue,
                TotalAssessedVal = b.AssessedValue > 0 ? b.AssessedValue : td.AssessedValue,
                StucturalType = b.StructuralType,
                OtherStructure = b.StructuralType,
                PropertyTypeAccordUse = b.BuildingUseType ?? td.BuildingUseType,
                OtherPropertyTypeAccordUse = b.OtherBuildingUseType,
                MailingAddress = b.LandOwnerAddress ?? td.OwnerAddress,
                TIN = b.LandOwnerTin ?? td.OwnerTin,
                MobileNo = b.LandOwnerPhone ?? td.OwnerPhone,
                EmailAddress = b.LandOwnerEmail ?? td.OwnerEmail,
                TransactionCode = td.TransactionCode
            }
        };
    }

    private TDNLandDTO BuildLandDTO(TaxDeclaration td, string? trackingNo = null, long? formWorkflowId = null, string? sketchPath = null, List<MobileAttachmentDTO>? attachments = null, int? partitionItemIndex = null, string? partitionIndex = null)
    {
        var l = td.LandDetails ?? td.EnsureLandDetails();
        return new TDNLandDTO
        {
            TDN = BuildTDNDTO(td, trackingNo, formWorkflowId, sketchPath, attachments, partitionItemIndex, partitionIndex),
            TDNLand = new TDNLandDataDTO
            {
                TDNLandId = GuidToNumericId(l.Id),
                TDNId = GuidToNumericId(td.Id),
                NorthBoundary = l.BoundaryNorth,
                SouthBoundary = l.BoundarySouth,
                EastBoundary = l.BoundaryEast,
                WestBoundary = l.BoundaryWest,
                Nremarks = l.Nremarks,
                Sremarks = l.Sremarks,
                Eremarks = l.Eremarks,
                Wremarks = l.Wremarks
            }
        };
    }

    private TDNMachineDTO BuildMachineryDTO(TaxDeclaration td, string? trackingNo = null, long? formWorkflowId = null, string? sketchPath = null, List<MobileAttachmentDTO>? attachments = null, int? partitionItemIndex = null, string? partitionIndex = null)
    {
        var m = td.MachineryDetails ?? td.EnsureMachineryDetails();
        var details = m.TDNMachineryDetails?.Select(d => new TDNMachineDetailDTO
        {
            TDNMachineDetailId = GuidToNumericId(d.Id),
            TDNMachineId = GuidToNumericId(m.Id),
            KindOfMachinery = d.KindOfMachinery ?? "Machinery",
            BrandModel = d.BrandModel,
            Capacity = d.Capacity,
            AcquiredDate = d.AcquiredDate,
            ConditionWhenAcquired = d.ConditionWhenAcquired,
            EconomicLifeEstimated = d.EconomicLifeEstimated,
            OriginalCost = d.OriginalCost,
            MarketValue = d.OriginalCost,
            AssessmentLevel = 75,
            AssessedValue = Math.Round(d.OriginalCost * 0.75m, 2),
            DepreciatedValue = d.OriginalCost,
            DepreciationTotalPercentage = 0
        }).ToList() ?? new List<TDNMachineDetailDTO>();

        if (details.Count == 0)
        {
            details.Add(new TDNMachineDetailDTO
            {
                TDNMachineDetailId = GuidToNumericId(m.Id),
                TDNMachineId = GuidToNumericId(m.Id),
                KindOfMachinery = m.DescriptionBrand ?? "Machinery / Equipment",
                BrandModel = m.DescriptionBrand,
                Capacity = m.RatedCapacity,
                OriginalCost = m.OriginalCost,
                EconomicLifeEstimated = m.EconomicLifeYears,
                ConditionWhenAcquired = m.ConditionWhenAcquired,
                AcquiredDate = m.DateAcquired,
                MarketValue = td.MarketValue,
                AssessedValue = td.AssessedValue
            });
        }

        return new TDNMachineDTO
        {
            TDN = BuildTDNDTO(td, trackingNo, formWorkflowId, sketchPath, attachments, partitionItemIndex, partitionIndex),
            TDNMachine = new TDNMachineDataDTO
            {
                TDNMachineId = GuidToNumericId(m.Id),
                TDNId = GuidToNumericId(td.Id),
                LandOwnerPIN = m.LandPin,
                BuildingOwnerPIN = m.BuildingPin,
                LandOwner = m.LandOwnerName,
                BuildingOwner = m.BuildingReferenceName
            },
            TDNMachineDetail = details
        };
    }
}
