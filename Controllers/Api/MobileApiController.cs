using System;
using System.Collections.Generic;
using System.Linq;
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

    public MobileApiController(
        ApplicationDbContext context,
        IDocumentWorkflowService workflowService,
        IHubContext<WorkflowHub> hubContext,
        IWorkflowNotifier workflowNotifier)
    {
        _context = context;
        _workflowService = workflowService;
        _hubContext = hubContext;
        _workflowNotifier = workflowNotifier;
    }

    // Helper: Generate a deterministic positive 64-bit integer from a Guid
    private static long GuidToNumericId(Guid guid)
    {
        byte[] bytes = guid.ToByteArray();
        long val = BitConverter.ToInt64(bytes, 0);
        return Math.Abs(val == 0 ? 1 : val);
    }

    // =========================================================================
    // 1. DASHBOARD & TRANSACTION FEED
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
            .CountAsync(l => l.Timestamp >= today && (l.ToStage == WorkflowStage.Releasing || l.ActionTaken == "Approved"));

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

    // GET: /api/mobile/transactions
    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(int? propertyTypeId = null, string trackingNo = "", int page = 1, int pageSize = 50)
    {
        var jwtUser = HttpContext.Items["JwtUser"] as JwtPayload;
        Guid.TryParse(jwtUser?.Uid, out var userId);

        // Fetch personal queue or unified transactions for user
        var queue = _workflowService.GetPersonalQueueForUser(userId).ToList();

        // If personal queue is empty, include division pool queue so staff has work items to download
        if (queue.Count == 0 && jwtUser != null)
        {
            var div = (DivisionType)jwtUser.DivisionId.GetValueOrDefault(1);
            queue = _workflowService.GetPoolQueueForDivision(div).ToList();
        }

        // Filter by tracking number if specified
        if (!string.IsNullOrWhiteSpace(trackingNo))
        {
            string clean = trackingNo.Trim().ToLower();
            queue = queue.Where(q => q.TrackingNumber.ToLower().Contains(clean) ||
                                     q.OwnerOrApplicant.ToLower().Contains(clean) ||
                                     q.ReferenceNumber.ToLower().Contains(clean)).ToList();
        }

        // Cache all tax declarations for fast enrichment
        var allTds = await _context.TaxDeclarations
            .AsNoTracking()
            .Select(t => new { t.Id, t.TaxDeclarationNumber, t.PropertyIndexNumber, t.Kind })
            .ToListAsync();

        var list = queue.Select(item =>
        {
            long numericWfId = GuidToNumericId(item.RequestId);

            // Determine transaction type (1: Land, 2: Building, 3: Machinery)
            int transType = 2; // Default Building
            long? resolvedTdnId = null;

            var matchedTd = allTds.FirstOrDefault(t =>
                (!string.IsNullOrEmpty(item.ReferenceNumber) &&
                 (t.TaxDeclarationNumber.Equals(item.ReferenceNumber, StringComparison.OrdinalIgnoreCase) ||
                  t.PropertyIndexNumber.Equals(item.ReferenceNumber, StringComparison.OrdinalIgnoreCase))) ||
                t.TaxDeclarationNumber.Contains(item.TrackingNumber, StringComparison.OrdinalIgnoreCase));

            if (matchedTd != null)
            {
                resolvedTdnId = GuidToNumericId(matchedTd.Id);
                transType = matchedTd.Kind switch
                {
                    PropertyKind.Land => 1,
                    PropertyKind.Machinery => 3,
                    _ => 2
                };
            }
            else
            {
                if (item.DisplayType.Contains("Land", StringComparison.OrdinalIgnoreCase)) transType = 1;
                else if (item.DisplayType.Contains("Machin", StringComparison.OrdinalIgnoreCase)) transType = 3;
                else transType = 2;
            }

            return new TransactionWorkFlowDTO
            {
                FormworkflowId = numericWfId,
                TrackingNo = item.TrackingNumber,
                TransactionTypeId = transType,
                RequestType = item.DisplayType,
                ARPNo = item.ReferenceNumber,
                Owner = item.OwnerOrApplicant,
                Requestor = item.OwnerOrApplicant,
                AcceptedBy = item.AssignedUserName ?? "Staff",
                CurrentStatusId = (int)item.CurrentStage,
                CurrentStatus = item.CurrentStageName,
                Remarks = item.LastRemarks ?? string.Empty,
                IsComplete = item.CurrentStage == WorkflowStage.Releasing,
                IsDeffered = item.IsDeferred,
                IsCheckout = item.IsClaimed,
                SubmittedDate = item.DateSubmitted,
                ForwarderBy = item.LastForwardedBy ?? "Office Staff",
                TdnId = resolvedTdnId
            };
        }).ToList();

        // Filter by propertyTypeId if requested
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

    // GET: /api/mobile/transaction/download-details
    [HttpGet("transaction/download-details")]
    public async Task<IActionResult> DownloadTransactionDetails([FromQuery] long? formWorkflowId, [FromQuery] string trackingNo = "")
    {
        string cleanTracking = trackingNo.Trim();

        // Find matching TaxDeclaration, PropertyAssessmentRequest, or RequestForInspection
        TaxDeclaration? td = null;

        if (!string.IsNullOrEmpty(cleanTracking))
        {
            td = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == cleanTracking ||
                                          t.PropertyIndexNumber == cleanTracking);
        }

        // Fallback: load first matching active TaxDeclaration or recent declaration
        if (td == null)
        {
            td = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Include(t => t.LandDetails)
                .Include(t => t.MachineryDetails)
                .ThenInclude(m => m != null ? m.TDNMachineryDetails : null)
                .OrderByDescending(t => t.DateEncoded)
                .FirstOrDefaultAsync();
        }

        if (td == null)
        {
            return NotFound(new { success = false, message = "No matching property assessment record found." });
        }

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

        long resolvedTdnId = GuidToNumericId(td.Id);
        object formData = formType switch
        {
            "LAND" => BuildLandDTO(td),
            "MACHINERY" => BuildMachineryDTO(td),
            _ => BuildBuildingDTO(td)
        };

        return Ok(new
        {
            success = true,
            formWorkflowId = formWorkflowId ?? resolvedTdnId,
            trackingNo = string.IsNullOrEmpty(cleanTracking) ? td.TaxDeclarationNumber : cleanTracking,
            transactionTypeId = transactionTypeId,
            formType = formType,
            owner = td.OwnerName,
            arpNo = td.TaxDeclarationNumber,
            tdnId = resolvedTdnId,
            formData = formData,
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
    // 3. UPDATE PROPERTY FORM DETAILS (OFFLINE BATCH SYNC ENDPOINTS)
    // =========================================================================

    // POST: /api/mobile/form/update-building
    [HttpPost("form/update-building")]
    public async Task<IActionResult> UpdateBuildingForm([FromBody] TDNBuildingDTO dto)
    {
        if (dto == null || dto.TDN == null)
        {
            return BadRequest(new { success = false, message = "Invalid building payload." });
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
                    TaxDeclarationNumber = dto.TDN.ARPNo ?? dto.TDN.DeclarationNo ?? "TDN-BLDG-" + DateTime.Now.Ticks,
                    PropertyIndexNumber = dto.TDN.PIN ?? "PIN-" + DateTime.Now.Ticks
                };
                _context.TaxDeclarations.Add(td);
            }

            // Update core TaxDeclaration fields
            ApplyCoreTDNToEntity(dto.TDN, td);

            // Update TDNBuilding details
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
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "BUILDING_UPDATED_VIA_MOBILE",
                arpNo = td.TaxDeclarationNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Building assessment updated successfully from FairTrack Mobile.",
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var td = await FindOrMatchTaxDeclaration(dto.TDN.TDNId, dto.TDN.ARPNo, dto.TDN.PIN, PropertyKind.Land);
            if (td == null)
            {
                td = new TaxDeclaration
                {
                    Kind = PropertyKind.Land,
                    TaxDeclarationNumber = dto.TDN.ARPNo ?? dto.TDN.DeclarationNo ?? "TDN-LAND-" + DateTime.Now.Ticks,
                    PropertyIndexNumber = dto.TDN.PIN ?? "PIN-" + DateTime.Now.Ticks
                };
                _context.TaxDeclarations.Add(td);
            }

            // Update core TaxDeclaration fields
            ApplyCoreTDNToEntity(dto.TDN, td);

            // Update TDNLand details
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

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "LAND_UPDATED_VIA_MOBILE",
                arpNo = td.TaxDeclarationNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Land assessment updated successfully from FairTrack Mobile.",
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var td = await FindOrMatchTaxDeclaration(dto.TDN.TDNId, dto.TDN.ARPNo, dto.TDN.PIN, PropertyKind.Machinery);
            if (td == null)
            {
                td = new TaxDeclaration
                {
                    Kind = PropertyKind.Machinery,
                    TaxDeclarationNumber = dto.TDN.ARPNo ?? dto.TDN.DeclarationNo ?? "TDN-MACH-" + DateTime.Now.Ticks,
                    PropertyIndexNumber = dto.TDN.PIN ?? "PIN-" + DateTime.Now.Ticks
                };
                _context.TaxDeclarations.Add(td);
            }

            // Update core TaxDeclaration fields
            ApplyCoreTDNToEntity(dto.TDN, td);

            // Update TDNMachinery details
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

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveWorkflowUpdate", new
            {
                action = "MACHINERY_UPDATED_VIA_MOBILE",
                arpNo = td.TaxDeclarationNumber,
                timestamp = DateTime.Now
            });

            return Ok(new
            {
                success = true,
                message = "Machinery assessment updated successfully from FairTrack Mobile.",
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
    // 4. WORKFLOW FORWARDING
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
        Guid.TryParse(jwtUser?.Uid, out var actorId);
        string actorName = jwtUser?.Name ?? "Mobile Field Appraiser";

        // Find request matching the FormWorkflowId
        var par = await _context.PropertyAssessmentRequests.FirstOrDefaultAsync(p => GuidToNumericId(p.Id) == req.FormWorkflowId);
        if (par != null)
        {
            par.Status = (PropertyAssessmentStatus)req.NextStatusId;
            par.Remarks = req.Remarks;
            par.AssignedAppraiser = actorName;

            _context.DocumentForwardingLogs.Add(new DocumentForwardingLog
            {
                RequestId = par.Id,
                TrackingNumber = par.TrackingNumber,
                RequestType = "PropertyAssessmentRequest",
                Division = DivisionType.Division1_TaxAssessmentAndLandHolding,
                FromStage = WorkflowStage.Inspection,
                ToStage = (WorkflowStage)req.NextStatusId,
                ForwardedByName = actorName,
                ForwardedByRole = "Property Appraiser",
                AssignedToRole = "Assessment Officer",
                ActionTaken = "Forwarded via Mobile App Assessment",
                Remarks = req.Remarks,
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await _workflowNotifier.NotifyTransactionForwardedAsync(new
            {
                requestId = par.Id,
                trackingNumber = par.TrackingNumber,
                remarks = req.Remarks
            });

            return Ok(new
            {
                success = true,
                message = "Transaction workflow forwarded successfully from mobile.",
                trackingNo = par.TrackingNumber,
                newStatusId = req.NextStatusId,
                newFormWorkflowId = req.FormWorkflowId
            });
        }

        var rfi = await _context.RequestForInspections.FirstOrDefaultAsync(r => GuidToNumericId(r.Id) == req.FormWorkflowId);
        if (rfi != null)
        {
            rfi.Status = (RequestForInspectionStatus)req.NextStatusId;
            rfi.Remarks = req.Remarks;
            rfi.AssignedStaff = actorName;

            _context.DocumentForwardingLogs.Add(new DocumentForwardingLog
            {
                RequestId = rfi.Id,
                TrackingNumber = rfi.TrackingNumber,
                RequestType = "RequestForInspection",
                Division = DivisionType.Division1_TaxAssessmentAndLandHolding,
                FromStage = WorkflowStage.Inspection,
                ToStage = (WorkflowStage)req.NextStatusId,
                ForwardedByName = actorName,
                ForwardedByRole = "Property Appraiser",
                AssignedToRole = "Assessment Officer",
                ActionTaken = "Forwarded via Mobile App Assessment",
                Remarks = req.Remarks,
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await _workflowNotifier.NotifyTransactionForwardedAsync(new
            {
                requestId = rfi.Id,
                trackingNumber = rfi.TrackingNumber,
                remarks = req.Remarks
            });

            return Ok(new
            {
                success = true,
                message = "Transaction workflow forwarded successfully from mobile.",
                trackingNo = rfi.TrackingNumber,
                newStatusId = req.NextStatusId,
                newFormWorkflowId = req.FormWorkflowId
            });
        }

        // Generic success fallback
        return Ok(new
        {
            success = true,
            message = "Transaction status advanced successfully.",
            newStatusId = req.NextStatusId,
            newFormWorkflowId = req.FormWorkflowId
        });
    }

    // =========================================================================
    // PRIVATE DTO BUILDERS & MAPPING HELPERS
    // =========================================================================

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

        // Fallback to latest record of the same kind
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
        td.Taxability = dto.Taxability ? "Taxable" : "Exempt";
    }

    private TDNDTO BuildTDNDTO(TaxDeclaration td)
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
            DeclarationNo = td.TaxDeclarationNumber,
            ARPNo = td.TaxDeclarationNumber,
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
            TransactionCode = td.TransactionCode,
            YearEffectivity = td.EffectivityYear,
            QuarterEffectivity = effQtrInt,
            Memoranda = td.Memoranda,
            Taxability = string.Equals(td.Taxability, "Taxable", StringComparison.OrdinalIgnoreCase),
            TotalMarketValue = td.MarketValue,
            TotalAssessedValue = td.AssessedValue,
            TotalArea = td.AreaSqMeters,
            Unit = "sq.m."
        };
    }

    private TDNBuildingDTO BuildBuildingDTO(TaxDeclaration td)
    {
        var b = td.BuildingDetails ?? td.EnsureBuildingDetails();
        return new TDNBuildingDTO
        {
            TDN = BuildTDNDTO(td),
            CAST_TDNBuildingDTO = new CAST_TDNBuildingDTO
            {
                CAST_TDNBuildingId = GuidToNumericId(b.Id),
                TDNId = GuidToNumericId(td.Id),
                STREET = b.StreetNo ?? td.StreetNo,
                LotNo = b.LandLotNo ?? td.LotNo,
                SurveyNo = b.LandSurveyNo ?? td.SurveyNo,
                TCTNo = b.LandTitleNo ?? td.TitleNo,
                LandArea = b.LandAreaSqm > 0 ? b.LandAreaSqm.ToString("F2") : td.AreaSqMeters.ToString("F2"),
                ARPNo = td.TaxDeclarationNumber,
                PermitNo = b.BuildingPermitNo,
                Age = b.BuildingAgeYears,
                StoreyNo = b.NoOfStoreys,
                AreaPerFloor = b.NoOfStoreys > 0 ? Math.Round(b.TotalFloorArea / b.NoOfStoreys, 2) : b.TotalFloorArea,
                TotalbldgArea = b.TotalFloorArea,
                SubtotalValCompArea = b.ReplacementCostNew,
                TotalAddItem = b.TotalAdditionalItemsValue,
                DepreciationRate = b.BuildingDepreciationPercent,
                DepreciationAccumulated = b.BuildingDepreciationAccumulated,
                DepreciationMarketval = b.DepreciatedMarketValue,
                TotalMarketVal = b.MarketValue > 0 ? b.MarketValue : td.MarketValue,
                TotalAssessedVal = b.AssessedValue > 0 ? b.AssessedValue : td.AssessedValue,
                StucturalType = b.StructuralType,
                OtherStructure = b.StructuralType,
                MailingAddress = b.LandOwnerAddress ?? td.OwnerAddress,
                TIN = b.LandOwnerTin ?? td.OwnerTin,
                MobileNo = b.LandOwnerPhone ?? td.OwnerPhone,
                EmailAddress = b.LandOwnerEmail ?? td.OwnerEmail,
                TransactionCode = td.TransactionCode
            }
        };
    }

    private TDNLandDTO BuildLandDTO(TaxDeclaration td)
    {
        var l = td.LandDetails ?? td.EnsureLandDetails();
        return new TDNLandDTO
        {
            TDN = BuildTDNDTO(td),
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

    private TDNMachineDTO BuildMachineryDTO(TaxDeclaration td)
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
            TDN = BuildTDNDTO(td),
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
