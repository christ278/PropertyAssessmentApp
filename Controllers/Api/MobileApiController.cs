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
    private readonly IPropertyAssessmentRequestService _rpaService;
    private readonly IRequestForInspectionService _rfiService;
    private readonly IAssessmentService _assessmentService;

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
        IWebHostEnvironment env,
        IPropertyAssessmentRequestService rpaService,
        IRequestForInspectionService rfiService,
        IAssessmentService assessmentService)
    {
        _context = context;
        _workflowService = workflowService;
        _hubContext = hubContext;
        _workflowNotifier = workflowNotifier;
        _env = env;
        _rpaService = rpaService;
        _rfiService = rfiService;
        _assessmentService = assessmentService;
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

        private List<MobilePartitionSummaryDTO> BuildPartitionSummaries(List<AssessmentPartitionItem> partitions, string defaultOwner, int fallbackPhotoCount = 0, string? parentKind = null)
    {
        var result = new List<MobilePartitionSummaryDTO>();
        for (int i = 0; i < partitions.Count; i++)
        {
            var p = partitions[i];
            if (!string.IsNullOrWhiteSpace(parentKind) && (string.IsNullOrWhiteSpace(p.Kind) || (string.Equals(parentKind, "Land", StringComparison.OrdinalIgnoreCase) && string.Equals(p.Kind, "Building", StringComparison.OrdinalIgnoreCase))))
            {
                p.Kind = parentKind;
            }
            int fallbackKindId = ParsePropertyKindId(parentKind, 1);
            int pKindId = ParsePropertyKindId(p.Kind, fallbackKindId);
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

            if (pPhotoCount == 0 && fallbackPhotoCount > 0)
            {
                pPhotoCount = fallbackPhotoCount;
            }

            result.Add(new MobilePartitionSummaryDTO
            {
                Index = i,
                PartitionIndex = !string.IsNullOrWhiteSpace(p.PartitionIndex) ? p.PartitionIndex : ((char)('A' + i)).ToString(),
                Kind = !string.IsNullOrWhiteSpace(p.Kind) ? p.Kind : (!string.IsNullOrWhiteSpace(parentKind) ? parentKind : "Land"),
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
                SketchUrl = ResolveFullUrl(p.BuildingFloorPlanSketchPath),
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

    // Kind-aware resolver: never lets a Land edit land on a Building/Machinery partition (and vice versa).
    private static int ResolvePartitionTargetIndex(List<AssessmentPartitionItem> partitions, TDNDTO? dtoTdn, string expectedKind)
    {
        int idx = ResolvePartitionTargetIndex(partitions, dtoTdn);
        if (partitions.Count == 0) return idx;

        bool KindOk(AssessmentPartitionItem p) =>
            string.IsNullOrWhiteSpace(p.Kind) || string.Equals(p.Kind, expectedKind, StringComparison.OrdinalIgnoreCase);

        if (KindOk(partitions[idx])) return idx;

        string? tdn = dtoTdn?.ARPNo ?? dtoTdn?.DeclarationNo ?? dtoTdn?.TDN;
        string? pin = dtoTdn?.PIN;
        var sameKind = Enumerable.Range(0, partitions.Count).Where(i => KindOk(partitions[i])).ToList();

        int byTdn = sameKind.FirstOrDefault(i => !string.IsNullOrWhiteSpace(tdn) &&
            string.Equals(partitions[i].SystemGeneratedTDN, tdn!.Trim(), StringComparison.OrdinalIgnoreCase), -1);
        if (byTdn >= 0) return byTdn;
        int byPin = sameKind.FirstOrDefault(i => !string.IsNullOrWhiteSpace(pin) &&
            string.Equals(partitions[i].PIN, pin!.Trim(), StringComparison.OrdinalIgnoreCase), -1);
        if (byPin >= 0) return byPin;
        if (sameKind.Count == 1) return sameKind[0];

        // If partitions in this transaction previously inherited a default or mismatched Kind (e.g. "Building" default on a Land transaction),
        // but this edit explicitly targets this partition from the matching form, allow it and reclassify the partition to expectedKind.
        if (idx >= 0 && idx < partitions.Count)
        {
            partitions[idx].Kind = expectedKind;
            return idx;
        }

        throw new InvalidOperationException(
            $"Could not match this {expectedKind} edit to a {expectedKind} partition. The resolved partition is '{partitions[idx].Kind}'.");
    }

    private static int ResolvePartitionTargetIndex(List<AssessmentPartitionItem> partitions, TDNDTO? dtoTdn)
    {
        if (dtoTdn == null || partitions.Count == 0) return 0;

        if (!string.IsNullOrWhiteSpace(dtoTdn.PartitionIndex))
        {
            var key = dtoTdn.PartitionIndex.Trim();
            // Only trust the letter when it is unique; duplicate labels (e.g. two "A") would always hit the first item.
            var matches = Enumerable.Range(0, partitions.Count)
                .Where(i => string.Equals(partitions[i].PartitionIndex, key, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 1) return matches[0];
        }

        if (dtoTdn.PartitionItemIndex.HasValue && dtoTdn.PartitionItemIndex.Value >= 0 && dtoTdn.PartitionItemIndex.Value < partitions.Count)
        {
            return dtoTdn.PartitionItemIndex.Value;
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

    private string ResolveFullUrl(string? rel)
    {
        if (string.IsNullOrWhiteSpace(rel)) return string.Empty;
        if (rel.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || rel.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return rel;
        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        string clean = rel.Replace('\\', '/');
        if (!clean.StartsWith("/")) clean = "/" + clean;
        return $"{baseUrl}{clean}";
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
            .Select(r => new { r.Id, r.TrackingNumber, r.Kind, r.RefTaxDeclarationId, r.PrevTDN, r.PrevPIN, r.NewOwnerName, r.PrevOwner, r.Requestor, r.RequestDate, r.GeneratedPartitionsJson, r.WebRelativePath })
            .ToListAsync();

        var allRfis = await _context.RequestForInspections
            .AsNoTracking()
            .Select(r => new { r.Id, r.TrackingNumber, r.Kind, r.TaxDeclarationId, r.TaxDeclarationNumber, r.PropertyIndexNumber, r.OwnerName, r.Representative, r.RequestDate, r.GeneratedPartitionsJson, r.WebRelativePath })
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

                    int dossierPhotos = !string.IsNullOrWhiteSpace(item.RequestType == "PropertyAssessmentRequest" ? allRpas.FirstOrDefault(r => r.Id == item.RequestId)?.WebRelativePath : allRfis.FirstOrDefault(r => r.Id == item.RequestId)?.WebRelativePath) ? 1 : 0;
                    partitionSummaries = BuildPartitionSummaries(partitions, ownerName, dossierPhotos, rpa.Kind);

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

                    transType = ParsePropertyKindId(rpa.Kind, 1);
                    if (string.Equals(rpa.Kind, "Land", StringComparison.OrdinalIgnoreCase))
                    {
                        transType = 1;
                    }
                    else if (string.Equals(rpa.Kind, "Machinery", StringComparison.OrdinalIgnoreCase))
                    {
                        transType = 3;
                    }
                    else if (firstPart != null && !string.IsNullOrWhiteSpace(firstPart.Kind))
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

                    int dossierPhotos = !string.IsNullOrWhiteSpace(item.RequestType == "PropertyAssessmentRequest" ? allRpas.FirstOrDefault(r => r.Id == item.RequestId)?.WebRelativePath : allRfis.FirstOrDefault(r => r.Id == item.RequestId)?.WebRelativePath) ? 1 : 0;
                    partitionSummaries = BuildPartitionSummaries(partitions, ownerName, dossierPhotos, rfi.Kind);

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

                    transType = ParsePropertyKindId(rfi.Kind, 1);
                    if (string.Equals(rfi.Kind, "Land", StringComparison.OrdinalIgnoreCase))
                    {
                        transType = 1;
                    }
                    else if (string.Equals(rfi.Kind, "Machinery", StringComparison.OrdinalIgnoreCase))
                    {
                        transType = 3;
                    }
                    else if (firstPart != null && !string.IsNullOrWhiteSpace(firstPart.Kind))
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

        private List<MobileAttachmentDTO> ResolveAllAttachmentsForPartition(
        AssessmentPartitionItem? part,
        int partIndex,
        PropertyAssessmentRequest? rpa,
        RequestForInspection? rfi,
        TaxDeclaration? td)
    {
        var list = part != null ? ParseAttachments(part.AttachmentsJson) : new List<MobileAttachmentDTO>();

        // 1. If partition has no attachments, inherit from other partitions if available
        if (list.Count == 0 && rpa != null && !string.IsNullOrWhiteSpace(rpa.GeneratedPartitionsJson))
        {
            var otherParts = PartitionJsonHelper.ParseList(rpa.GeneratedPartitionsJson);
            var firstWithAtts = otherParts.FirstOrDefault(op => !string.IsNullOrWhiteSpace(op.AttachmentsJson));
            if (firstWithAtts != null)
            {
                var inherited = ParseAttachments(firstWithAtts.AttachmentsJson);
                foreach (var att in inherited)
                {
                    if (!list.Any(a => string.Equals(a.FileUrl, att.FileUrl, StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Add(att);
                    }
                }
            }
        }

        // 2. Only if partition still has no attachments, include main file upload / inspection dossier photo as fallback (WebRelativePath)
        string? webRelPath = rpa?.WebRelativePath ?? rfi?.WebRelativePath;
        string? attFileName = rpa?.AttachmentFileName ?? rfi?.AttachmentFileName;
        string? attFileSize = rpa?.AttachmentFileSize ?? rfi?.AttachmentFileSize;
        DateTime? uploadDate = rpa?.RequestDate ?? rfi?.RequestDate;

        if (list.Count == 0 && !string.IsNullOrWhiteSpace(webRelPath))
        {
            string normalizedUrl = webRelPath.Replace('\\', '/');
            if (!normalizedUrl.StartsWith("/")) normalizedUrl = "/" + normalizedUrl;

            if (!list.Any(a => string.Equals(a.FileUrl, normalizedUrl, StringComparison.OrdinalIgnoreCase)))
            {
                string ext = Path.GetExtension(normalizedUrl).ToLowerInvariant();
                bool isImg = ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" || ext == ".gif";
                list.Add(new MobileAttachmentDTO
                {
                    Id = $"main_web_{Math.Abs(normalizedUrl.GetHashCode())}",
                    FileName = !string.IsNullOrWhiteSpace(attFileName) ? attFileName : Path.GetFileName(normalizedUrl),
                    FileUrl = normalizedUrl,
                    FileType = isImg ? "image" : "document",
                    FileSize = attFileSize ?? "Dossier",
                    UploadedAt = uploadDate ?? DateTime.UtcNow
                });
            }
        }

        // 3. Only if partition still has no attachments, include any TDNPropertyImages matching this TD as fallback
        if (list.Count == 0 && td != null && td.Id != Guid.Empty)
        {
            try
            {
                var propImages = _context.TDNPropertyImages.Where(img => img.TaxDeclarationId == td.Id).ToList();
                foreach (var img in propImages)
                {
                    if (!string.IsNullOrWhiteSpace(img.ImagePath))
                    {
                        string normImg = img.ImagePath.Replace('\\', '/');
                        if (!normImg.StartsWith("/")) normImg = "/" + normImg;
                        if (!list.Any(a => string.Equals(a.FileUrl, normImg, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Add(new MobileAttachmentDTO
                            {
                                Id = $"tdn_img_{img.Id}",
                                FileName = !string.IsNullOrWhiteSpace(img.OriginalFileName) ? img.OriginalFileName : Path.GetFileName(normImg),
                                FileUrl = normImg,
                                FileType = "image",
                                FileSize = "Photo",
                                Caption = img.Caption,
                                UploadedAt = img.UploadedAt
                            });
                        }
                    }
                }
            }
            catch { }
        }

        // Filter out any dead/legacy blob URLs that cannot survive across browser/app sessions
        list.RemoveAll(a => !string.IsNullOrEmpty(a.FileUrl) && a.FileUrl.StartsWith("blob:", StringComparison.OrdinalIgnoreCase));

        // Ensure all fileUrls have proper fully-qualified URL based on incoming request host
        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        foreach (var att in list)
        {
            if (!string.IsNullOrWhiteSpace(att.FileUrl))
            {
                att.FileUrl = att.FileUrl.Replace('\\', '/');
                if (!att.FileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !att.FileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    if (!att.FileUrl.StartsWith("/")) att.FileUrl = "/" + att.FileUrl;
                    att.FileUrl = $"{baseUrl}{att.FileUrl}";
                }
            }
        }

        return list;
    }

    // =========================================================================
    // DEDICATED PARTITION MAPPERS FOR DOWNLOAD (SEPARATE FOR LAND, BUILDING, MACHINERY)
    // =========================================================================

    private TDNDTO BuildCoreTDNDTOFromPartition(
        AssessmentPartitionItem part,
        int partIndex,
        string trackingNo,
        long formWorkflowId,
        TaxDeclaration? baseTd = null,
        string? sketchPath = null,
        List<MobileAttachmentDTO>? attachments = null)
    {
        int effQtrInt = 1;
        string? effQtrStr = !string.IsNullOrWhiteSpace(part.EffectivityQuarter) ? part.EffectivityQuarter : baseTd?.EffectivityQuarter;
        if (!string.IsNullOrEmpty(effQtrStr))
        {
            var digits = new string(effQtrStr.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int q)) effQtrInt = q;
        }

        string pLetter = !string.IsNullOrWhiteSpace(part.PartitionIndex) ? part.PartitionIndex : ((char)('A' + partIndex)).ToString();
        string cleanArp = !string.IsNullOrWhiteSpace(part.ARPNo) ? part.ARPNo : (!string.IsNullOrWhiteSpace(part.SystemGeneratedTDN) ? part.SystemGeneratedTDN : (baseTd?.ARPNo ?? baseTd?.TaxDeclarationNumber ?? ""));
        string cleanPin = !string.IsNullOrWhiteSpace(part.PIN) ? part.PIN : (baseTd != null && !string.IsNullOrWhiteSpace(baseTd.PropertyIndexNumber) ? $"{baseTd.PropertyIndexNumber}-{pLetter}" : "");
        string ownerName = !string.IsNullOrWhiteSpace(part.NewOwnerName) ? part.NewOwnerName : (baseTd?.OwnerName ?? "");
        string ownerAddress = !string.IsNullOrWhiteSpace(part.OwnerAddress) ? part.OwnerAddress : (baseTd?.OwnerAddress ?? "");
        string street = !string.IsNullOrWhiteSpace(part.StreetLocation) ? part.StreetLocation : (baseTd?.StreetNo ?? "");
        string brgy = !string.IsNullOrWhiteSpace(part.DistrictOrBarangay) ? part.DistrictOrBarangay : (baseTd?.DistrictOrBarangay ?? "");
        string city = !string.IsNullOrWhiteSpace(part.CityMunicipality) ? part.CityMunicipality : (baseTd?.Municipality ?? "Koronadal City");
        string prov = !string.IsNullOrWhiteSpace(part.Province) ? part.Province : "South Cotabato";
        string? rawSketch = sketchPath ?? part.BuildingFloorPlanSketchPath ?? baseTd?.FloorPlanSketchPath;

        return new TDNDTO
        {
            TDNId = baseTd != null ? GuidToNumericId(baseTd.Id) : formWorkflowId,
            TrackingNo = trackingNo,
            FormWorkflowId = formWorkflowId,
            PartitionItemIndex = partIndex,
            PartitionIndex = pLetter,
            DeclarationNo = !string.IsNullOrWhiteSpace(part.SystemGeneratedTDN) ? part.SystemGeneratedTDN : (baseTd?.TaxDeclarationNumber ?? cleanArp),
            ARPNo = cleanArp,
            PIN = cleanPin,
            DisplayName = ownerName,
            OwnerAddress = ownerAddress,
            OwnerTIN = !string.IsNullOrWhiteSpace(part.OwnerTIN) ? part.OwnerTIN : (baseTd?.OwnerTin ?? string.Empty),
            OwnerContactNo = !string.IsNullOrWhiteSpace(part.OwnerContactNo) ? part.OwnerContactNo : (baseTd?.OwnerPhone ?? string.Empty),
            OwnerEmail = part.OwnerEmail ?? baseTd?.OwnerEmail,
            OwnerGender = part.OwnerGender,
            OwnerCivilStatus = part.OwnerCivilStatus,

            IsAdministrator = part.IsAdministrator,
            IsBeneficialUser = part.IsBeneficialUser,
            IsPossessor = part.IsPossessor,
            AdministratorName = part.AdministratorName ?? baseTd?.AdministratorName,
            AdministratorAddress = part.AdministratorAddress ?? baseTd?.AdministratorAddress,
            AdministratorContactNo = part.AdministratorContactNo ?? baseTd?.AdministratorContactNo,
            AdministratorTin = part.AdministratorTin,
            AdministratorEmail = part.AdministratorEmail,
            AdministratorGender = part.AdministratorGender,
            PropAdminName = part.AdministratorName ?? baseTd?.AdministratorName,
            PropAdminAddress = part.AdministratorAddress ?? baseTd?.AdministratorAddress,
            PropAdminContactNo = part.AdministratorContactNo ?? baseTd?.AdministratorContactNo,

            PropLocationStreet = street,
            Location = !string.IsNullOrWhiteSpace(part.DisplayPropertyLocation) ? part.DisplayPropertyLocation : $"{brgy}, {city}",
            CadastralSurveyNo = !string.IsNullOrWhiteSpace(part.CadastralSurveyNo) ? part.CadastralSurveyNo : (partIndex == 0 ? baseTd?.CadastralSurveyNo : string.Empty),
            SurveyNo = !string.IsNullOrWhiteSpace(part.SurveyNumber) ? part.SurveyNumber : (partIndex == 0 ? baseTd?.SurveyNo : string.Empty),
            LotNo = !string.IsNullOrWhiteSpace(part.LotNumber) ? part.LotNumber : $"Lot {partIndex + 1}-{pLetter}",
            BlockNo = !string.IsNullOrWhiteSpace(part.BlockNumber) ? part.BlockNumber : (baseTd?.BlockNo ?? string.Empty),
            SectionNo = !string.IsNullOrWhiteSpace(part.SectionNo) ? part.SectionNo : (baseTd?.SectionNo ?? string.Empty),
            ParcelNo = !string.IsNullOrWhiteSpace(part.ParcelNo) ? part.ParcelNo : string.Empty,
            TCTNo = part.TitleNo ?? string.Empty,
            TitleNo = part.TitleNo ?? string.Empty,
            TCTDate = part.DateRegistered,
            DateRegistration = part.DateRegistered,
            DateRegistered = part.DateRegistered,
            RegistrationStatus = !string.IsNullOrWhiteSpace(part.RegistrationStatus) ? part.RegistrationStatus : (baseTd?.RegistrationStatus ?? "Titled"),
            TitleType = !string.IsNullOrWhiteSpace(part.TitleType) ? part.TitleType : (baseTd?.TitleType ?? "OCT"),
            DistrictOrBarangay = brgy,
            CityMunicipality = city,
            Province = prov,
            Purok = part.Purok ?? string.Empty,

            AssessmentBasis = !string.IsNullOrWhiteSpace(part.AssessmentBasis) ? part.AssessmentBasis : baseTd?.AssessmentBasis,
            BackTaxesYearFrom = part.BackTaxesYearFrom ?? baseTd?.BackTaxesYearFrom,
            BackTaxesYearTo = part.BackTaxesYearTo ?? baseTd?.BackTaxesYearTo,
            PropertyTypeAccordUse = !string.IsNullOrWhiteSpace(part.PropertyTypeAccordUse) ? part.PropertyTypeAccordUse : baseTd?.BuildingUseType,
            OtherPropertyTypeAccordUse = !string.IsNullOrWhiteSpace(part.OtherPropertyTypeAccordUse) ? part.OtherPropertyTypeAccordUse : baseTd?.BuildingDetails?.OtherBuildingUseType,
            TransactionCode = baseTd?.TransactionCode,
            YearEffectivity = part.EffectivityYear ?? baseTd?.EffectivityYear ?? DateTime.Now.Year,
            QuarterEffectivity = effQtrInt,
            Memoranda = !string.IsNullOrWhiteSpace(part.Memoranda) ? part.Memoranda : baseTd?.Memoranda,
            Taxability = string.Equals(part.Taxability ?? baseTd?.Taxability, "Taxable", StringComparison.OrdinalIgnoreCase),
            TotalMarketValue = part.MarketValue > 0 ? part.MarketValue : 0m,
            TotalAssessedValue = part.AssessedValue > 0 ? part.AssessedValue : 0m,
            TotalArea = part.AreaSqMeters > 0 ? part.AreaSqMeters : (part.TotalBuildingAreaSqm > 0 ? part.TotalBuildingAreaSqm : 0m),
            Unit = "sq.m.",
            FloorPlanSketchPath = ResolveFullUrl(rawSketch),
            Attachments = attachments ?? new()
        };
    }

    private TDNLandDTO MapPartitionToLandDTO(
        AssessmentPartitionItem part,
        int partIndex,
        string trackingNo,
        long formWorkflowId,
        TaxDeclaration? baseTd = null,
        string? sketchPath = null,
        List<MobileAttachmentDTO>? attachments = null)
    {
        var tdnDto = BuildCoreTDNDTOFromPartition(part, partIndex, trackingNo, formWorkflowId, baseTd, sketchPath, attachments);
        var l = baseTd?.LandDetails;

        bool isSegregated = partIndex > 0 || (!string.IsNullOrWhiteSpace(part.PartitionIndex) && !string.Equals(part.PartitionIndex, "A", StringComparison.OrdinalIgnoreCase) && !string.Equals(part.PartitionIndex, "1", StringComparison.OrdinalIgnoreCase));
        string cleanNorth = isSegregated && (string.Equals(part.BoundaryNorth, baseTd?.BoundaryNorth, StringComparison.OrdinalIgnoreCase) || string.Equals(part.BoundaryNorth, l?.BoundaryNorth, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (part.BoundaryNorth ?? string.Empty);
        string cleanSouth = isSegregated && (string.Equals(part.BoundarySouth, baseTd?.BoundarySouth, StringComparison.OrdinalIgnoreCase) || string.Equals(part.BoundarySouth, l?.BoundarySouth, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (part.BoundarySouth ?? string.Empty);
        string cleanEast = isSegregated && (string.Equals(part.BoundaryEast, baseTd?.BoundaryEast, StringComparison.OrdinalIgnoreCase) || string.Equals(part.BoundaryEast, l?.BoundaryEast, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (part.BoundaryEast ?? string.Empty);
        string cleanWest = isSegregated && (string.Equals(part.BoundaryWest, baseTd?.BoundaryWest, StringComparison.OrdinalIgnoreCase) || string.Equals(part.BoundaryWest, l?.BoundaryWest, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (part.BoundaryWest ?? string.Empty);
        string cleanNrem = isSegregated && (string.Equals(part.Nremarks, baseTd?.Nremarks, StringComparison.OrdinalIgnoreCase) || string.Equals(part.Nremarks, l?.Nremarks, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (part.Nremarks ?? string.Empty);
        string cleanSrem = isSegregated && (string.Equals(part.Sremarks, baseTd?.Sremarks, StringComparison.OrdinalIgnoreCase) || string.Equals(part.Sremarks, l?.Sremarks, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (part.Sremarks ?? string.Empty);
        string cleanErem = isSegregated && (string.Equals(part.Eremarks, baseTd?.Eremarks, StringComparison.OrdinalIgnoreCase) || string.Equals(part.Eremarks, l?.Eremarks, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (part.Eremarks ?? string.Empty);
        string cleanWrem = isSegregated && (string.Equals(part.Wremarks, baseTd?.Wremarks, StringComparison.OrdinalIgnoreCase) || string.Equals(part.Wremarks, l?.Wremarks, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (part.Wremarks ?? string.Empty);

        var landDto = new TDNLandDataDTO
        {
            TDNLandId = l != null ? GuidToNumericId(l.Id) : formWorkflowId,
            TDNId = tdnDto.TDNId,
            NorthBoundary = cleanNorth,
            SouthBoundary = cleanSouth,
            EastBoundary = cleanEast,
            WestBoundary = cleanWest,
            Nremarks = cleanNrem,
            Sremarks = cleanSrem,
            Eremarks = cleanErem,
            Wremarks = cleanWrem,
            LandAppraisalDetailsJson = part.LandAppraisalDetailsJson,
            LandImprovementsJson = part.LandImprovementsJson,
            LandAdjustmentsJson = part.LandAdjustmentsJson,
            LandPropertyAssessmentsJson = part.LandPropertyAssessmentsJson,
            Classification = !string.IsNullOrWhiteSpace(part.Classification) ? part.Classification : baseTd?.Classification.ToString(),
            ActualUse = !string.IsNullOrWhiteSpace(part.ActualUse) ? part.ActualUse : baseTd?.ActualUse,
            UnitValue = part.UnitValue > 0 ? part.UnitValue : baseTd?.UnitValue,
            AssessmentLevelPercent = part.AssessmentLevelPercent > 0 ? part.AssessmentLevelPercent : 20m
        };

        return new TDNLandDTO
        {
            TDN = tdnDto,
            TDNLand = landDto
        };
    }

    private TDNBuildingDTO MapPartitionToBuildingDTO(
        AssessmentPartitionItem part,
        int partIndex,
        string trackingNo,
        long formWorkflowId,
        TaxDeclaration? baseTd = null,
        string? sketchPath = null,
        List<MobileAttachmentDTO>? attachments = null)
    {
        var tdnDto = BuildCoreTDNDTOFromPartition(part, partIndex, trackingNo, formWorkflowId, baseTd, sketchPath, attachments);
        var b = baseTd?.BuildingDetails;

        decimal totalArea = part.TotalBuildingAreaSqm > 0 ? part.TotalBuildingAreaSqm : (part.AreaSqMeters > 0 ? part.AreaSqMeters : (b?.TotalFloorArea ?? baseTd?.AreaSqMeters ?? 0));
        int storeys = part.NoOfStoreys > 0 ? part.NoOfStoreys : (b?.NoOfStoreys ?? 1);
        decimal areaPerFloor = storeys > 0 ? Math.Round(totalArea / storeys, 2) : totalArea;

        var bldgDto = new CAST_TDNBuildingDTO
        {
            CAST_TDNBuildingId = b != null ? GuidToNumericId(b.Id) : formWorkflowId,
            TDNId = tdnDto.TDNId,
            STREET = !string.IsNullOrWhiteSpace(part.StreetLocation) ? part.StreetLocation : (b?.StreetNo ?? baseTd?.StreetNo),
            LotNo = !string.IsNullOrWhiteSpace(part.LotNumber) ? part.LotNumber : (b?.LandLotNo ?? baseTd?.LotNo),
            SurveyNo = !string.IsNullOrWhiteSpace(part.SurveyNumber) ? part.SurveyNumber : (b?.LandSurveyNo ?? baseTd?.SurveyNo ?? baseTd?.CadastralSurveyNo),
            TCTNo = !string.IsNullOrWhiteSpace(part.TitleNo) ? part.TitleNo : (b?.LandTitleNo ?? baseTd?.TitleNo),
            LandArea = (part.AreaSqMeters > 0 ? part.AreaSqMeters : (b?.LandAreaSqm > 0 ? b.LandAreaSqm : baseTd?.AreaSqMeters ?? 0)).ToString("F2"),
            ARPNo = tdnDto.ARPNo,
            PermitNo = !string.IsNullOrWhiteSpace(part.BuildingPermitNo) ? part.BuildingPermitNo : b?.BuildingPermitNo,
            OccupancyPermitNo = part.OccupancyPermitNo,
            Age = part.BuildingAgeYears > 0 ? part.BuildingAgeYears : b?.BuildingAgeYears,
            StoreyNo = storeys,
            AreaPerFloor = areaPerFloor,
            TotalbldgArea = totalArea,
            SubtotalValCompArea = part.BaseRcnValue > 0 ? part.BaseRcnValue : b?.ReplacementCostNew,
            TotalAddItem = part.BuildingTotalAdditionsValue > 0 ? part.BuildingTotalAdditionsValue : b?.TotalAdditionalItemsValue,
            DepreciationRate = part.BuildingDepreciationPercent > 0 ? part.BuildingDepreciationPercent : b?.BuildingDepreciationPercent,
            DepreciationAccumulated = part.BuildingDepreciationAccumulated > 0 ? part.BuildingDepreciationAccumulated : b?.BuildingDepreciationAccumulated,
            DepreciationMarketval = part.BuildingDepreciatedMarketValue > 0 ? part.BuildingDepreciatedMarketValue : b?.DepreciatedMarketValue,
            TotalMarketVal = part.MarketValue > 0 ? part.MarketValue : (b?.MarketValue > 0 ? b.MarketValue : baseTd?.MarketValue),
            TotalAssessedVal = part.AssessedValue > 0 ? part.AssessedValue : (b?.AssessedValue > 0 ? b.AssessedValue : baseTd?.AssessedValue),
            StucturalType = !string.IsNullOrWhiteSpace(part.StructuralType) ? part.StructuralType : b?.StructuralType,
            OtherStructure = !string.IsNullOrWhiteSpace(part.StructuralType) ? part.StructuralType : b?.StructuralType,
            BuildingClassification = part.BuildingClassification,
            PropertyTypeAccordUse = !string.IsNullOrWhiteSpace(part.PropertyTypeAccordUse) ? part.PropertyTypeAccordUse : (b?.BuildingUseType ?? baseTd?.BuildingUseType),
            OtherPropertyTypeAccordUse = !string.IsNullOrWhiteSpace(part.OtherPropertyTypeAccordUse) ? part.OtherPropertyTypeAccordUse : b?.OtherBuildingUseType,
            MailingAddress = !string.IsNullOrWhiteSpace(part.OwnerAddress) ? part.OwnerAddress : (b?.LandOwnerAddress ?? baseTd?.OwnerAddress),
            TIN = !string.IsNullOrWhiteSpace(part.OwnerTIN) ? part.OwnerTIN : (b?.LandOwnerTin ?? baseTd?.OwnerTin),
            MobileNo = !string.IsNullOrWhiteSpace(part.OwnerContactNo) ? part.OwnerContactNo : (b?.LandOwnerPhone ?? baseTd?.OwnerPhone),
            EmailAddress = !string.IsNullOrWhiteSpace(part.OwnerEmail) ? part.OwnerEmail : (b?.LandOwnerEmail ?? baseTd?.OwnerEmail),
            Gender = part.OwnerGender,
            CivilStatus = part.OwnerCivilStatus,
            TransactionCode = baseTd?.TransactionCode,
            FloorBreakdownJson = !string.IsNullOrWhiteSpace(part.BuildingFloorBreakdownJson) ? part.BuildingFloorBreakdownJson : b?.FloorBreakdownJson,
            AdditionalItemsJson = !string.IsNullOrWhiteSpace(part.BuildingAdditionalItemsJson) ? part.BuildingAdditionalItemsJson : b?.AdditionalItemsJson,

            // 17 Structural Materials & Finishes
            StructuralFoundation = part.StructuralFoundation,
            StructuralColumns = part.StructuralColumns,
            StructuralBeams = part.StructuralBeams,
            StructuralTrussRoof = part.StructuralTrussRoof,
            StructuralRoof = part.StructuralRoof,
            StructuralExteriorWalls = part.StructuralExteriorWalls,
            StructuralFlooring = part.StructuralFlooring,
            DoorMain = part.DoorMain,
            Ceiling = part.Ceiling,
            StructuralWindows = part.StructuralWindows,
            StructuralStairs = part.StructuralStairs,
            StructuralPartitionWalls = part.StructuralPartitionWalls,
            StructuralFloorFinish = part.StructuralFloorFinish,
            StructuralFinishFront = part.StructuralFinishFront,
            ElectricalSystem = part.ElectricalSystem,
            ToiletAndBathroom = part.ToiletAndBathroom,
            PlumbingSewer = part.PlumbingSewer,

            // CCT and Land Reference
            IsCct = part.IsCct,
            CctTitleNo = part.CctTitleNo,
            CctDateRegistered = part.CctDateRegistered?.ToString("yyyy-MM-dd"),
            OwnerNameReflectedInCct = part.OwnerNameReflectedInCct,
            LandOwnerName = !string.IsNullOrWhiteSpace(part.LandOwnerName) ? part.LandOwnerName : b?.LandOwnerName,
            LandTdNo = !string.IsNullOrWhiteSpace(part.LandTdNo) ? part.LandTdNo : b?.LandTdNo,
            LandPin = !string.IsNullOrWhiteSpace(part.LandPin) ? part.LandPin : b?.LandPin,

            DateConstructed = part.DateConstructed?.ToString("yyyy-MM-dd"),
            DateCompleted = part.DateCompleted?.ToString("yyyy-MM-dd"),
            DateOccupied = part.DateOccupied?.ToString("yyyy-MM-dd")
        };

        return new TDNBuildingDTO
        {
            TDN = tdnDto,
            CAST_TDNBuildingDTO = bldgDto
        };
    }

    private TDNMachineDTO MapPartitionToMachineryDTO(
        AssessmentPartitionItem part,
        int partIndex,
        string trackingNo,
        long formWorkflowId,
        TaxDeclaration? baseTd = null,
        string? sketchPath = null,
        List<MobileAttachmentDTO>? attachments = null)
    {
        var tdnDto = BuildCoreTDNDTOFromPartition(part, partIndex, trackingNo, formWorkflowId, baseTd, sketchPath, attachments);
        var m = baseTd?.MachineryDetails;

        var details = m?.TDNMachineryDetails?.Select(d => new TDNMachineDetailDTO
        {
            TDNMachineDetailId = GuidToNumericId(d.Id),
            TDNMachineId = m != null ? GuidToNumericId(m.Id) : formWorkflowId,
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
                TDNMachineDetailId = m != null ? GuidToNumericId(m.Id) : formWorkflowId,
                TDNMachineId = m != null ? GuidToNumericId(m.Id) : formWorkflowId,
                KindOfMachinery = m?.DescriptionBrand ?? "Machinery / Equipment",
                BrandModel = m?.DescriptionBrand,
                Capacity = m?.RatedCapacity,
                OriginalCost = part.AcquisitionCost > 0 ? part.AcquisitionCost : (m?.OriginalCost ?? 0),
                EconomicLifeEstimated = m?.EconomicLifeYears,
                ConditionWhenAcquired = m?.ConditionWhenAcquired,
                AcquiredDate = m?.DateAcquired,
                MarketValue = part.MarketValue > 0 ? part.MarketValue : (baseTd?.MarketValue ?? 0),
                AssessedValue = part.AssessedValue > 0 ? part.AssessedValue : (baseTd?.AssessedValue ?? 0)
            });
        }

        return new TDNMachineDTO
        {
            TDN = tdnDto,
            TDNMachine = new TDNMachineDataDTO
            {
                TDNMachineId = m != null ? GuidToNumericId(m.Id) : formWorkflowId,
                TDNId = tdnDto.TDNId,
                LandOwnerPIN = !string.IsNullOrWhiteSpace(part.LandPin) ? part.LandPin : m?.LandPin,
                BuildingOwnerPIN = !string.IsNullOrWhiteSpace(part.BuildingPin) ? part.BuildingPin : m?.BuildingPin,
                LandOwner = !string.IsNullOrWhiteSpace(part.LandOwnerName) ? part.LandOwnerName : m?.LandOwnerName,
                BuildingOwner = !string.IsNullOrWhiteSpace(part.BuildingReferenceName) ? part.BuildingReferenceName : m?.BuildingReferenceName,
                LandTdNo = part.LandTdNo,
                BuildingTdNo = part.BuildingTdNo,
                IsLandOwnerAlsoBuildingOwner = part.IsLandOwnerAlsoBuildingOwner,
                IsLocallyManufactured = part.IsLocallyManufactured,
                MachineryRegNo = part.MachineryRegNo,
                MachineryDateRegistered = part.MachineryDateRegistered,
                ImportationPermitNo = part.ImportationPermitNo,
                ImportationDateIssued = part.ImportationDateIssued,
                CountryOfOrigin = part.CountryOfOrigin,
                SupplierSeller = part.SupplierSeller,
                SupplierAddress = part.SupplierAddress,
                AcquisitionCost = part.AcquisitionCost > 0 ? part.AcquisitionCost : (m?.OriginalCost ?? 0),
                OrNo = part.OrNo,
                OrDate = part.OrDate,
                OrNoAndDate = part.OrNoAndDate,
                LocalDateIssued = part.LocalDateIssued,
                MachineryAppraisalDetailsJson = part.MachineryAppraisalDetailsJson,
                MachineryPropertyAssessmentsJson = part.MachineryPropertyAssessmentsJson
            },
            TDNMachineDetail = details
        };
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
        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        string ResolveFullUrl(string? rel)
        {
            if (string.IsNullOrWhiteSpace(rel)) return string.Empty;
            if (rel.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || rel.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return rel;
            string clean = rel.Replace('\\', '/');
            if (!clean.StartsWith("/")) clean = "/" + clean;
            return $"{baseUrl}{clean}";
        }

        // Build packages for ALL partitions
        var partitionDetailsList = new List<object>();
        for (int i = 0; i < parts.Count; i++)
        {
            var p = parts[i];
            string parentKindStr = matchedRpa != null ? matchedRpa.Kind : (matchedRfi != null ? matchedRfi.Kind : "Land");
            if (string.Equals(parentKindStr, "Land", StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(p.Kind) || string.Equals(p.Kind, "Building", StringComparison.OrdinalIgnoreCase)))
            {
                p.Kind = "Land";
            }
            var pAttachments = ResolveAllAttachmentsForPartition(p, i, matchedRpa, matchedRfi, td);
            int pKindId = ParsePropertyKindId(p.Kind, ParsePropertyKindId(parentKindStr, 1));
            string? rawSketch = p.BuildingFloorPlanSketchPath ?? td.FloorPlanSketchPath;
            string? pSketch = !string.IsNullOrWhiteSpace(rawSketch) ? ResolveFullUrl(rawSketch) : null;
            object pForm = pKindId switch
            {
                1 => MapPartitionToLandDTO(p, i, resolvedTrackingNo, resolvedWfId, td, pSketch, pAttachments),
                3 => MapPartitionToMachineryDTO(p, i, resolvedTrackingNo, resolvedWfId, td, pSketch, pAttachments),
                _ => MapPartitionToBuildingDTO(p, i, resolvedTrackingNo, resolvedWfId, td, pSketch, pAttachments)
            };
            string pFormType = pKindId switch { 1 => "LAND", 3 => "MACHINERY", _ => "BUILDING" };

            bool isSegPart = i > 0 || (!string.IsNullOrWhiteSpace(p.PartitionIndex) && !string.Equals(p.PartitionIndex, "A", StringComparison.OrdinalIgnoreCase) && !string.Equals(p.PartitionIndex, "1", StringComparison.OrdinalIgnoreCase));
            string cleanNorth = isSegPart && (string.Equals(p.BoundaryNorth, td.BoundaryNorth, StringComparison.OrdinalIgnoreCase) || string.Equals(p.BoundaryNorth, td.LandDetails?.BoundaryNorth, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (p.BoundaryNorth ?? string.Empty);
            string cleanSouth = isSegPart && (string.Equals(p.BoundarySouth, td.BoundarySouth, StringComparison.OrdinalIgnoreCase) || string.Equals(p.BoundarySouth, td.LandDetails?.BoundarySouth, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (p.BoundarySouth ?? string.Empty);
            string cleanEast = isSegPart && (string.Equals(p.BoundaryEast, td.BoundaryEast, StringComparison.OrdinalIgnoreCase) || string.Equals(p.BoundaryEast, td.LandDetails?.BoundaryEast, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (p.BoundaryEast ?? string.Empty);
            string cleanWest = isSegPart && (string.Equals(p.BoundaryWest, td.BoundaryWest, StringComparison.OrdinalIgnoreCase) || string.Equals(p.BoundaryWest, td.LandDetails?.BoundaryWest, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (p.BoundaryWest ?? string.Empty);
            string cleanNrem = isSegPart && (string.Equals(p.Nremarks, td.Nremarks, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Nremarks, td.LandDetails?.Nremarks, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (p.Nremarks ?? string.Empty);
            string cleanSrem = isSegPart && (string.Equals(p.Sremarks, td.Sremarks, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Sremarks, td.LandDetails?.Sremarks, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (p.Sremarks ?? string.Empty);
            string cleanErem = isSegPart && (string.Equals(p.Eremarks, td.Eremarks, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Eremarks, td.LandDetails?.Eremarks, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (p.Eremarks ?? string.Empty);
            string cleanWrem = isSegPart && (string.Equals(p.Wremarks, td.Wremarks, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Wremarks, td.LandDetails?.Wremarks, StringComparison.OrdinalIgnoreCase)) ? string.Empty : (p.Wremarks ?? string.Empty);

            string? cleanSurveyNo = isSegPart && (string.Equals(p.SurveyNumber, td.SurveyNo, StringComparison.OrdinalIgnoreCase) || string.Equals(p.SurveyNumber, td.LandDetails?.SurveyNo, StringComparison.OrdinalIgnoreCase)) ? null : (!string.IsNullOrWhiteSpace(p.SurveyNumber) ? p.SurveyNumber : (i == 0 ? td.SurveyNo : null));
            string? cleanParcelNo = isSegPart && (string.Equals(p.ParcelNo, td.ParcelNo, StringComparison.OrdinalIgnoreCase) || string.Equals(p.ParcelNo, td.LandDetails?.ParcelNo, StringComparison.OrdinalIgnoreCase)) ? null : p.ParcelNo;
            string? cleanCadastral = isSegPart && (string.Equals(p.CadastralSurveyNo, td.CadastralSurveyNo, StringComparison.OrdinalIgnoreCase) || string.Equals(p.CadastralSurveyNo, td.LandDetails?.CadastralSurveyNo, StringComparison.OrdinalIgnoreCase)) ? null : (!string.IsNullOrWhiteSpace(p.CadastralSurveyNo) ? p.CadastralSurveyNo : (i == 0 ? td.CadastralSurveyNo : null));

            partitionDetailsList.Add(new
            {
                index = i,
                partitionIndex = !string.IsNullOrWhiteSpace(p.PartitionIndex) ? p.PartitionIndex : ((char)('A' + i)).ToString(),
                kind = !string.IsNullOrWhiteSpace(p.Kind) ? p.Kind : "Land",
                transactionTypeId = pKindId,
                formType = pFormType,
                tdnId = resolvedTdnId,
                tdnLandId = td.LandDetails != null ? GuidToNumericId(td.LandDetails.Id) : (long?)null,
                castTdnBuildingId = td.BuildingDetails != null ? GuidToNumericId(td.BuildingDetails.Id) : (long?)null,
                tdnMachineryId = td.MachineryDetails != null ? GuidToNumericId(td.MachineryDetails.Id) : (long?)null,
                systemGeneratedTDN = p.SystemGeneratedTDN,
                arpNo = !string.IsNullOrWhiteSpace(p.ARPNo) ? p.ARPNo : (p.SystemGeneratedTDN ?? td.TaxDeclarationNumber),
                pin = p.PIN,
                propertyTypeAccordUse = p.PropertyTypeAccordUse,
                otherPropertyTypeAccordUse = p.OtherPropertyTypeAccordUse,
                surveyNo = cleanSurveyNo,
                lotNo = !string.IsNullOrWhiteSpace(p.LotNumber) ? p.LotNumber : $"Lot {i + 1}-{(!string.IsNullOrWhiteSpace(p.PartitionIndex) ? p.PartitionIndex : ((char)('A' + i)).ToString())}",
                blockNo = !string.IsNullOrWhiteSpace(p.BlockNumber) ? p.BlockNumber : (i == 0 ? td.BlockNo : null),
                blockNumber = !string.IsNullOrWhiteSpace(p.BlockNumber) ? p.BlockNumber : (i == 0 ? td.BlockNo : null),
                titleNo = p.TitleNo,
                sectionNo = p.SectionNo,
                parcelNo = cleanParcelNo,
                cadastralSurveyNo = cleanCadastral,
                dateRegistered = p.DateRegistered?.ToString("yyyy-MM-dd"),
                registrationStatus = !string.IsNullOrWhiteSpace(p.RegistrationStatus) ? p.RegistrationStatus : td.RegistrationStatus,
                titleType = !string.IsNullOrWhiteSpace(p.TitleType) ? p.TitleType : td.TitleType,
                purok = p.Purok,
                streetLocation = !string.IsNullOrWhiteSpace(p.StreetLocation) ? p.StreetLocation : td.StreetNo,
                streetNo = !string.IsNullOrWhiteSpace(p.StreetLocation) ? p.StreetLocation : td.StreetNo,
                buildingNo = !string.IsNullOrWhiteSpace(p.StreetLocation) ? p.StreetLocation : td.StreetNo,
                propertyLocation = p.DisplayPropertyLocation,
                districtOrBarangay = !string.IsNullOrWhiteSpace(p.DistrictOrBarangay) ? p.DistrictOrBarangay : td.DistrictOrBarangay,
                cityMunicipality = !string.IsNullOrWhiteSpace(p.CityMunicipality) ? p.CityMunicipality : td.Municipality,
                province = !string.IsNullOrWhiteSpace(p.Province) ? p.Province : "South Cotabato",
                ownerName = !string.IsNullOrWhiteSpace(p.NewOwnerName) ? p.NewOwnerName : td.OwnerName,
                ownerTin = p.OwnerTIN,
                ownerContactNo = p.OwnerContactNo,
                ownerAddress = p.OwnerAddress,
                ownerEmail = p.OwnerEmail,
                ownerGender = p.OwnerGender,
                isAdministrator = p.IsAdministrator,
                isBeneficialUser = p.IsBeneficialUser,
                isPossessor = p.IsPossessor,
                administratorName = p.AdministratorName,
                administratorAddress = p.AdministratorAddress,
                administratorContactNo = p.AdministratorContactNo,
                administratorTin = p.AdministratorTin,
                administratorEmail = p.AdministratorEmail,
                administratorGender = p.AdministratorGender,
                boundaryNorth = cleanNorth,
                nremarks = cleanNrem,
                boundarySouth = cleanSouth,
                sremarks = cleanSrem,
                boundaryEast = cleanEast,
                eremarks = cleanErem,
                boundaryWest = cleanWest,
                wremarks = cleanWrem,
                areaSqMeters = p.AreaSqMeters > 0 ? p.AreaSqMeters : p.TotalBuildingAreaSqm,
                marketValue = p.MarketValue,
                assessedValue = p.AssessedValue,
                classification = p.Classification,
                actualUse = p.ActualUse,
                unitValue = p.UnitValue,
                assessmentLevelPercent = p.AssessmentLevelPercent,
                landAppraisalDetailsJson = p.LandAppraisalDetailsJson,
                landImprovementsJson = p.LandImprovementsJson,
                landAdjustmentsJson = p.LandAdjustmentsJson,
                landPropertyAssessmentsJson = p.LandPropertyAssessmentsJson,
                isLandOwnerAlsoBuildingOwner = p.IsLandOwnerAlsoBuildingOwner,
                buildingReferenceName = p.BuildingReferenceName,
                buildingPin = p.BuildingPin,
                buildingTdNo = p.BuildingTdNo,
                landOwnerName = p.LandOwnerName,
                landPin = p.LandPin,
                landTdNo = p.LandTdNo,
                isLocallyManufactured = p.IsLocallyManufactured,
                machineryRegNo = p.MachineryRegNo,
                machineryDateRegistered = p.MachineryDateRegistered?.ToString("yyyy-MM-dd"),
                importationPermitNo = p.ImportationPermitNo,
                importationDateIssued = p.ImportationDateIssued?.ToString("yyyy-MM-dd"),
                countryOfOrigin = p.CountryOfOrigin,
                supplierSeller = p.SupplierSeller,
                supplierAddress = p.SupplierAddress,
                acquisitionCost = p.AcquisitionCost,
                orNo = p.OrNo,
                orDate = p.OrDate?.ToString("yyyy-MM-dd"),
                orNoAndDate = p.OrNoAndDate,
                localDateIssued = p.LocalDateIssued?.ToString("yyyy-MM-dd"),
                machineryAppraisalDetailsJson = p.MachineryAppraisalDetailsJson,
                machineryPropertyAssessmentsJson = p.MachineryPropertyAssessmentsJson,
                taxability = !string.IsNullOrWhiteSpace(p.Taxability) ? p.Taxability : td.Taxability,
                effectivityYear = p.EffectivityYear ?? td.EffectivityYear,
                effectivityQuarter = !string.IsNullOrWhiteSpace(p.EffectivityQuarter) ? p.EffectivityQuarter : td.EffectivityQuarter,
                assessmentBasis = !string.IsNullOrWhiteSpace(p.AssessmentBasis) ? p.AssessmentBasis : td.AssessmentBasis,
                backTaxesYearFrom = p.BackTaxesYearFrom ?? td.BackTaxesYearFrom,
                backTaxesYearTo = p.BackTaxesYearTo ?? td.BackTaxesYearTo,
                memoranda = !string.IsNullOrWhiteSpace(p.Memoranda) ? p.Memoranda : td.Memoranda,
                sketchUrl = pSketch,
                sketchFileName = !string.IsNullOrWhiteSpace(pSketch) ? Path.GetFileName(rawSketch) : null,
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

        var fallbackAttachments = ResolveAllAttachmentsForPartition(null, 0, matchedRpa, matchedRfi, td);
        object fallbackFormData = formType switch
        {
            "LAND" => MapTaxDeclarationToLandDTO(td, resolvedTrackingNo, resolvedWfId, td.FloorPlanSketchPath, fallbackAttachments, 0, "A"),
            "MACHINERY" => MapTaxDeclarationToMachineryDTO(td, resolvedTrackingNo, resolvedWfId, td.FloorPlanSketchPath, fallbackAttachments, 0, "A"),
            _ => MapTaxDeclarationToBuildingDTO(td, resolvedTrackingNo, resolvedWfId, td.FloorPlanSketchPath, fallbackAttachments, 0, "A")
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
            sketchUrl = selectedPart != null ? selectedPart.sketchUrl : ResolveFullUrl(td.FloorPlanSketchPath),
            sketchFileName = selectedPart != null ? selectedPart.sketchFileName : (string.IsNullOrWhiteSpace(td.FloorPlanSketchPath) ? null : Path.GetFileName(td.FloorPlanSketchPath)),
            attachments = selectedPart != null ? selectedPart.attachments : fallbackAttachments,
            formData = selectedPart != null ? selectedPart.formData : fallbackFormData,
            partitions = partitionDetailsList,
            downloadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
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

        if (matchedRpa == null && matchedRfi == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Matching Property Assessment or Field Inspection transaction could not be found."
            });
        }

        var partitions = matchedRpa != null
            ? PartitionJsonHelper.ParseList(matchedRpa.GeneratedPartitionsJson)
            : PartitionJsonHelper.ParseList(matchedRfi!.GeneratedPartitionsJson);

        if (partitions.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Cannot edit transaction: This transaction has no generated partitions. Partitions must be generated in the Office Web Portal first before field edits can be submitted."
            });
        }

        try
        {
            int targetIdx = ResolvePartitionTargetIndex(partitions, dto.TDN, "Building");
            var part = partitions[targetIdx];

            string trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber ?? dto.TDN.TrackingNo ?? "general";
            var processedAttachments = ProcessAndSaveAttachments(dto.TDN.Attachments, trackingNo);
            ProcessAndSaveSketch(dto.TDN, trackingNo);

            ApplyBuildingDTOToPartition(part, dto.TDN, dto.CAST_TDNBuildingDTO, processedAttachments);

            if (matchedRpa != null)
            {
                matchedRpa.GeneratedPartitionsJson = JsonSerializer.Serialize(partitions, _jsonOptions);
                matchedRpa.UpdatedDate = DateTime.Now;
                if (targetIdx == 0)
                {
                    if (!string.IsNullOrWhiteSpace(part.NewOwnerName)) matchedRpa.NewOwnerName = part.NewOwnerName;
                    if (!string.IsNullOrWhiteSpace(part.OwnerAddress)) matchedRpa.Address = part.OwnerAddress;
                    if (!string.IsNullOrWhiteSpace(part.LotNumber)) matchedRpa.LotNo = part.LotNumber;
                    if (!string.IsNullOrWhiteSpace(part.StreetLocation)) matchedRpa.PropertyLocation = part.StreetLocation;
                    if (!string.IsNullOrWhiteSpace(part.Memoranda)) matchedRpa.Remarks = part.Memoranda;
                }
                _rpaService.SyncPartitionTaxDeclarationStatuses(matchedRpa);
            }
            else if (matchedRfi != null)
            {
                matchedRfi.GeneratedPartitionsJson = JsonSerializer.Serialize(partitions, _jsonOptions);
                matchedRfi.UpdatedDate = DateTime.Now;
                if (targetIdx == 0)
                {
                    if (!string.IsNullOrWhiteSpace(part.NewOwnerName)) matchedRfi.OwnerName = part.NewOwnerName;
                    if (!string.IsNullOrWhiteSpace(part.OwnerAddress)) matchedRfi.PropertyLocation = part.OwnerAddress;
                    if (!string.IsNullOrWhiteSpace(part.LotNumber)) matchedRfi.LotNo = part.LotNumber;
                    if (!string.IsNullOrWhiteSpace(part.StreetLocation)) matchedRfi.PropertyLocation = part.StreetLocation;
                    if (!string.IsNullOrWhiteSpace(part.Memoranda)) matchedRfi.Remarks = part.Memoranda;
                }
                _rfiService.SyncPartitionTaxDeclarationStatuses(matchedRfi);
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "BUILDING_UPDATED_VIA_MOBILE",
                arpNo = part.SystemGeneratedTDN,
                partitionIndex = part.PartitionIndex,
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Building assessment partition updated and synced successfully.",
                partitionIndex = part.PartitionIndex,
                systemGeneratedTDN = part.SystemGeneratedTDN,
                tdnId = matchedRpa != null ? GuidToNumericId(matchedRpa.Id) : GuidToNumericId(matchedRfi!.Id),
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Failed to update Building: {ex.Message} | {ex.GetBaseException().Message}" });
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

        if (matchedRpa == null && matchedRfi == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Matching Property Assessment or Field Inspection transaction could not be found."
            });
        }

        var partitions = matchedRpa != null
            ? PartitionJsonHelper.ParseList(matchedRpa.GeneratedPartitionsJson)
            : PartitionJsonHelper.ParseList(matchedRfi!.GeneratedPartitionsJson);

        if (partitions.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Cannot edit transaction: This transaction has no generated partitions. Partitions must be generated in the Office Web Portal first before field edits can be submitted."
            });
        }

        try
        {
            if (matchedRpa != null && string.Equals(matchedRpa.Kind, "Land", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var p in partitions)
                {
                    if (string.IsNullOrWhiteSpace(p.Kind) || string.Equals(p.Kind, "Building", StringComparison.OrdinalIgnoreCase))
                    {
                        p.Kind = "Land";
                    }
                }
            }
            else if (matchedRfi != null && string.Equals(matchedRfi.Kind, "Land", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var p in partitions)
                {
                    if (string.IsNullOrWhiteSpace(p.Kind) || string.Equals(p.Kind, "Building", StringComparison.OrdinalIgnoreCase))
                    {
                        p.Kind = "Land";
                    }
                }
            }

            int targetIdx = ResolvePartitionTargetIndex(partitions, dto.TDN, "Land");
            var part = partitions[targetIdx];
            part.Kind = "Land";

            string trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber ?? dto.TDN.TrackingNo ?? "general";
            var processedAttachments = ProcessAndSaveAttachments(dto.TDN.Attachments, trackingNo);
            ProcessAndSaveSketch(dto.TDN, trackingNo);

            ApplyLandDTOToPartition(part, dto.TDN, dto.TDNLand, processedAttachments);

            if (matchedRpa != null)
            {
                matchedRpa.GeneratedPartitionsJson = JsonSerializer.Serialize(partitions, _jsonOptions);
                matchedRpa.UpdatedDate = DateTime.Now;
                if (targetIdx == 0)
                {
                    if (!string.IsNullOrWhiteSpace(part.NewOwnerName)) matchedRpa.NewOwnerName = part.NewOwnerName;
                    if (!string.IsNullOrWhiteSpace(part.OwnerAddress)) matchedRpa.Address = part.OwnerAddress;
                    if (!string.IsNullOrWhiteSpace(part.LotNumber)) matchedRpa.LotNo = part.LotNumber;
                    if (!string.IsNullOrWhiteSpace(part.StreetLocation)) matchedRpa.PropertyLocation = part.StreetLocation;
                    if (!string.IsNullOrWhiteSpace(part.Memoranda)) matchedRpa.Remarks = part.Memoranda;
                }
                _rpaService.SyncPartitionTaxDeclarationStatuses(matchedRpa);
            }
            else if (matchedRfi != null)
            {
                matchedRfi.GeneratedPartitionsJson = JsonSerializer.Serialize(partitions, _jsonOptions);
                matchedRfi.UpdatedDate = DateTime.Now;
                if (targetIdx == 0)
                {
                    if (!string.IsNullOrWhiteSpace(part.NewOwnerName)) matchedRfi.OwnerName = part.NewOwnerName;
                    if (!string.IsNullOrWhiteSpace(part.OwnerAddress)) matchedRfi.PropertyLocation = part.OwnerAddress;
                    if (!string.IsNullOrWhiteSpace(part.LotNumber)) matchedRfi.LotNo = part.LotNumber;
                    if (!string.IsNullOrWhiteSpace(part.StreetLocation)) matchedRfi.PropertyLocation = part.StreetLocation;
                    if (!string.IsNullOrWhiteSpace(part.Memoranda)) matchedRfi.Remarks = part.Memoranda;
                }
                _rfiService.SyncPartitionTaxDeclarationStatuses(matchedRfi);
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "LAND_UPDATED_VIA_MOBILE",
                arpNo = part.SystemGeneratedTDN,
                partitionIndex = part.PartitionIndex,
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Land appraisal partition updated and synced successfully.",
                partitionIndex = part.PartitionIndex,
                systemGeneratedTDN = part.SystemGeneratedTDN,
                tdnId = matchedRpa != null ? GuidToNumericId(matchedRpa.Id) : GuidToNumericId(matchedRfi!.Id),
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Failed to update Land: {ex.Message} | {ex.GetBaseException().Message}" });
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

        if (matchedRpa == null && matchedRfi == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Matching Property Assessment or Field Inspection transaction could not be found."
            });
        }

        var partitions = matchedRpa != null
            ? PartitionJsonHelper.ParseList(matchedRpa.GeneratedPartitionsJson)
            : PartitionJsonHelper.ParseList(matchedRfi!.GeneratedPartitionsJson);

        if (partitions.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Cannot edit transaction: This transaction has no generated partitions. Partitions must be generated in the Office Web Portal first before field edits can be submitted."
            });
        }

        try
        {
            int targetIdx = ResolvePartitionTargetIndex(partitions, dto.TDN, "Machinery");
            var part = partitions[targetIdx];

            string trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber ?? dto.TDN.TrackingNo ?? "general";
            var processedAttachments = ProcessAndSaveAttachments(dto.TDN.Attachments, trackingNo);
            ProcessAndSaveSketch(dto.TDN, trackingNo);

            ApplyMachineryDTOToPartition(part, dto.TDN, dto, processedAttachments);

            if (matchedRpa != null)
            {
                matchedRpa.GeneratedPartitionsJson = JsonSerializer.Serialize(partitions, _jsonOptions);
                matchedRpa.UpdatedDate = DateTime.Now;
                if (targetIdx == 0)
                {
                    if (!string.IsNullOrWhiteSpace(part.NewOwnerName)) matchedRpa.NewOwnerName = part.NewOwnerName;
                    if (!string.IsNullOrWhiteSpace(part.OwnerAddress)) matchedRpa.Address = part.OwnerAddress;
                    if (!string.IsNullOrWhiteSpace(part.LotNumber)) matchedRpa.LotNo = part.LotNumber;
                    if (!string.IsNullOrWhiteSpace(part.StreetLocation)) matchedRpa.PropertyLocation = part.StreetLocation;
                    if (!string.IsNullOrWhiteSpace(part.Memoranda)) matchedRpa.Remarks = part.Memoranda;
                }
                _rpaService.SyncPartitionTaxDeclarationStatuses(matchedRpa);
            }
            else if (matchedRfi != null)
            {
                matchedRfi.GeneratedPartitionsJson = JsonSerializer.Serialize(partitions, _jsonOptions);
                matchedRfi.UpdatedDate = DateTime.Now;
                if (targetIdx == 0)
                {
                    if (!string.IsNullOrWhiteSpace(part.NewOwnerName)) matchedRfi.OwnerName = part.NewOwnerName;
                    if (!string.IsNullOrWhiteSpace(part.OwnerAddress)) matchedRfi.PropertyLocation = part.OwnerAddress;
                    if (!string.IsNullOrWhiteSpace(part.LotNumber)) matchedRfi.LotNo = part.LotNumber;
                    if (!string.IsNullOrWhiteSpace(part.StreetLocation)) matchedRfi.PropertyLocation = part.StreetLocation;
                    if (!string.IsNullOrWhiteSpace(part.Memoranda)) matchedRfi.Remarks = part.Memoranda;
                }
                _rfiService.SyncPartitionTaxDeclarationStatuses(matchedRfi);
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "MACHINERY_UPDATED_VIA_MOBILE",
                arpNo = part.SystemGeneratedTDN,
                partitionIndex = part.PartitionIndex,
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Machinery assessment partition updated and synced successfully.",
                partitionIndex = part.PartitionIndex,
                systemGeneratedTDN = part.SystemGeneratedTDN,
                tdnId = matchedRpa != null ? GuidToNumericId(matchedRpa.Id) : GuidToNumericId(matchedRfi!.Id),
                trackingNo = matchedRpa?.TrackingNumber ?? matchedRfi?.TrackingNumber
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Failed to update Machinery: {ex.Message} | {ex.GetBaseException().Message}" });
        }
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

        var allRpas = await _context.PropertyAssessmentRequests.AsNoTracking().ToListAsync();
        var matchedRpaCandidate = allRpas.FirstOrDefault(p =>
            (formWorkflowId.HasValue && formWorkflowId.Value > 0 && GuidToNumericId(p.Id) == formWorkflowId.Value) ||
            (!string.IsNullOrEmpty(cleanTracking) && p.TrackingNumber.Equals(cleanTracking, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && p.TrackingNumber.Equals(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && !string.IsNullOrEmpty(p.PrevTDN) && p.PrevTDN.Equals(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanPin) && !string.IsNullOrEmpty(p.PrevPIN) && p.PrevPIN.Equals(cleanPin, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanTracking) && !string.IsNullOrEmpty(p.GeneratedPartitionsJson) && p.GeneratedPartitionsJson.Contains(cleanTracking, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && !string.IsNullOrEmpty(p.GeneratedPartitionsJson) && p.GeneratedPartitionsJson.Contains(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanPin) && !string.IsNullOrEmpty(p.GeneratedPartitionsJson) && p.GeneratedPartitionsJson.Contains(cleanPin, StringComparison.OrdinalIgnoreCase)));

        if (matchedRpaCandidate != null)
        {
            var trackedRpa = await _context.PropertyAssessmentRequests.FirstOrDefaultAsync(p => p.Id == matchedRpaCandidate.Id);
            return (trackedRpa ?? matchedRpaCandidate, null, false);
        }

        var allRfis = await _context.RequestForInspections.AsNoTracking().ToListAsync();
        var matchedRfiCandidate = allRfis.FirstOrDefault(r =>
            (formWorkflowId.HasValue && formWorkflowId.Value > 0 && GuidToNumericId(r.Id) == formWorkflowId.Value) ||
            (!string.IsNullOrEmpty(cleanTracking) && r.TrackingNumber.Equals(cleanTracking, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && r.TrackingNumber.Equals(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && !string.IsNullOrEmpty(r.TaxDeclarationNumber) && r.TaxDeclarationNumber.Equals(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanPin) && !string.IsNullOrEmpty(r.PropertyIndexNumber) && r.PropertyIndexNumber.Equals(cleanPin, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanTracking) && !string.IsNullOrEmpty(r.GeneratedPartitionsJson) && r.GeneratedPartitionsJson.Contains(cleanTracking, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanArp) && !string.IsNullOrEmpty(r.GeneratedPartitionsJson) && r.GeneratedPartitionsJson.Contains(cleanArp, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanPin) && !string.IsNullOrEmpty(r.GeneratedPartitionsJson) && r.GeneratedPartitionsJson.Contains(cleanPin, StringComparison.OrdinalIgnoreCase)));

        if (matchedRfiCandidate != null)
        {
            var trackedRfi = await _context.RequestForInspections.FirstOrDefaultAsync(r => r.Id == matchedRfiCandidate.Id);
            return (null, trackedRfi ?? matchedRfiCandidate, false);
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

    private static TaxDeclaration CloneTaxDeclaration(TaxDeclaration source)
    {
        var copy = new TaxDeclaration
        {
            Id = source.Id,
            Kind = source.Kind,
            TaxDeclarationNumber = source.TaxDeclarationNumber,
            PropertyIndexNumber = source.PropertyIndexNumber,
            ARPNo = source.ARPNo,
            TransactionCode = source.TransactionCode,
            OwnerName = source.OwnerName,
            OwnerAddress = source.OwnerAddress,
            OwnerTin = source.OwnerTin,
            OwnerPhone = source.OwnerPhone,
            DistrictOrBarangay = source.DistrictOrBarangay,
            Municipality = source.Municipality,
            ProvinceCity = source.ProvinceCity,
            StreetNo = source.StreetNo,
            LotNo = source.LotNo,
            BlockNo = source.BlockNo,
            SectionNo = source.SectionNo,
            SurveyNo = source.SurveyNo,
            CadastralSurveyNo = source.CadastralSurveyNo,
            TitleNo = source.TitleNo,
            DateRegistered = source.DateRegistered,
            RegistrationStatus = source.RegistrationStatus,
            TitleType = source.TitleType,
            Classification = source.Classification,
            ActualUse = source.ActualUse,
            UnitValue = source.UnitValue,
            AreaSqMeters = source.AreaSqMeters,
            MarketValue = source.MarketValue,
            AssessedValue = source.AssessedValue,
            AssessmentLevelPercent = source.AssessmentLevelPercent,
            Taxability = source.Taxability,
            EffectivityYear = source.EffectivityYear,
            EffectivityQuarter = source.EffectivityQuarter,
            Memoranda = source.Memoranda,
            FloorPlanSketchPath = source.FloorPlanSketchPath
        };

        if (source.LandDetails != null)
        {
            copy.LandDetails = new TDNLand
            {
                Id = source.LandDetails.Id,
                TaxDeclarationId = source.LandDetails.TaxDeclarationId,
                CadastralSurveyNo = source.LandDetails.CadastralSurveyNo,
                SectionNo = source.LandDetails.SectionNo,
                LotNo = source.LandDetails.LotNo,
                BlockNo = source.LandDetails.BlockNo,
                ParcelNo = source.LandDetails.ParcelNo,
                SurveyNo = source.LandDetails.SurveyNo,
                BoundaryNorth = source.LandDetails.BoundaryNorth,
                BoundarySouth = source.LandDetails.BoundarySouth,
                BoundaryEast = source.LandDetails.BoundaryEast,
                BoundaryWest = source.LandDetails.BoundaryWest,
                Nremarks = source.LandDetails.Nremarks,
                Sremarks = source.LandDetails.Sremarks,
                Eremarks = source.LandDetails.Eremarks,
                Wremarks = source.LandDetails.Wremarks
            };
        }

        if (source.BuildingDetails != null)
        {
            copy.BuildingDetails = new TDNBuilding
            {
                Id = source.BuildingDetails.Id,
                TaxDeclarationId = source.BuildingDetails.TaxDeclarationId,
                NoOfStoreys = source.BuildingDetails.NoOfStoreys,
                TotalFloorArea = source.BuildingDetails.TotalFloorArea,
                BuildingPermitNo = source.BuildingPermitNo,
                BuildingAgeYears = source.BuildingAgeYears,
                StructuralType = source.BuildingDetails.StructuralType,
                MarketValue = source.BuildingDetails.MarketValue,
                AssessedValue = source.BuildingDetails.AssessedValue
            };
        }

        return copy;
    }

    private async Task<TaxDeclaration> BuildOrFindTaxDeclarationForRpaAsync(PropertyAssessmentRequest rpa, AssessmentPartitionItem? specificPart = null)
    {
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

        if (refTd == null && !string.IsNullOrWhiteSpace(rpa.PrevTDN))
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == rpa.PrevTDN);
        }

        if (refTd == null && specificPart != null && !string.IsNullOrWhiteSpace(specificPart.SystemGeneratedTDN))
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == specificPart.SystemGeneratedTDN);
        }

        var kindStr = specificPart?.Kind ?? rpa.Kind ?? refTd?.Kind.ToString() ?? "Building";
        var parsedKind = Enum.TryParse<PropertyKind>(kindStr, true, out var pk) ? pk : (refTd?.Kind ?? PropertyKind.Building);

        var td = refTd != null ? CloneTaxDeclaration(refTd) : new TaxDeclaration
        {
            Id = rpa.Id,
            Kind = parsedKind,
            TaxDeclarationNumber = !string.IsNullOrWhiteSpace(specificPart?.SystemGeneratedTDN)
                ? specificPart.SystemGeneratedTDN
                : (!string.IsNullOrWhiteSpace(rpa.PrevTDN) ? rpa.PrevTDN : rpa.TrackingNumber),
            PropertyIndexNumber = !string.IsNullOrWhiteSpace(specificPart?.PIN)
                ? specificPart.PIN
                : (!string.IsNullOrWhiteSpace(rpa.PrevPIN) ? rpa.PrevPIN : "N/A"),
            TransactionCode = rpa.TransactionTypeCode,
            OwnerName = !string.IsNullOrWhiteSpace(specificPart?.NewOwnerName) ? specificPart.NewOwnerName : rpa.NewOwnerName,
            OwnerAddress = !string.IsNullOrWhiteSpace(specificPart?.OwnerAddress) ? specificPart.OwnerAddress : (rpa.Address ?? string.Empty),
            OwnerTin = !string.IsNullOrWhiteSpace(specificPart?.OwnerTIN) ? specificPart.OwnerTIN : (rpa.OwnerTIN ?? string.Empty),
            OwnerPhone = !string.IsNullOrWhiteSpace(specificPart?.OwnerContactNo) ? specificPart.OwnerContactNo : (rpa.ContactNo ?? string.Empty),
            StreetNo = !string.IsNullOrWhiteSpace(specificPart?.StreetLocation) ? specificPart.StreetLocation : (rpa.PropertyLocation ?? string.Empty),
            LotNo = !string.IsNullOrWhiteSpace(specificPart?.LotNumber) ? specificPart.LotNumber : rpa.LotNo,
            SurveyNo = specificPart?.SurveyNumber,
            TitleNo = specificPart?.TitleNo,
            AreaSqMeters = specificPart?.AreaSqMeters ?? 0m,
            MarketValue = specificPart?.MarketValue ?? 0m,
            AssessedValue = specificPart?.AssessedValue ?? 0m,
            Memoranda = (!string.IsNullOrWhiteSpace(specificPart?.Memoranda) ? specificPart.Memoranda : rpa.Remarks) ?? string.Empty,
            EffectivityYear = specificPart?.EffectivityYear ?? DateTime.Now.Year
        };

        td.Kind = parsedKind;
        if (specificPart != null)
        {
            OverlayPartitionOntoTaxDeclaration(specificPart, td);
        }

        return td;
    }

    private async Task<TaxDeclaration> BuildOrFindTaxDeclarationForRfiAsync(RequestForInspection rfi, AssessmentPartitionItem? specificPart = null)
    {
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

        if (refTd == null && !string.IsNullOrWhiteSpace(rfi.TaxDeclarationNumber))
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == rfi.TaxDeclarationNumber);
        }

        if (refTd == null && specificPart != null && !string.IsNullOrWhiteSpace(specificPart.SystemGeneratedTDN))
        {
            refTd = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == specificPart.SystemGeneratedTDN);
        }

        var kindStr = specificPart?.Kind ?? rfi.Kind ?? refTd?.Kind.ToString() ?? "Building";
        var parsedKind = Enum.TryParse<PropertyKind>(kindStr, true, out var pk) ? pk : (refTd?.Kind ?? PropertyKind.Building);

        var td = refTd != null ? CloneTaxDeclaration(refTd) : new TaxDeclaration
        {
            Id = rfi.Id,
            Kind = parsedKind,
            TaxDeclarationNumber = !string.IsNullOrWhiteSpace(specificPart?.SystemGeneratedTDN)
                ? specificPart.SystemGeneratedTDN
                : (!string.IsNullOrWhiteSpace(rfi.TaxDeclarationNumber) ? rfi.TaxDeclarationNumber : rfi.TrackingNumber),
            PropertyIndexNumber = !string.IsNullOrWhiteSpace(specificPart?.PIN)
                ? specificPart.PIN
                : (!string.IsNullOrWhiteSpace(rfi.PropertyIndexNumber) ? rfi.PropertyIndexNumber : "N/A"),
            TransactionCode = rfi.TransactionCode ?? "INSP",
            OwnerName = !string.IsNullOrWhiteSpace(specificPart?.NewOwnerName) ? specificPart.NewOwnerName : rfi.OwnerName,
            OwnerAddress = !string.IsNullOrWhiteSpace(specificPart?.OwnerAddress) ? specificPart.OwnerAddress : (rfi.PropertyLocation ?? string.Empty),
            OwnerPhone = !string.IsNullOrWhiteSpace(specificPart?.OwnerContactNo) ? specificPart.OwnerContactNo : (rfi.ContactNumber ?? string.Empty),
            StreetNo = !string.IsNullOrWhiteSpace(specificPart?.StreetLocation) ? specificPart.StreetLocation : (rfi.PropertyLocation ?? string.Empty),
            LotNo = !string.IsNullOrWhiteSpace(specificPart?.LotNumber) ? specificPart.LotNumber : rfi.LotNo,
            SurveyNo = specificPart?.SurveyNumber,
            TitleNo = specificPart?.TitleNo,
            AreaSqMeters = specificPart?.AreaSqMeters ?? 0m,
            MarketValue = specificPart?.MarketValue ?? 0m,
            AssessedValue = specificPart?.AssessedValue ?? 0m,
            Memoranda = (!string.IsNullOrWhiteSpace(specificPart?.Memoranda) ? specificPart.Memoranda : rfi.Remarks) ?? string.Empty,
            EffectivityYear = specificPart?.EffectivityYear ?? DateTime.Now.Year
        };

        td.Kind = parsedKind;
        if (specificPart != null)
        {
            OverlayPartitionOntoTaxDeclaration(specificPart, td);
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

    private static void ApplyBuildingDTOToPartition(
        AssessmentPartitionItem part,
        TDNDTO dtoTdn,
        CAST_TDNBuildingDTO? dtoBldg,
        List<MobileAttachmentDTO>? attachments)
    {
        part.Kind = "Building";
        ApplyCoreTDNDTOToPartition(part, dtoTdn);

        if (dtoBldg != null)
        {
            if (!string.IsNullOrWhiteSpace(dtoBldg.StucturalType)) part.StructuralType = dtoBldg.StucturalType;
            else if (!string.IsNullOrWhiteSpace(dtoBldg.OtherStructure)) part.StructuralType = dtoBldg.OtherStructure;
            if (!string.IsNullOrWhiteSpace(dtoBldg.BuildingClassification)) part.BuildingClassification = dtoBldg.BuildingClassification;
            if (dtoBldg.StoreyNo.HasValue && dtoBldg.StoreyNo.Value > 0) part.NoOfStoreys = dtoBldg.StoreyNo.Value;
            if (!string.IsNullOrWhiteSpace(dtoBldg.PermitNo)) part.BuildingPermitNo = dtoBldg.PermitNo;
            if (!string.IsNullOrWhiteSpace(dtoBldg.OccupancyPermitNo)) part.OccupancyPermitNo = dtoBldg.OccupancyPermitNo;
            if (dtoBldg.Age.HasValue) part.BuildingAgeYears = dtoBldg.Age.Value;
            if (!string.IsNullOrWhiteSpace(dtoBldg.DateConstructed) && DateTime.TryParse(dtoBldg.DateConstructed, out var dConst)) part.DateConstructed = dConst;
            if (!string.IsNullOrWhiteSpace(dtoBldg.DateCompleted) && DateTime.TryParse(dtoBldg.DateCompleted, out var dComp)) part.DateCompleted = dComp;
            if (!string.IsNullOrWhiteSpace(dtoBldg.DateOccupied) && DateTime.TryParse(dtoBldg.DateOccupied, out var dOcc)) part.DateOccupied = dOcc;

            // 17 Structural Materials & Finishes
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralFoundation)) part.StructuralFoundation = dtoBldg.StructuralFoundation;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralColumns)) part.StructuralColumns = dtoBldg.StructuralColumns;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralBeams)) part.StructuralBeams = dtoBldg.StructuralBeams;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralTrussRoof)) part.StructuralTrussRoof = dtoBldg.StructuralTrussRoof;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralRoof)) part.StructuralRoof = dtoBldg.StructuralRoof;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralExteriorWalls)) part.StructuralExteriorWalls = dtoBldg.StructuralExteriorWalls;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralFlooring)) part.StructuralFlooring = dtoBldg.StructuralFlooring;
            if (!string.IsNullOrWhiteSpace(dtoBldg.DoorMain)) part.DoorMain = dtoBldg.DoorMain;
            if (!string.IsNullOrWhiteSpace(dtoBldg.Ceiling)) part.Ceiling = dtoBldg.Ceiling;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralWindows)) part.StructuralWindows = dtoBldg.StructuralWindows;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralStairs)) part.StructuralStairs = dtoBldg.StructuralStairs;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralPartitionWalls)) part.StructuralPartitionWalls = dtoBldg.StructuralPartitionWalls;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralFloorFinish)) part.StructuralFloorFinish = dtoBldg.StructuralFloorFinish;
            if (!string.IsNullOrWhiteSpace(dtoBldg.StructuralFinishFront)) part.StructuralFinishFront = dtoBldg.StructuralFinishFront;
            if (!string.IsNullOrWhiteSpace(dtoBldg.ElectricalSystem)) part.ElectricalSystem = dtoBldg.ElectricalSystem;
            if (!string.IsNullOrWhiteSpace(dtoBldg.ToiletAndBathroom)) part.ToiletAndBathroom = dtoBldg.ToiletAndBathroom;
            if (!string.IsNullOrWhiteSpace(dtoBldg.PlumbingSewer)) part.PlumbingSewer = dtoBldg.PlumbingSewer;

            // Schedules & Totals
            if (!string.IsNullOrWhiteSpace(dtoBldg.FloorBreakdownJson)) part.BuildingFloorBreakdownJson = dtoBldg.FloorBreakdownJson;
            if (!string.IsNullOrWhiteSpace(dtoBldg.AdditionalItemsJson)) part.BuildingAdditionalItemsJson = dtoBldg.AdditionalItemsJson;
            if (dtoBldg.SubtotalValCompArea.HasValue) part.BaseRcnValue = dtoBldg.SubtotalValCompArea.Value;
            if (dtoBldg.TotalAddItem.HasValue) part.BuildingTotalAdditionsValue = dtoBldg.TotalAddItem.Value;
            if (dtoBldg.DepreciationRate.HasValue) part.BuildingDepreciationPercent = dtoBldg.DepreciationRate.Value;
            if (dtoBldg.DepreciationAccumulated.HasValue) part.BuildingDepreciationAccumulated = dtoBldg.DepreciationAccumulated.Value;
            if (dtoBldg.DepreciationMarketval.HasValue) part.BuildingDepreciatedMarketValue = dtoBldg.DepreciationMarketval.Value;
            if (dtoBldg.TotalbldgArea.HasValue && dtoBldg.TotalbldgArea.Value > 0)
            {
                part.TotalBuildingAreaSqm = dtoBldg.TotalbldgArea.Value;
                part.AreaSqMeters = dtoBldg.TotalbldgArea.Value;
            }
            if (dtoBldg.TotalMarketVal.HasValue && dtoBldg.TotalMarketVal.Value > 0) part.MarketValue = dtoBldg.TotalMarketVal.Value;
            if (dtoBldg.TotalAssessedVal.HasValue && dtoBldg.TotalAssessedVal.Value > 0) part.AssessedValue = dtoBldg.TotalAssessedVal.Value;

            // CCT & Land Reference
            if (dtoBldg.IsCct.HasValue) part.IsCct = dtoBldg.IsCct.Value;
            if (!string.IsNullOrWhiteSpace(dtoBldg.CctTitleNo)) part.CctTitleNo = dtoBldg.CctTitleNo;
            if (!string.IsNullOrWhiteSpace(dtoBldg.CctDateRegistered) && DateTime.TryParse(dtoBldg.CctDateRegistered, out var cctDate)) part.CctDateRegistered = cctDate;
            if (!string.IsNullOrWhiteSpace(dtoBldg.OwnerNameReflectedInCct)) part.OwnerNameReflectedInCct = dtoBldg.OwnerNameReflectedInCct;
            if (!string.IsNullOrWhiteSpace(dtoBldg.LandOwnerName)) part.LandOwnerName = dtoBldg.LandOwnerName;
            if (!string.IsNullOrWhiteSpace(dtoBldg.LandTdNo)) part.LandTdNo = dtoBldg.LandTdNo;
            if (!string.IsNullOrWhiteSpace(dtoBldg.LandPin)) part.LandPin = dtoBldg.LandPin;

            // Demographics & Property details from dtoBldg
            if (!string.IsNullOrWhiteSpace(dtoBldg.Gender)) part.OwnerGender = dtoBldg.Gender;
            if (!string.IsNullOrWhiteSpace(dtoBldg.CivilStatus)) part.OwnerCivilStatus = dtoBldg.CivilStatus;
            if (!string.IsNullOrWhiteSpace(dtoBldg.Email)) part.OwnerEmail = dtoBldg.Email;
            if (!string.IsNullOrWhiteSpace(dtoBldg.MobileNo)) part.OwnerContactNo = dtoBldg.MobileNo;
            if (!string.IsNullOrWhiteSpace(dtoBldg.TIN)) part.OwnerTIN = dtoBldg.TIN;
            if (!string.IsNullOrWhiteSpace(dtoBldg.MailingAddress)) part.OwnerAddress = dtoBldg.MailingAddress;
            if (!string.IsNullOrWhiteSpace(dtoBldg.STREET)) part.StreetLocation = dtoBldg.STREET;
            if (!string.IsNullOrWhiteSpace(dtoBldg.LotNo)) part.LotNumber = dtoBldg.LotNo;
            if (!string.IsNullOrWhiteSpace(dtoBldg.SurveyNo)) part.SurveyNumber = dtoBldg.SurveyNo;
            if (!string.IsNullOrWhiteSpace(dtoBldg.TCTNo)) part.TitleNo = dtoBldg.TCTNo;
            if (!string.IsNullOrWhiteSpace(dtoBldg.ARPNo)) part.ARPNo = dtoBldg.ARPNo;
            if (!string.IsNullOrWhiteSpace(dtoBldg.PropertyTypeAccordUse)) part.PropertyTypeAccordUse = dtoBldg.PropertyTypeAccordUse;
            if (!string.IsNullOrWhiteSpace(dtoBldg.OtherPropertyTypeAccordUse)) part.OtherPropertyTypeAccordUse = dtoBldg.OtherPropertyTypeAccordUse;
        }

        if (attachments != null && attachments.Count > 0)
        {
            part.AttachmentsJson = JsonSerializer.Serialize(attachments, _jsonOptions);
        }
    }

    private static void ApplyLandDTOToPartition(
        AssessmentPartitionItem part,
        TDNDTO dtoTdn,
        TDNLandDataDTO? dtoLand,
        List<MobileAttachmentDTO>? attachments)
    {
        part.Kind = "Land";
        ApplyCoreTDNDTOToPartition(part, dtoTdn);

        if (dtoLand != null)
        {
            if (!string.IsNullOrWhiteSpace(dtoLand.NorthBoundary)) part.BoundaryNorth = dtoLand.NorthBoundary;
            if (!string.IsNullOrWhiteSpace(dtoLand.SouthBoundary)) part.BoundarySouth = dtoLand.SouthBoundary;
            if (!string.IsNullOrWhiteSpace(dtoLand.EastBoundary)) part.BoundaryEast = dtoLand.EastBoundary;
            if (!string.IsNullOrWhiteSpace(dtoLand.WestBoundary)) part.BoundaryWest = dtoLand.WestBoundary;
            if (!string.IsNullOrWhiteSpace(dtoLand.Nremarks)) part.Nremarks = dtoLand.Nremarks;
            if (!string.IsNullOrWhiteSpace(dtoLand.Sremarks)) part.Sremarks = dtoLand.Sremarks;
            if (!string.IsNullOrWhiteSpace(dtoLand.Eremarks)) part.Eremarks = dtoLand.Eremarks;
            if (!string.IsNullOrWhiteSpace(dtoLand.Wremarks)) part.Wremarks = dtoLand.Wremarks;
            if (!string.IsNullOrWhiteSpace(dtoLand.Classification)) part.Classification = dtoLand.Classification;
            if (!string.IsNullOrWhiteSpace(dtoLand.ActualUse)) part.ActualUse = dtoLand.ActualUse;
            if (dtoLand.UnitValue.HasValue && dtoLand.UnitValue.Value > 0) part.UnitValue = dtoLand.UnitValue.Value;
            if (dtoLand.AssessmentLevelPercent.HasValue && dtoLand.AssessmentLevelPercent.Value > 0) part.AssessmentLevelPercent = dtoLand.AssessmentLevelPercent.Value;
            if (!string.IsNullOrWhiteSpace(dtoLand.LandAppraisalDetailsJson)) part.LandAppraisalDetailsJson = dtoLand.LandAppraisalDetailsJson;
            if (!string.IsNullOrWhiteSpace(dtoLand.LandImprovementsJson)) part.LandImprovementsJson = dtoLand.LandImprovementsJson;
            if (!string.IsNullOrWhiteSpace(dtoLand.LandAdjustmentsJson)) part.LandAdjustmentsJson = dtoLand.LandAdjustmentsJson;
            if (!string.IsNullOrWhiteSpace(dtoLand.LandPropertyAssessmentsJson)) part.LandPropertyAssessmentsJson = dtoLand.LandPropertyAssessmentsJson;
        }

        if (attachments != null && attachments.Count > 0)
        {
            part.AttachmentsJson = JsonSerializer.Serialize(attachments, _jsonOptions);
        }
    }

    private static void ApplyMachineryDTOToPartition(
        AssessmentPartitionItem part,
        TDNDTO dtoTdn,
        TDNMachineDTO dtoMach,
        List<MobileAttachmentDTO>? attachments)
    {
        part.Kind = "Machinery";
        ApplyCoreTDNDTOToPartition(part, dtoTdn);

        if (dtoMach.TDNMachine != null)
        {
            var m = dtoMach.TDNMachine;
            if (!string.IsNullOrWhiteSpace(m.LandOwnerPIN)) part.LandPin = m.LandOwnerPIN;
            if (!string.IsNullOrWhiteSpace(m.BuildingOwnerPIN)) part.BuildingPin = m.BuildingOwnerPIN;
            if (!string.IsNullOrWhiteSpace(m.LandOwner)) part.LandOwnerName = m.LandOwner;
            if (!string.IsNullOrWhiteSpace(m.BuildingOwner)) part.BuildingReferenceName = m.BuildingOwner;
            if (!string.IsNullOrWhiteSpace(m.LandTdNo)) part.LandTdNo = m.LandTdNo;
            if (!string.IsNullOrWhiteSpace(m.BuildingTdNo)) part.BuildingTdNo = m.BuildingTdNo;
            part.IsLandOwnerAlsoBuildingOwner = m.IsLandOwnerAlsoBuildingOwner;
            part.IsLocallyManufactured = m.IsLocallyManufactured;
            if (!string.IsNullOrWhiteSpace(m.MachineryRegNo)) part.MachineryRegNo = m.MachineryRegNo;
            if (m.MachineryDateRegistered.HasValue) part.MachineryDateRegistered = m.MachineryDateRegistered.Value;
            if (!string.IsNullOrWhiteSpace(m.ImportationPermitNo)) part.ImportationPermitNo = m.ImportationPermitNo;
            if (m.ImportationDateIssued.HasValue) part.ImportationDateIssued = m.ImportationDateIssued.Value;
            if (!string.IsNullOrWhiteSpace(m.CountryOfOrigin)) part.CountryOfOrigin = m.CountryOfOrigin;
            if (!string.IsNullOrWhiteSpace(m.SupplierSeller)) part.SupplierSeller = m.SupplierSeller;
            if (!string.IsNullOrWhiteSpace(m.SupplierAddress)) part.SupplierAddress = m.SupplierAddress;
            if (m.AcquisitionCost.HasValue && m.AcquisitionCost.Value > 0) part.AcquisitionCost = m.AcquisitionCost.Value;
            if (!string.IsNullOrWhiteSpace(m.OrNo)) part.OrNo = m.OrNo;
            if (m.OrDate.HasValue) part.OrDate = m.OrDate.Value;
            if (!string.IsNullOrWhiteSpace(m.OrNoAndDate)) part.OrNoAndDate = m.OrNoAndDate;
            if (m.LocalDateIssued.HasValue) part.LocalDateIssued = m.LocalDateIssued.Value;
            if (!string.IsNullOrWhiteSpace(m.MachineryAppraisalDetailsJson)) part.MachineryAppraisalDetailsJson = m.MachineryAppraisalDetailsJson;
            if (!string.IsNullOrWhiteSpace(m.MachineryPropertyAssessmentsJson)) part.MachineryPropertyAssessmentsJson = m.MachineryPropertyAssessmentsJson;
        }

        if (!string.IsNullOrWhiteSpace(dtoTdn.MachineryAppraisalDetailsJson)) part.MachineryAppraisalDetailsJson = dtoTdn.MachineryAppraisalDetailsJson;
        if (!string.IsNullOrWhiteSpace(dtoTdn.MachineryPropertyAssessmentsJson)) part.MachineryPropertyAssessmentsJson = dtoTdn.MachineryPropertyAssessmentsJson;

        if (dtoMach.TDNMachineDetail != null && dtoMach.TDNMachineDetail.Count > 0)
        {
            var first = dtoMach.TDNMachineDetail[0];
            if (first.OriginalCost.HasValue && first.OriginalCost.Value > 0) part.AcquisitionCost = first.OriginalCost.Value;
        }

        if (attachments != null && attachments.Count > 0)
        {
            part.AttachmentsJson = JsonSerializer.Serialize(attachments, _jsonOptions);
        }
    }

    private static void ApplyCoreTDNDTOToPartition(AssessmentPartitionItem part, TDNDTO dtoTdn)
    {
        if (!string.IsNullOrWhiteSpace(dtoTdn.PIN)) part.PIN = dtoTdn.PIN;
        if (!string.IsNullOrWhiteSpace(dtoTdn.ARPNo)) part.ARPNo = dtoTdn.ARPNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.SectionNo)) part.SectionNo = dtoTdn.SectionNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.LotNo)) part.LotNumber = dtoTdn.LotNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.BlockNo)) part.BlockNumber = dtoTdn.BlockNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.SurveyNo)) part.SurveyNumber = dtoTdn.SurveyNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.CadastralSurveyNo))
        {
            part.CadastralSurveyNo = dtoTdn.CadastralSurveyNo;
            if (string.IsNullOrWhiteSpace(part.SurveyNumber)) part.SurveyNumber = dtoTdn.CadastralSurveyNo;
        }
        if (!string.IsNullOrWhiteSpace(dtoTdn.PropLocationStreet)) part.StreetLocation = dtoTdn.PropLocationStreet;
        else if (!string.IsNullOrWhiteSpace(dtoTdn.Location) && string.IsNullOrWhiteSpace(part.StreetLocation)) part.StreetLocation = dtoTdn.Location;

        if (!string.IsNullOrWhiteSpace(dtoTdn.ParcelNo)) part.ParcelNo = dtoTdn.ParcelNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.DistrictOrBarangay)) part.DistrictOrBarangay = dtoTdn.DistrictOrBarangay;
        if (!string.IsNullOrWhiteSpace(dtoTdn.CityMunicipality)) part.CityMunicipality = dtoTdn.CityMunicipality;
        if (!string.IsNullOrWhiteSpace(dtoTdn.Province)) part.Province = dtoTdn.Province;
        if (!string.IsNullOrWhiteSpace(dtoTdn.Purok)) part.Purok = dtoTdn.Purok;

        var locTokens = new[] { part.StreetLocation, part.Purok, part.DistrictOrBarangay, part.CityMunicipality, part.Province }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var fullLoc = string.Join(", ", locTokens);
        if (!string.IsNullOrWhiteSpace(fullLoc)) part.PropertyLocation = fullLoc;

        if (!string.IsNullOrWhiteSpace(dtoTdn.DisplayName)) part.NewOwnerName = dtoTdn.DisplayName;
        if (!string.IsNullOrWhiteSpace(dtoTdn.OwnerAddress)) part.OwnerAddress = dtoTdn.OwnerAddress;
        if (!string.IsNullOrWhiteSpace(dtoTdn.OwnerTIN)) part.OwnerTIN = dtoTdn.OwnerTIN;
        if (!string.IsNullOrWhiteSpace(dtoTdn.OwnerContactNo)) part.OwnerContactNo = dtoTdn.OwnerContactNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.OwnerEmail)) part.OwnerEmail = dtoTdn.OwnerEmail;
        if (!string.IsNullOrWhiteSpace(dtoTdn.OwnerGender)) part.OwnerGender = dtoTdn.OwnerGender;

        part.IsAdministrator = dtoTdn.IsAdministrator;
        part.IsBeneficialUser = dtoTdn.IsBeneficialUser;
        part.IsPossessor = dtoTdn.IsPossessor;

        if (!string.IsNullOrWhiteSpace(dtoTdn.AdministratorName) || !string.IsNullOrWhiteSpace(dtoTdn.PropAdminName))
            part.AdministratorName = dtoTdn.AdministratorName ?? dtoTdn.PropAdminName;
        if (!string.IsNullOrWhiteSpace(dtoTdn.AdministratorAddress) || !string.IsNullOrWhiteSpace(dtoTdn.PropAdminAddress))
            part.AdministratorAddress = dtoTdn.AdministratorAddress ?? dtoTdn.PropAdminAddress;
        if (!string.IsNullOrWhiteSpace(dtoTdn.AdministratorContactNo) || !string.IsNullOrWhiteSpace(dtoTdn.PropAdminContactNo))
            part.AdministratorContactNo = dtoTdn.AdministratorContactNo ?? dtoTdn.PropAdminContactNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.AdministratorTin)) part.AdministratorTin = dtoTdn.AdministratorTin;
        if (!string.IsNullOrWhiteSpace(dtoTdn.AdministratorEmail)) part.AdministratorEmail = dtoTdn.AdministratorEmail;
        if (!string.IsNullOrWhiteSpace(dtoTdn.AdministratorGender)) part.AdministratorGender = dtoTdn.AdministratorGender;

        if (!string.IsNullOrWhiteSpace(dtoTdn.TitleNo)) part.TitleNo = dtoTdn.TitleNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.TCTNo) && string.IsNullOrWhiteSpace(part.TitleNo)) part.TitleNo = dtoTdn.TCTNo;
        if (!string.IsNullOrWhiteSpace(dtoTdn.RegistrationStatus)) part.RegistrationStatus = dtoTdn.RegistrationStatus;
        if (!string.IsNullOrWhiteSpace(dtoTdn.TitleType)) part.TitleType = dtoTdn.TitleType;
        if (dtoTdn.DateRegistered.HasValue) part.DateRegistered = dtoTdn.DateRegistered.Value;

        if (!string.IsNullOrWhiteSpace(dtoTdn.PropertyTypeAccordUse)) part.PropertyTypeAccordUse = dtoTdn.PropertyTypeAccordUse;
        if (!string.IsNullOrWhiteSpace(dtoTdn.OtherPropertyTypeAccordUse)) part.OtherPropertyTypeAccordUse = dtoTdn.OtherPropertyTypeAccordUse;
        if (!string.IsNullOrWhiteSpace(dtoTdn.Classification)) part.Classification = dtoTdn.Classification;
        if (!string.IsNullOrWhiteSpace(dtoTdn.ActualUse)) part.ActualUse = dtoTdn.ActualUse;

        if (dtoTdn.UnitValue.HasValue && dtoTdn.UnitValue.Value > 0) part.UnitValue = dtoTdn.UnitValue.Value;
        if (dtoTdn.TotalMarketValue.HasValue && dtoTdn.TotalMarketValue.Value > 0) part.MarketValue = dtoTdn.TotalMarketValue.Value;
        if (dtoTdn.TotalAssessedValue.HasValue && dtoTdn.TotalAssessedValue.Value > 0) part.AssessedValue = dtoTdn.TotalAssessedValue.Value;
        if (dtoTdn.AssessmentLevelPercent.HasValue && dtoTdn.AssessmentLevelPercent.Value > 0) part.AssessmentLevelPercent = dtoTdn.AssessmentLevelPercent.Value;
        if (dtoTdn.TotalArea.HasValue && dtoTdn.TotalArea.Value > 0)
        {
            part.AreaSqMeters = dtoTdn.TotalArea.Value;
            part.TotalBuildingAreaSqm = dtoTdn.TotalArea.Value;
        }

        if (!string.IsNullOrWhiteSpace(dtoTdn.AssessmentBasis)) part.AssessmentBasis = dtoTdn.AssessmentBasis;
        if (dtoTdn.BackTaxesYearFrom.HasValue) part.BackTaxesYearFrom = dtoTdn.BackTaxesYearFrom.Value;
        if (dtoTdn.BackTaxesYearTo.HasValue) part.BackTaxesYearTo = dtoTdn.BackTaxesYearTo.Value;
        if (dtoTdn.YearEffectivity.HasValue) part.EffectivityYear = dtoTdn.YearEffectivity.Value;
        if (dtoTdn.QuarterEffectivity.HasValue)
        {
            int qVal = dtoTdn.QuarterEffectivity.Value;
            part.EffectivityQuarter = $"{qVal}{(qVal == 1 ? "st" : qVal == 2 ? "nd" : qVal == 3 ? "rd" : "th")} Quarter";
        }
        part.Taxability = dtoTdn.Taxability ? "Taxable" : "Exempt";
        if (!string.IsNullOrWhiteSpace(dtoTdn.Memoranda)) part.Memoranda = dtoTdn.Memoranda;

        if (!string.IsNullOrWhiteSpace(dtoTdn.LandAppraisalDetailsJson)) part.LandAppraisalDetailsJson = dtoTdn.LandAppraisalDetailsJson;
        if (!string.IsNullOrWhiteSpace(dtoTdn.LandImprovementsJson)) part.LandImprovementsJson = dtoTdn.LandImprovementsJson;
        if (!string.IsNullOrWhiteSpace(dtoTdn.LandAdjustmentsJson)) part.LandAdjustmentsJson = dtoTdn.LandAdjustmentsJson;
        if (!string.IsNullOrWhiteSpace(dtoTdn.LandPropertyAssessmentsJson)) part.LandPropertyAssessmentsJson = dtoTdn.LandPropertyAssessmentsJson;

        if (!string.IsNullOrWhiteSpace(dtoTdn.FloorPlanSketchPath)) part.BuildingFloorPlanSketchPath = dtoTdn.FloorPlanSketchPath;
    }

    // Saves a mobile-captured sketch (base64) during bulk sync and rewrites dtoTdn.FloorPlanSketchPath.
    // Local device URIs are never persisted.
    private void ProcessAndSaveSketch(TDNDTO? dtoTdn, string trackingNo)
    {
        if (dtoTdn == null) return;

        if (!string.IsNullOrWhiteSpace(dtoTdn.FloorPlanSketchBase64))
        {
            try
            {
                string cleanTracking = string.IsNullOrWhiteSpace(trackingNo) ? "general" : Regex.Replace(trackingNo.Trim(), @"[^a-zA-Z0-9_\-]", "_");
                string webRoot = !string.IsNullOrWhiteSpace(_env.WebRootPath)
                    ? _env.WebRootPath
                    : Path.Combine(_env.ContentRootPath, "wwwroot");
                string targetDir = Path.Combine(webRoot, "uploads", "InspectionSketches", cleanTracking);
                Directory.CreateDirectory(targetDir);

                string raw = dtoTdn.FloorPlanSketchBase64;
                if (raw.Contains(",")) raw = raw.Substring(raw.IndexOf(",") + 1);
                byte[] bytes = Convert.FromBase64String(raw);

                string ext = Path.GetExtension(dtoTdn.FloorPlanSketchFileName ?? "");
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                string baseName = Regex.Replace(Path.GetFileNameWithoutExtension(dtoTdn.FloorPlanSketchFileName ?? "sketch"), @"[^a-zA-Z0-9_\-]", "_");
                string fileName = $"{Guid.NewGuid():N}_{baseName}{ext}";
                System.IO.File.WriteAllBytes(Path.Combine(targetDir, fileName), bytes);

                dtoTdn.FloorPlanSketchPath = $"/uploads/InspectionSketches/{cleanTracking}/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving base64 sketch: {ex.Message}");
            }
            finally
            {
                dtoTdn.FloorPlanSketchBase64 = null;
            }
        }

        var p = dtoTdn.FloorPlanSketchPath;
        if (!string.IsNullOrWhiteSpace(p) &&
            (p.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || p.StartsWith("content:", StringComparison.OrdinalIgnoreCase) || p.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)))
        {
            dtoTdn.FloorPlanSketchPath = null; // unsynced local URI; keep existing server value
        }
    }

    private List<MobileAttachmentDTO> ProcessAndSaveAttachments(List<MobileAttachmentDTO>? attachments, string trackingNo)
    {
        if (attachments == null || attachments.Count == 0) return new List<MobileAttachmentDTO>();

        var result = new List<MobileAttachmentDTO>();
        string cleanTracking = string.IsNullOrWhiteSpace(trackingNo) ? "general" : Regex.Replace(trackingNo.Trim(), @"[^a-zA-Z0-9_\-]", "_");
        string webRoot = !string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? _env.WebRootPath
            : Path.Combine(_env.ContentRootPath, "wwwroot");
        string targetDir = Path.Combine(webRoot, "uploads", "InspectionAttachments", cleanTracking);

        foreach (var att in attachments)
        {
            if (att == null) continue;
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

            if (!string.IsNullOrWhiteSpace(att.FileUrl) &&
                (att.FileUrl.StartsWith("blob:", StringComparison.OrdinalIgnoreCase) ||
                 att.FileUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                 att.FileUrl.StartsWith("content:", StringComparison.OrdinalIgnoreCase)))
            {
                continue; // Do not persist un-saved local or temporary blob URIs
            }

            result.Add(att);
        }

        return result;
    }

    // =========================================================================
    // DEDICATED TAX DECLARATION MAPPERS (FOR EXISTING TDN LOOKUP)
    // =========================================================================

    private TDNDTO BuildCoreTDNDTOFromTaxDeclaration(
        TaxDeclaration td,
        string? trackingNo = null,
        long? formWorkflowId = null,
        string? sketchPath = null,
        List<MobileAttachmentDTO>? attachments = null,
        int? partitionItemIndex = null,
        string? partitionIndex = null)
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
            OwnerTIN = td.OwnerTin,
            OwnerContactNo = td.OwnerPhone,
            OwnerEmail = td.OwnerEmail,
            OwnerCivilStatus = td.OwnerCivilStatus,
            IsAdministrator = td.IsAdministrator,
            IsBeneficialUser = td.IsBeneficialUser,
            IsPossessor = td.IsPossessor,
            AdministratorName = td.AdministratorName,
            AdministratorAddress = td.AdministratorAddress,
            AdministratorContactNo = td.AdministratorContactNo,
            AdministratorTin = td.AdministratorTin,
            AdministratorEmail = td.AdministratorEmail,
            AdministratorGender = td.AdministratorGender,
            PropAdminName = td.AdministratorName,
            PropAdminAddress = td.AdministratorAddress,
            PropAdminContactNo = td.AdministratorContactNo,
            PropLocationStreet = td.StreetNo,
            Location = $"{td.DistrictOrBarangay}, {td.Municipality}",
            CadastralSurveyNo = td.CadastralSurveyNo,
            SurveyNo = td.SurveyNo,
            LotNo = td.LotNo,
            BlockNo = td.BlockNo,
            SectionNo = td.SectionNo,
            ParcelNo = td.ParcelNo,
            TCTNo = td.TitleNo,
            TitleNo = td.TitleNo,
            TCTDate = td.DateRegistered,
            DateRegistration = td.DateRegistered,
            DateRegistered = td.DateRegistered,
            RegistrationStatus = td.RegistrationStatus,
            TitleType = td.TitleType,
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
            FloorPlanSketchPath = ResolveFullUrl(sketchPath ?? td.FloorPlanSketchPath),
            Attachments = attachments ?? new()
        };
    }

    private TDNBuildingDTO MapTaxDeclarationToBuildingDTO(
        TaxDeclaration td,
        string? trackingNo = null,
        long? formWorkflowId = null,
        string? sketchPath = null,
        List<MobileAttachmentDTO>? attachments = null,
        int? partitionItemIndex = null,
        string? partitionIndex = null)
    {
        var b = td.BuildingDetails ?? td.EnsureBuildingDetails();
        var tdnDto = BuildCoreTDNDTOFromTaxDeclaration(td, trackingNo, formWorkflowId, sketchPath, attachments, partitionItemIndex, partitionIndex);

        return new TDNBuildingDTO
        {
            TDN = tdnDto,
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
                TransactionCode = td.TransactionCode,
                FloorBreakdownJson = b.FloorBreakdownJson,
                AdditionalItemsJson = b.AdditionalItemsJson
            }
        };
    }

    private TDNLandDTO MapTaxDeclarationToLandDTO(
        TaxDeclaration td,
        string? trackingNo = null,
        long? formWorkflowId = null,
        string? sketchPath = null,
        List<MobileAttachmentDTO>? attachments = null,
        int? partitionItemIndex = null,
        string? partitionIndex = null)
    {
        var l = td.LandDetails ?? td.EnsureLandDetails();
        var tdnDto = BuildCoreTDNDTOFromTaxDeclaration(td, trackingNo, formWorkflowId, sketchPath, attachments, partitionItemIndex, partitionIndex);

        return new TDNLandDTO
        {
            TDN = tdnDto,
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
                Wremarks = l.Wremarks,
                Classification = td.Classification.ToString(),
                ActualUse = td.ActualUse,
                UnitValue = td.UnitValue,
                AssessmentLevelPercent = 20m
            }
        };
    }

    private TDNMachineDTO MapTaxDeclarationToMachineryDTO(
        TaxDeclaration td,
        string? trackingNo = null,
        long? formWorkflowId = null,
        string? sketchPath = null,
        List<MobileAttachmentDTO>? attachments = null,
        int? partitionItemIndex = null,
        string? partitionIndex = null)
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

        var tdnDto = BuildCoreTDNDTOFromTaxDeclaration(td, trackingNo, formWorkflowId, sketchPath, attachments, partitionItemIndex, partitionIndex);

        return new TDNMachineDTO
        {
            TDN = tdnDto,
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
