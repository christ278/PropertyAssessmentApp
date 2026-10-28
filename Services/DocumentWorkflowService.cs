using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class DocumentWorkflowService : IDocumentWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly ITaxDeclarationRequestService _tdService;
    private readonly ILandHoldingRequestService _lhService;
    private readonly ITaxMappingService _tmService;

    public DocumentWorkflowService(
        ApplicationDbContext context,
        ITaxDeclarationRequestService tdService,
        ILandHoldingRequestService lhService,
        ITaxMappingService tmService)
    {
        _context = context;
        _tdService = tdService;
        _lhService = lhService;
        _tmService = tmService;

        EnsureSeedSettings();
    }

    private void EnsureSeedSettings()
    {
        if (!_context.WorkflowDivisionSettings.Any(s => s.Division == DivisionType.Division1_TaxAssessmentAndLandHolding))
        {
            _context.WorkflowDivisionSettings.Add(new WorkflowDivisionSetting
            {
                Division = DivisionType.Division1_TaxAssessmentAndLandHolding,
                DivisionName = "Division 1: Real Property Appraisal & Tax Declaration Division",
                Description = "Encodes Tax Declarations, Land Holding Certifications, Records Verification, and Appraisals.",
                RequireAppraiserReview = true,
                RequireMapperVerification = true,
                RequireHeadAssessorApproval = true,
                SlaHoursTarget = 48
            });
        }

        if (!_context.WorkflowDivisionSettings.Any(s => s.Division == DivisionType.Division2_GIS_TaxMapping))
        {
            _context.WorkflowDivisionSettings.Add(new WorkflowDivisionSetting
            {
                Division = DivisionType.Division2_GIS_TaxMapping,
                DivisionName = "Division 2: GIS Spatial & Tax Mapping Division",
                Description = "Handles Parcel Subdivisions, Consolidations, Section Maps, RROW Clearances, and Map Images.",
                RequireAppraiserReview = false,
                RequireMapperVerification = true,
                RequireHeadAssessorApproval = true,
                SlaHoursTarget = 72
            });
        }

        // Seed Division 1 Stages if empty
        if (!_context.WorkflowStageSettings.Any(s => s.Division == DivisionType.Division1_TaxAssessmentAndLandHolding))
        {
            _context.WorkflowStageSettings.AddRange(new[]
            {
                new WorkflowStageSetting { Division = DivisionType.Division1_TaxAssessmentAndLandHolding, StepOrder = 1, StageName = "Receiving & Document Intake", AssignedRole = "Assessment Clerk", Description = "Initial document intake, fee validation, and requirements verification." },
                new WorkflowStageSetting { Division = DivisionType.Division1_TaxAssessmentAndLandHolding, StepOrder = 2, StageName = "Appraisal & Valuation Review", AssignedRole = "Property Appraiser", Description = "Appraisal of structural materials, machinery, and market valuation computing." },
                new WorkflowStageSetting { Division = DivisionType.Division1_TaxAssessmentAndLandHolding, StepOrder = 3, StageName = "Records Verification", AssignedRole = "Taxation Officer", Description = "Cross-verification against master assessment roll and property index." },
                new WorkflowStageSetting { Division = DivisionType.Division1_TaxAssessmentAndLandHolding, StepOrder = 4, StageName = "City Assessor Final Approval", AssignedRole = "City Assessor Head", Description = "Final signature, official seal, and tax declaration issuance.", IsApprovalStage = true }
            });
        }

        // Seed Division 2 (Tax Mapping) Stages if empty (including Replotting!)
        if (!_context.WorkflowStageSettings.Any(s => s.Division == DivisionType.Division2_GIS_TaxMapping))
        {
            _context.WorkflowStageSettings.AddRange(new[]
            {
                new WorkflowStageSetting { Division = DivisionType.Division2_GIS_TaxMapping, StepOrder = 1, StageName = "Receiving & Request Intake", AssignedRole = "Assessment Clerk", Description = "Intake of parcel subdivision, consolidation, or boundary survey request." },
                new WorkflowStageSetting { Division = DivisionType.Division2_GIS_TaxMapping, StepOrder = 2, StageName = "Field Survey & Boundary Verification", AssignedRole = "Tax Mapper", Description = "Field verification, boundary identification, and lot checking." },
                new WorkflowStageSetting { Division = DivisionType.Division2_GIS_TaxMapping, StepOrder = 3, StageName = "Replotting & GIS Drafting", AssignedRole = "GIS Specialist / Replotter", Description = "Digital map replotting, GIS layer update, and blueprint drafting." },
                new WorkflowStageSetting { Division = DivisionType.Division2_GIS_TaxMapping, StepOrder = 4, StageName = "Technical Plan Review", AssignedRole = "Senior Tax Mapper", Description = "Quality control and map section accuracy validation." },
                new WorkflowStageSetting { Division = DivisionType.Division2_GIS_TaxMapping, StepOrder = 5, StageName = "City Assessor Final Approval", AssignedRole = "City Assessor Head", Description = "Final approval of section map, Tax Mapping certificate release.", IsApprovalStage = true }
            });
        }

        _context.SaveChanges();
    }

    public IEnumerable<WorkflowStageSetting> GetStagesForDivision(DivisionType division)
    {
        return _context.WorkflowStageSettings
            .Where(s => s.Division == division && s.IsActive)
            .OrderBy(s => s.StepOrder)
            .ToList();
    }

    public WorkflowStageSetting AddStage(DivisionType division, string stageName, string assignedRole, string? description, bool isApprovalStage)
    {
        var existingStages = _context.WorkflowStageSettings
            .Where(s => s.Division == division)
            .ToList();

        int nextOrder = existingStages.Any() ? existingStages.Max(s => s.StepOrder) + 1 : 1;

        var newStage = new WorkflowStageSetting
        {
            Division = division,
            StepOrder = nextOrder,
            StageName = stageName,
            AssignedRole = assignedRole,
            Description = description,
            IsApprovalStage = isApprovalStage,
            IsActive = true
        };

        _context.WorkflowStageSettings.Add(newStage);
        _context.SaveChanges();

        return newStage;
    }

    public void UpdateStage(WorkflowStageSetting stage)
    {
        var item = _context.WorkflowStageSettings.FirstOrDefault(s => s.Id == stage.Id);
        if (item != null)
        {
            item.StageName = stage.StageName;
            item.AssignedRole = stage.AssignedRole;
            item.Description = stage.Description;
            item.IsApprovalStage = stage.IsApprovalStage;
            item.IsActive = stage.IsActive;

            _context.SaveChanges();
        }
    }

    public void DeleteStage(Guid stageId)
    {
        var item = _context.WorkflowStageSettings.FirstOrDefault(s => s.Id == stageId);
        if (item != null)
        {
            var division = item.Division;
            _context.WorkflowStageSettings.Remove(item);
            _context.SaveChanges();

            // Reorder remaining steps sequentially
            var remaining = _context.WorkflowStageSettings
                .Where(s => s.Division == division)
                .OrderBy(s => s.StepOrder)
                .ToList();

            for (int i = 0; i < remaining.Count; i++)
            {
                remaining[i].StepOrder = i + 1;
            }
            _context.SaveChanges();
        }
    }

    public void MoveStage(Guid stageId, bool moveUp)
    {
        var item = _context.WorkflowStageSettings.FirstOrDefault(s => s.Id == stageId);
        if (item == null) return;

        var stages = _context.WorkflowStageSettings
            .Where(s => s.Division == item.Division)
            .OrderBy(s => s.StepOrder)
            .ToList();

        int index = stages.FindIndex(s => s.Id == stageId);
        if (index == -1) return;

        if (moveUp && index > 0)
        {
            // Swap with previous
            var prev = stages[index - 1];
            int tempOrder = item.StepOrder;
            item.StepOrder = prev.StepOrder;
            prev.StepOrder = tempOrder;
            _context.SaveChanges();
        }
        else if (!moveUp && index < stages.Count - 1)
        {
            // Swap with next
            var next = stages[index + 1];
            int tempOrder = item.StepOrder;
            item.StepOrder = next.StepOrder;
            next.StepOrder = tempOrder;
            _context.SaveChanges();
        }
    }

    public IEnumerable<DocumentForwardingLog> GetForwardingLogs(Guid requestId)
    {
        return _context.DocumentForwardingLogs
            .Where(l => l.RequestId == requestId)
            .OrderByDescending(l => l.Timestamp)
            .ToList();
    }

    public IEnumerable<DocumentForwardingLog> GetDivisionLogs(DivisionType division)
    {
        return _context.DocumentForwardingLogs
            .Where(l => l.Division == division)
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .ToList();
    }

    public static string FormatStatusDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "checkAndReview", "Check and Review" },
            { "Reploting", "Replotting" },
            { "RecommendingAndApproved", "Recommending Approval" },
            { "ValuationAndExamination", "Valuation & Examination" },
            { "Verify", "Verification" },
            { "Release", "Releasing" }
        };
        if (map.TryGetValue(name, out var clean)) return clean;
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2").Trim();
    }

    private string GetStageDisplayName(WorkflowStage stage)
    {
        int stageId = (int)stage;
        var status = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == stageId);
        if (status != null)
        {
            return FormatStatusDisplayName(status.Name);
        }
        return stage switch
        {
            WorkflowStage.ClerkReceiving => "Receiving",
            WorkflowStage.AppraiserReview => "Appraiser Review",
            WorkflowStage.MapperVerification => "GIS Verification",
            WorkflowStage.AssessorApproval => "Approval",
            WorkflowStage.Completed => "Completed / Issued",
            WorkflowStage.Returned => "Returned",
            _ => $"Stage {stageId}"
        };
    }

    public IEnumerable<NextWorkflowStageDto> GetNextStagesForRequest(Guid requestId, string requestType, Guid? currentUserId = null)
    {
        // 1. Resolve TransactionTypeId: 1=Inspection, 2=PropertyAssessment, 3=TaxMapping, 4=TaxDeclaration, 5=LandHolding
        int transactionTypeId = (requestType ?? "").ToLower() switch
        {
            "requestforinspection" or "inspection" or "rfi" => 1,
            "propertyassessmentrequest" or "propertyassessment" or "rpa" => 2,
            "taxmappingrequest" or "taxmapping" or "tm" => 3,
            "taxdeclarationrequest" or "taxdeclaration" or "td" => 4,
            "landholdingrequest" or "landholding" or "lh" => 5,
            _ => 1
        };

        // 2. Identify default initial receiving status per division
        int defaultInitialStatus = transactionTypeId switch
        {
            1 => 1,  // Receiving (Appraisal)
            2 => 1,  // Receiving (Appraisal)
            3 => 15, // Receiving (GIS)
            4 => 25, // Receiving (Records)
            5 => 25, // Receiving (Records)
            _ => 1
        };

        // 3. Find latest forwarding log for this request
        var latestLog = _context.DocumentForwardingLogs
            .Where(l => l.RequestId == requestId)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefault();

        int currentStatusId = defaultInitialStatus;
        if (latestLog != null && (int)latestLog.ToStage > 0)
        {
            currentStatusId = (int)latestLog.ToStage;
        }

        // If current user is logged in with an assigned workflow role matching the workflow sequence, use it
        if (currentUserId.HasValue)
        {
            var userAcc = _context.UserAccounts.FirstOrDefault(u => u.Id == currentUserId.Value);
            if (userAcc != null && userAcc.WorkflowRoleId.HasValue && userAcc.WorkflowRoleId.Value > 0)
            {
                bool hasConfiguredSequence = _context.WorkflowSequences.Any(w => w.TransactionTypeId == transactionTypeId && w.CurrentStatusId == userAcc.WorkflowRoleId.Value);
                if (hasConfiguredSequence && (latestLog == null || (int)latestLog.ToStage == defaultInitialStatus || (int)latestLog.ToStage == userAcc.WorkflowRoleId.Value))
                {
                    currentStatusId = userAcc.WorkflowRoleId.Value;
                }
            }
        }

        // 4. Query WorkflowSequence table configured in Transaction Workflow Manager
        var sequences = _context.WorkflowSequences
            .Where(w => w.TransactionTypeId == transactionTypeId && w.CurrentStatusId == currentStatusId)
            .OrderBy(w => w.SequenceId)
            .ToList();

        var allStatuses = _context.TransactionStatuses.ToList();
        var result = new List<NextWorkflowStageDto>();

        foreach (var seq in sequences)
        {
            if (seq.NextStatusId.HasValue && seq.NextStatusId.Value > 0)
            {
                var st = allStatuses.FirstOrDefault(s => s.TransactionStatusId == seq.NextStatusId.Value);
                string cleanName = st != null ? FormatStatusDisplayName(st.Name) : $"Status {seq.NextStatusId.Value}";
                if (st != null && !cleanName.Contains("GIS", StringComparison.OrdinalIgnoreCase) && !cleanName.Contains("Records", StringComparison.OrdinalIgnoreCase))
                {
                    if (st.DivisionId == 2) cleanName += " (GIS)";
                    else if (st.DivisionId == 3) cleanName += " (Records)";
                }
                if (seq.IsApproval)
                {
                    cleanName += " [Approval Gate]";
                }

                result.Add(new NextWorkflowStageDto
                {
                    StatusId = seq.NextStatusId.Value,
                    StatusName = cleanName,
                    IsApproval = seq.IsApproval,
                    IsComplete = st?.IsComplete ?? false
                });
            }
            else
            {
                // Terminal step configured with NextStatusId == null
                result.Add(new NextWorkflowStageDto
                {
                    StatusId = (int)WorkflowStage.Completed,
                    StatusName = "Completed & Issued (Final Release)",
                    IsApproval = false,
                    IsComplete = true
                });
            }
        }

        // 5. Fallback if sequence not configured
        if (!result.Any())
        {
            bool isReleasingStatus = (currentStatusId == 10 || currentStatusId == 19 || currentStatusId == 30);
            if (isReleasingStatus)
            {
                result.Add(new NextWorkflowStageDto
                {
                    StatusId = (int)WorkflowStage.Completed,
                    StatusName = "Completed & Issued (Final Release)",
                    IsApproval = false,
                    IsComplete = true
                });
            }
            else
            {
                int nextId = currentStatusId + 1;
                var st = allStatuses.FirstOrDefault(s => s.TransactionStatusId == nextId);
                if (st != null)
                {
                    result.Add(new NextWorkflowStageDto
                    {
                        StatusId = st.TransactionStatusId,
                        StatusName = FormatStatusDisplayName(st.Name),
                        IsApproval = false,
                        IsComplete = st.IsComplete
                    });
                }
                else
                {
                    result.Add(new NextWorkflowStageDto
                    {
                        StatusId = (int)WorkflowStage.Completed,
                        StatusName = "Completed & Issued (Final Release)",
                        IsApproval = false,
                        IsComplete = true
                    });
                }
            }
        }

        return result;
    }

    public DocumentForwardingLog ForwardDocument(
        Guid requestId,
        string requestType,
        DivisionType division,
        WorkflowStage targetStage,
        string forwardedByName,
        string role,
        string remarks,
        string actionTaken = "Forwarded",
        string? orNumber = null,
        DateTime? orDateIssued = null)
    {
        var logs = GetForwardingLogs(requestId);
        var currentStage = logs.FirstOrDefault()?.ToStage ?? WorkflowStage.ClerkReceiving;
        var trackingNo = "TRK-" + requestId.ToString().Substring(0, 8);

        int targetStatusId = (int)targetStage;
        var targetStatus = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == targetStatusId);
        var roleSetting = _context.UserRoleSettings.FirstOrDefault(r => r.TransactionStatusId == targetStatusId);

        bool isCompleted = targetStage == WorkflowStage.Completed 
            || targetStatusId == (int) RequestForInspectionStatus.Printing
            || targetStatus?.IsComplete == true 
            || (actionTaken == "Released" && (targetStatusId == (int) RequestForInspectionStatus.Releasing
            || targetStatusId == (int) TaxMappingRequestStatus.Released
            || targetStatusId == (int) TaxDeclarationRequestStatus.Released));
        bool isDeferred = (actionTaken == "Returned" || targetStage == WorkflowStage.Returned || actionTaken == "Deferred");

        if (requestType == "TaxDeclarationRequest")
        {
            var item = _tdService.GetById(requestId);
            if (item != null)
            {
                trackingNo = item.TrackingNumber;
                item.UpdatedDate = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(orNumber)) item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue) item.ORDateIssued = orDateIssued.Value;
                if (isCompleted)
                    item.Status = TaxDeclarationRequestStatus.Approved;
                else if (isDeferred)
                    item.Status = TaxDeclarationRequestStatus.Rejected;
                else
                    item.Status = TaxDeclarationRequestStatus.Processing;
                
                _tdService.Update(item, null, "");
            }
        }
        else if (requestType == "LandHoldingRequest")
        {
            var item = _lhService.GetById(requestId);
            if (item != null)
            {
                trackingNo = item.TrackingNumber;
                item.UpdatedDate = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(orNumber)) item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue) item.ORDateIssued = orDateIssued.Value;
                if (isCompleted)
                    item.Status = LandHoldingRequestStatus.Approved;
                else if (isDeferred)
                    item.Status = LandHoldingRequestStatus.Rejected;
                else
                    item.Status = LandHoldingRequestStatus.Processing;

                _lhService.Update(item, null, "");
            }
        }
        else if (requestType == "TaxMappingRequest")
        {
            var item = _tmService.GetById(requestId);
            if (item != null)
            {
                trackingNo = item.RequestNumber;
                item.UpdatedDate = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(orNumber)) item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue) item.ORDateIssued = orDateIssued.Value;
                if (isCompleted)
                    item.Status = TaxMappingRequestStatus.Approved;
                else if (isDeferred)
                    item.Status = TaxMappingRequestStatus.Rejected;
                else
                    item.Status = TaxMappingRequestStatus.UnderReview;

                _tmService.Update(item, null, "");
            }
        }
        else if (requestType == "RequestForInspection")
        {
            var item = _context.RequestForInspections.FirstOrDefault(r => r.Id == requestId);
            if (item != null)
            {
                trackingNo = item.TrackingNumber;
                item.UpdatedDate = DateTime.Now;
                if (isCompleted)
                    item.Status = RequestForInspectionStatus.Approval;
                else if (isDeferred)
                    item.Status = RequestForInspectionStatus.Deferred;
                else if (Enum.IsDefined(typeof(RequestForInspectionStatus), targetStatusId))
                    item.Status = (RequestForInspectionStatus)targetStatusId;

                _context.SaveChanges();
            }
        }
        else if (requestType == "PropertyAssessmentRequest")
        {
            var item = _context.PropertyAssessmentRequests.FirstOrDefault(r => r.Id == requestId);
            if (item != null)
            {
                if (!string.IsNullOrWhiteSpace(orNumber)) item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue) item.ORDateIssued = orDateIssued.Value;
                trackingNo = item.TrackingNumber;
                item.UpdatedDate = DateTime.Now;
                if (isCompleted)
                    item.Status = PropertyAssessmentStatus.Approved;
                else if (isDeferred)
                    item.Status = PropertyAssessmentStatus.Deferred;

                SyncPropertyAssessmentPartitionTaxDeclarations(item);
                _context.SaveChanges();
            }
        }

        string destRole = roleSetting != null && !string.IsNullOrWhiteSpace(roleSetting.RoleName)
            ? roleSetting.RoleName
            : targetStatus != null 
                ? FormatStatusDisplayName(targetStatus.Name) 
                : targetStage switch
                {
                    WorkflowStage.ClerkReceiving => "Assessment Clerk",
                    WorkflowStage.AppraiserReview => "Property Appraiser",
                    WorkflowStage.MapperVerification => "Tax Mapper / Replotter",
                    WorkflowStage.AssessorApproval => "City Assessor Head",
                    WorkflowStage.Completed => "Records / Vault Officer",
                    _ => "Receiving Desk"
                };

        var log = new DocumentForwardingLog
        {
            RequestId = requestId,
            RequestType = requestType,
            TrackingNumber = trackingNo,
            Division = division,
            FromStage = currentStage,
            ToStage = targetStage,
            ForwardedByName = string.IsNullOrWhiteSpace(forwardedByName) ? "System User" : forwardedByName,
            ForwardedByRole = string.IsNullOrWhiteSpace(role) ? "LGU Officer" : role,
            AssignedToRole = destRole,
            ActionTaken = actionTaken,
            Remarks = remarks,
            AssignedUserId = null,
            AssignedUserName = null,
            IsClaimed = false,
            IsDeferred = isDeferred,
            Timestamp = DateTime.Now
        };

        _context.DocumentForwardingLogs.Add(log);
        _context.SaveChanges();

        return log;
    }

    public bool ClaimDocument(Guid requestId, string requestType, Guid userId, string userName)
    {
        // Optimistic / Atomic Lock check: fetch latest log for document
        var latestLog = _context.DocumentForwardingLogs
            .Where(l => l.RequestId == requestId)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefault();

        if (latestLog == null)
        {
            // Auto-create initial receiving log and claim it immediately (CAST Pool -> Checkout logic)
            var division = (requestType == "TaxMappingRequest") 
                ? DivisionType.Division2_GIS_TaxMapping 
                : (requestType == "LandHoldingRequest" ? DivisionType.Division3_RecordsAndCertification : DivisionType.Division1_TaxAssessmentAndLandHolding);

            string trackingNo = "TRK-" + requestId.ToString().Substring(0, 8);
            if (requestType == "TaxDeclarationRequest")
            {
                var td = _tdService.GetById(requestId);
                if (td != null) trackingNo = td.TrackingNumber;
            }
            else if (requestType == "LandHoldingRequest")
            {
                var lh = _lhService.GetById(requestId);
                if (lh != null) trackingNo = lh.TrackingNumber;
            }
            else if (requestType == "TaxMappingRequest")
            {
                var tm = _tmService.GetById(requestId);
                if (tm != null) trackingNo = tm.RequestNumber;
            }
            else if (requestType == "RequestForInspection")
            {
                var rfi = _context.RequestForInspections.FirstOrDefault(r => r.Id == requestId);
                if (rfi != null) trackingNo = rfi.TrackingNumber;
            }
            else if (requestType == "PropertyAssessmentRequest")
            {
                var rpa = _context.PropertyAssessmentRequests.FirstOrDefault(r => r.Id == requestId);
                if (rpa != null) trackingNo = rpa.TrackingNumber;
            }

            int initialStatus = division switch
            {
                DivisionType.Division2_GIS_TaxMapping => 15,
                DivisionType.Division3_RecordsAndCertification => 25,
                _ => 1
            };
            string initialRole = division switch
            {
                DivisionType.Division2_GIS_TaxMapping => "Receiving (GIS)",
                DivisionType.Division3_RecordsAndCertification => "Receiving (Records)",
                _ => "Receiving"
            };

            latestLog = new DocumentForwardingLog
            {
                RequestId = requestId,
                RequestType = requestType,
                TrackingNumber = trackingNo,
                Division = division,
                FromStage = (WorkflowStage)initialStatus,
                ToStage = (WorkflowStage)initialStatus,
                ForwardedByName = "System Intake",
                ForwardedByRole = initialRole,
                AssignedToRole = initialRole,
                ActionTaken = "Checked Out from Pool",
                Remarks = "Claimed from transaction pool into personal processing queue.",
                AssignedUserId = userId,
                AssignedUserName = userName,
                IsClaimed = true,
                Timestamp = DateTime.Now
            };

            _context.DocumentForwardingLogs.Add(latestLog);
            _context.SaveChanges();
            return true;
        }

        // Concurrency lock check: if already claimed by someone else, reject claim!
        if (latestLog.IsClaimed && latestLog.AssignedUserId.HasValue && latestLog.AssignedUserId.Value != userId)
        {
            return false; // Prevent race condition duplicate claim
        }

        latestLog.AssignedUserId = userId;
        latestLog.AssignedUserName = userName;
        latestLog.IsClaimed = true;
        _context.SaveChanges();

        return true;
    }

    public bool ReleaseDocumentToPool(Guid requestId, string requestType)
    {
        var latestLog = _context.DocumentForwardingLogs
            .Where(l => l.RequestId == requestId)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefault();

        if (latestLog == null) return false;

        latestLog.AssignedUserId = null;
        latestLog.AssignedUserName = null;
        latestLog.IsClaimed = false;
        _context.SaveChanges();

        return true;
    }

    public bool DeferDocument(Guid requestId, string requestType, DivisionType division, Guid userId, string userName, string userRole, string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            throw new ArgumentException("Remarks / Reason for deferral is mandatory when returning a document.");
        }

        var logs = GetForwardingLogs(requestId).ToList();
        var currentStage = logs.FirstOrDefault()?.ToStage ?? WorkflowStage.ClerkReceiving;
        var trackingNo = "TRK-" + requestId.ToString().Substring(0, 8);

        if (requestType == "TaxDeclarationRequest")
        {
            var item = _tdService.GetById(requestId);
            if (item != null)
            {
                trackingNo = item.TrackingNumber;
                item.UpdatedDate = DateTime.Now;
                _tdService.Update(item, null, "");
            }
        }
        else if (requestType == "LandHoldingRequest")
        {
            var item = _lhService.GetById(requestId);
            if (item != null)
            {
                trackingNo = item.TrackingNumber;
                item.UpdatedDate = DateTime.Now;
                _lhService.Update(item, null, "");
            }
        }
        else if (requestType == "TaxMappingRequest")
        {
            var item = _tmService.GetById(requestId);
            if (item != null)
            {
                trackingNo = item.RequestNumber;
                item.UpdatedDate = DateTime.Now;
                _tmService.Update(item, null, "");
            }
        }

        // When deferred, transaction STAYS in user's personal My Transactions workspace with IsDeferred = true
        var log = new DocumentForwardingLog
        {
            RequestId = requestId,
            RequestType = requestType,
            TrackingNumber = trackingNo,
            Division = division,
            FromStage = currentStage,
            ToStage = currentStage,
            ForwardedByName = string.IsNullOrWhiteSpace(userName) ? "System User" : userName,
            ForwardedByRole = string.IsNullOrWhiteSpace(userRole) ? "LGU Officer" : userRole,
            AssignedToRole = userRole,
            ActionTaken = "Deferred",
            Remarks = remarks,
            AssignedUserId = userId,
            AssignedUserName = userName,
            IsClaimed = true,
            IsDeferred = true,
            Timestamp = DateTime.Now
        };

        _context.DocumentForwardingLogs.Add(log);
        _context.SaveChanges();

        return true;
    }

    public IEnumerable<WorkflowQueueItemDto> GetQueueForDivision(DivisionType division, WorkflowStage? stageFilter = null, string? search = null)
    {
        var items = BuildQueueItems(division);

        var query = items.AsEnumerable();

        if (stageFilter.HasValue)
        {
            query = query.Where(i => i.CurrentStage == stageFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(i => i.TrackingNumber.ToLower().Contains(term)
                                  || i.OwnerOrApplicant.ToLower().Contains(term)
                                  || i.ReferenceNumber.ToLower().Contains(term));
        }

        return query.OrderByDescending(i => i.DateSubmitted);
    }

    public IEnumerable<WorkflowQueueItemDto> GetPoolQueueForDivision(DivisionType division, WorkflowStage? stageFilter = null, string? search = null)
    {
        var items = BuildQueueItems(division)
            .Where(i => !i.IsClaimed && i.CurrentStage != WorkflowStage.Completed);

        var query = items.AsEnumerable();

        if (stageFilter.HasValue)
        {
            query = query.Where(i => i.CurrentStage == stageFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(i => i.TrackingNumber.ToLower().Contains(term)
                                  || i.OwnerOrApplicant.ToLower().Contains(term)
                                  || i.ReferenceNumber.ToLower().Contains(term));
        }

        return query.OrderByDescending(i => i.DateSubmitted);
    }

    public IEnumerable<WorkflowQueueItemDto> GetPersonalQueueForUser(Guid userId, DivisionType? divisionFilter = null, string? search = null)
    {
        var items = new List<WorkflowQueueItemDto>();
        items.AddRange(BuildQueueItems(DivisionType.Division1_TaxAssessmentAndLandHolding));
        items.AddRange(BuildQueueItems(DivisionType.Division2_GIS_TaxMapping));

        var query = items.Where(i => i.IsClaimed && i.AssignedUserId == userId && i.CurrentStage != WorkflowStage.Completed);

        if (divisionFilter.HasValue)
        {
            query = query.Where(i => i.Division == divisionFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(i => i.TrackingNumber.ToLower().Contains(term)
                                  || i.OwnerOrApplicant.ToLower().Contains(term)
                                  || i.ReferenceNumber.ToLower().Contains(term));
        }

        return query.OrderByDescending(i => i.DateSubmitted);
    }

    public IEnumerable<WorkflowQueueItemDto> GetUnifiedQueue(
        int? divisionId = null,
        string? mode = "pool",
        WorkflowStage? stageFilter = null,
        string? search = null,
        string? requestType = null,
        Guid? userId = null,
        int? userWorkflowRoleId = null,
        string? userWorkflowRoleName = null,
        bool isSuperAdminOrHead = false)
    {
        var allItems = new List<WorkflowQueueItemDto>();

        bool isReleasingOfficer = (userWorkflowRoleId.HasValue && (userWorkflowRoleId.Value == 10 || userWorkflowRoleId.Value == 19 || userWorkflowRoleId.Value == 30))
            || (!string.IsNullOrEmpty(userWorkflowRoleName) && userWorkflowRoleName.Contains("Releas", StringComparison.OrdinalIgnoreCase));

        // In releasing, disregard division so that all transactions ready for releasing across all divisions appear in pool
        if (isReleasingOfficer && (!divisionId.HasValue || divisionId.Value <= 0))
        {
            allItems.AddRange(BuildQueueItems(DivisionType.Division1_TaxAssessmentAndLandHolding));
            allItems.AddRange(BuildQueueItems(DivisionType.Division2_GIS_TaxMapping));
            allItems.AddRange(BuildDivision3Items());
        }
        else
        {
            if (!divisionId.HasValue || divisionId.Value == 1)
            {
                allItems.AddRange(BuildQueueItems(DivisionType.Division1_TaxAssessmentAndLandHolding));
            }

            if (!divisionId.HasValue || divisionId.Value == 2)
            {
                allItems.AddRange(BuildQueueItems(DivisionType.Division2_GIS_TaxMapping));
            }

            if (!divisionId.HasValue || divisionId.Value == 3)
            {
                allItems.AddRange(BuildDivision3Items());
            }
        }

        var query = allItems.AsEnumerable();
        var modeStr = string.IsNullOrWhiteSpace(mode) ? "pool" : mode.ToLower();

        if (modeStr == "pool")
        {
            query = query.Where(i => !i.IsClaimed && i.CurrentStage != WorkflowStage.Completed);

            // In releasing, disregard division and stage ID differences:
            // All transactions ready for releasing across all divisions appear in pool
            if (isReleasingOfficer)
            {
                query = query.Where(i => 
                    (int)i.CurrentStage == 10 || (int)i.CurrentStage == 19 || (int)i.CurrentStage == 30 
                    || (i.CurrentStageName != null && i.CurrentStageName.Contains("Releas", StringComparison.OrdinalIgnoreCase))
                    || (i.AssignedToRole != null && i.AssignedToRole.Contains("Releas", StringComparison.OrdinalIgnoreCase)));
            }
            // Restrict pool transactions to the user's assigned workflow stage/station
            // If the user is NOT super admin or assessor head, they must only see transactions currently at their station!
            else if (!isSuperAdminOrHead && userWorkflowRoleId.HasValue && userWorkflowRoleId.Value > 0)
            {
                query = query.Where(i => 
                    (int)i.CurrentStage == userWorkflowRoleId.Value 
                    || (!string.IsNullOrEmpty(userWorkflowRoleName) && i.AssignedToRole.Equals(userWorkflowRoleName, StringComparison.OrdinalIgnoreCase)));
            }
        }
        else if (modeStr == "my")
        {
            if (userId.HasValue)
            {
                query = query.Where(i => i.IsClaimed && i.AssignedUserId == userId.Value && i.CurrentStage != WorkflowStage.Completed);
            }
            else
            {
                query = query.Where(i => i.IsClaimed && i.CurrentStage != WorkflowStage.Completed);
            }
        }
        else if (modeStr == "deferred")
        {
            query = query.Where(i => i.IsDeferred || i.CurrentStage == WorkflowStage.Returned);
        }
        else if (modeStr == "completed")
        {
            query = query.Where(i => i.CurrentStage == WorkflowStage.Completed);
        }

        if (stageFilter.HasValue)
        {
            if (isReleasingOfficer && ((int)stageFilter.Value == 10 || (int)stageFilter.Value == 19 || (int)stageFilter.Value == 30))
            {
                query = query.Where(i => (int)i.CurrentStage == 10 || (int)i.CurrentStage == 19 || (int)i.CurrentStage == 30
                    || (i.CurrentStageName != null && i.CurrentStageName.Contains("Releas", StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                query = query.Where(i => i.CurrentStage == stageFilter.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(requestType) && requestType != "-1" && !requestType.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.RequestType.Equals(requestType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(i => (i.TrackingNumber != null && i.TrackingNumber.ToLower().Contains(term))
                                  || (i.OwnerOrApplicant != null && i.OwnerOrApplicant.ToLower().Contains(term))
                                  || (i.ReferenceNumber != null && i.ReferenceNumber.ToLower().Contains(term)));
        }

        return query.OrderByDescending(i => i.DateSubmitted).ToList();
    }

    private (string statusText, bool isApprovalStage, bool isReleased) ResolveTransactionStatus(
        int transactionTypeId,
        DivisionType division,
        int currentStageId,
        bool isClaimed,
        bool isDeferred,
        bool isExplicitlyCompleted,
        DocumentForwardingLog? latestLog,
        string? baseStatus)
    {
        // 1. Check if the step is marked as Approval Step in Transaction Workflow Manager
        bool isApprovalConfigured = _context.WorkflowSequences.Any(w =>
            (w.TransactionTypeId == transactionTypeId || w.TransactionTypeId == (int)division) &&
            w.CurrentStatusId == currentStageId);

        bool isApprovalStep;
        if (isApprovalConfigured)
        {
            isApprovalStep = _context.WorkflowSequences.Any(w =>
                (w.TransactionTypeId == transactionTypeId || w.TransactionTypeId == (int)division) &&
                w.CurrentStatusId == currentStageId &&
                w.IsApproval);
        }
        else
        {
            var st = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == currentStageId);
            isApprovalStep = st != null && (st.Name.Contains("Approval", StringComparison.OrdinalIgnoreCase) || currentStageId == 6 || currentStageId == 8 || currentStageId == 18 || currentStageId == 29);
        }

        // 2. Check if released (completed or released action)
        bool isReleased = isExplicitlyCompleted
            || currentStageId == (int)WorkflowStage.Completed
            || currentStageId == 999
            || (latestLog != null && latestLog.ActionTaken == "Released" && (currentStageId == 10 || currentStageId == 19 || currentStageId == 30))
            || (baseStatus != null && (baseStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) || baseStatus.Equals("Released", StringComparison.OrdinalIgnoreCase) || baseStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase)));

        // 3. User Rule Hierarchy:
        // - Released: if the transaction is now released
        // - Deferred: if and only if the status is being changed to deferred
        // - For Approval: if marked as Approval Step in Transaction Workflow Manager
        // - On Going: once claimed
        // - Pending: if in pool (unclaimed)
        string status;
        if (isReleased)
        {
            status = "Released";
        }
        else if (isDeferred)
        {
            status = "Deferred";
        }
        else if (isApprovalStep)
        {
            status = "For Approval";
        }
        else if (isClaimed)
        {
            status = "On Going";
        }
        else
        {
            status = "Pending";
        }

        return (status, isApprovalStep, isReleased);
    }

    private List<WorkflowQueueItemDto> BuildDivision3Items()
    {
        var items = new List<WorkflowQueueItemDto>();
        var lhList = _lhService.GetAll();
        foreach (var lh in lhList)
        {
            var logs = GetForwardingLogs(lh.Id).ToList();
            var lastLog = logs.FirstOrDefault();
            var currentStage = lastLog?.ToStage ?? (lh.Status == LandHoldingRequestStatus.Approved ? WorkflowStage.Completed : (WorkflowStage)25);
            int stageId = (int)currentStage;
            bool isClaimed = lastLog?.IsClaimed ?? false;
            bool isDeferred = lastLog?.IsDeferred ?? false;
            bool isDone = lastLog?.ToStage == WorkflowStage.Completed || lh.Status == LandHoldingRequestStatus.Approved;

            var (statusText, isApproval, isReleased) = ResolveTransactionStatus(
                5,
                DivisionType.Division3_RecordsAndCertification,
                stageId,
                isClaimed,
                isDeferred,
                isDone,
                lastLog,
                lh.Status.ToString());

            items.Add(new WorkflowQueueItemDto
            {
                RequestId = lh.Id,
                RequestType = "LandHoldingRequest",
                DisplayType = "Land Holding & Records Certificate",
                TrackingNumber = lh.TrackingNumber,
                OwnerOrApplicant = lh.OwnerName,
                ReferenceNumber = lh.TaxDeclarationNumber ?? lh.TrackingNumber,
                Division = DivisionType.Division3_RecordsAndCertification,
                CurrentStage = isReleased ? WorkflowStage.Completed : currentStage,
                CurrentStageName = GetStageDisplayName(currentStage),
                AssignedToRole = lastLog?.AssignedToRole ?? "Receiving (Records)",
                AssignedUserId = lastLog?.AssignedUserId,
                AssignedUserName = lastLog?.AssignedUserName,
                IsClaimed = isClaimed,
                IsDeferred = isDeferred,
                IsApprovalStage = isApproval,
                StatusText = statusText,
                TotalAmount = lh.TotalFeesAmount,
                DateSubmitted = lh.RequestDate,
                LastForwardedBy = lastLog?.ForwardedByName,
                LastForwardedDate = lastLog?.Timestamp,
                LastRemarks = lastLog?.Remarks,
                ORNumber = lh.ORNumber,
                ORDateIssued = lh.ORDateIssued
            });
        }

        var tdList = _tdService.GetAll();
        foreach (var td in tdList)
        {
            var logs = GetForwardingLogs(td.Id).ToList();
            var lastLog = logs.FirstOrDefault();
            var currentStage = lastLog?.ToStage ?? (td.Status == TaxDeclarationRequestStatus.Approved ? WorkflowStage.Completed : (WorkflowStage)25);
            int stageId = (int)currentStage;
            bool isClaimed = lastLog?.IsClaimed ?? false;
            bool isDeferred = lastLog?.IsDeferred ?? false;
            bool isDone = lastLog?.ToStage == WorkflowStage.Completed || td.Status == TaxDeclarationRequestStatus.Approved;

            var (statusText, isApproval, isReleased) = ResolveTransactionStatus(
                4,
                DivisionType.Division3_RecordsAndCertification,
                stageId,
                isClaimed,
                isDeferred,
                isDone,
                lastLog,
                td.Status.ToString());

            items.Add(new WorkflowQueueItemDto
            {
                RequestId = td.Id,
                RequestType = "TaxDeclarationRequest",
                DisplayType = "Tax Declaration Certified Record",
                TrackingNumber = td.TrackingNumber,
                OwnerOrApplicant = td.OwnerName,
                ReferenceNumber = td.TaxDeclarationNumber ?? td.TrackingNumber,
                Division = DivisionType.Division3_RecordsAndCertification,
                CurrentStage = isReleased ? WorkflowStage.Completed : currentStage,
                CurrentStageName = GetStageDisplayName(currentStage),
                AssignedToRole = lastLog?.AssignedToRole ?? "Receiving (Records)",
                AssignedUserId = lastLog?.AssignedUserId,
                AssignedUserName = lastLog?.AssignedUserName,
                IsClaimed = isClaimed,
                IsDeferred = isDeferred,
                IsApprovalStage = isApproval,
                StatusText = statusText,
                TotalAmount = td.TotalFeesAmount,
                DateSubmitted = td.RequestDate,
                LastForwardedBy = lastLog?.ForwardedByName,
                LastForwardedDate = lastLog?.Timestamp,
                LastRemarks = lastLog?.Remarks,
                ORNumber = td.ORNumber,
                ORDateIssued = td.ORDateIssued
            });
        }
        return items;
    }

    private List<WorkflowQueueItemDto> BuildQueueItems(DivisionType division)
    {
        var items = new List<WorkflowQueueItemDto>();

        if (division == DivisionType.Division1_TaxAssessmentAndLandHolding)
        {
            var rfiList = _context.RequestForInspections.ToList();
            foreach (var rfi in rfiList)
            {
                var logs = GetForwardingLogs(rfi.Id).ToList();
                var lastLog = logs.FirstOrDefault();
                var currentStage = lastLog?.ToStage ?? (rfi.Status == RequestForInspectionStatus.Approval || rfi.Status == RequestForInspectionStatus.Printing ? WorkflowStage.Completed : (WorkflowStage)1);
                int stageId = (int)currentStage;
                bool isClaimed = lastLog?.IsClaimed ?? false;
                bool isDeferred = lastLog?.IsDeferred ?? (rfi.Status == RequestForInspectionStatus.Deferred);
                bool isDone = lastLog?.ToStage == WorkflowStage.Completed || rfi.Status == RequestForInspectionStatus.Releasing;

                var (statusText, isApproval, isReleased) = ResolveTransactionStatus(
                    1,
                    DivisionType.Division1_TaxAssessmentAndLandHolding,
                    stageId,
                    isClaimed,
                    isDeferred,
                    isDone,
                    lastLog,
                    rfi.Status.ToString());

                items.Add(new WorkflowQueueItemDto
                {
                    RequestId = rfi.Id,
                    RequestType = "RequestForInspection",
                    DisplayType = "Request for Inspection (RFI)",
                    TrackingNumber = rfi.TrackingNumber,
                    OwnerOrApplicant = rfi.OwnerName ?? "Property Owner",
                    ReferenceNumber = rfi.PropertyIndexNumber ?? rfi.TaxDeclarationNumber ?? rfi.TrackingNumber,
                    Division = DivisionType.Division1_TaxAssessmentAndLandHolding,
                    CurrentStage = isReleased ? WorkflowStage.Completed : currentStage,
                    CurrentStageName = GetStageDisplayName(currentStage),
                    AssignedToRole = lastLog?.AssignedToRole ?? "Receiving",
                    AssignedUserId = lastLog?.AssignedUserId,
                    AssignedUserName = lastLog?.AssignedUserName,
                    IsClaimed = isClaimed,
                    IsDeferred = isDeferred,
                    IsApprovalStage = isApproval,
                    StatusText = statusText,
                    TotalAmount = 0,
                    DateSubmitted = rfi.RequestDate,
                    LastForwardedBy = lastLog?.ForwardedByName,
                    LastForwardedDate = lastLog?.Timestamp,
                    LastRemarks = lastLog?.Remarks
                });
            }

            var rpaList = _context.PropertyAssessmentRequests.ToList();
            foreach (var rpa in rpaList)
            {
                var logs = GetForwardingLogs(rpa.Id).ToList();
                var lastLog = logs.FirstOrDefault();
                var currentStage = lastLog?.ToStage ?? (rpa.Status == PropertyAssessmentStatus.Approved ? WorkflowStage.Completed : (WorkflowStage)1);
                int stageId = (int)currentStage;
                bool isClaimed = lastLog?.IsClaimed ?? false;
                bool isDeferred = lastLog?.IsDeferred ?? (rpa.Status == PropertyAssessmentStatus.Deferred);
                bool isDone = lastLog?.ToStage == WorkflowStage.Completed || rpa.Status == PropertyAssessmentStatus.Approved;

                var (statusText, isApproval, isReleased) = ResolveTransactionStatus(
                    2,
                    DivisionType.Division1_TaxAssessmentAndLandHolding,
                    stageId,
                    isClaimed,
                    isDeferred,
                    isDone,
                    lastLog,
                    rpa.Status.ToString());

                items.Add(new WorkflowQueueItemDto
                {
                    RequestId = rpa.Id,
                    RequestType = "PropertyAssessmentRequest",
                    DisplayType = "Property Assessment (RPA)",
                    TrackingNumber = rpa.TrackingNumber,
                    OwnerOrApplicant = rpa.NewOwnerName ?? rpa.PrevOwner ?? "Property Owner",
                    ReferenceNumber = rpa.PrevPIN ?? rpa.PrevTDN ?? rpa.TrackingNumber,
                    Division = DivisionType.Division1_TaxAssessmentAndLandHolding,
                    CurrentStage = isReleased ? WorkflowStage.Completed : currentStage,
                    CurrentStageName = GetStageDisplayName(currentStage),
                    AssignedToRole = lastLog?.AssignedToRole ?? "Receiving",
                    AssignedUserId = lastLog?.AssignedUserId,
                    AssignedUserName = lastLog?.AssignedUserName,
                    IsClaimed = isClaimed,
                    IsDeferred = isDeferred,
                    IsApprovalStage = isApproval,
                    StatusText = statusText,
                    TotalAmount = rpa.TotalFeesAmount,
                    DateSubmitted = rpa.RequestDate,
                    LastForwardedBy = lastLog?.ForwardedByName,
                    LastForwardedDate = lastLog?.Timestamp,
                    LastRemarks = lastLog?.Remarks,
                    ORNumber = rpa.ORNumber,
                    ORDateIssued = rpa.ORDateIssued
                });
            }
        }
        else if (division == DivisionType.Division2_GIS_TaxMapping)
        {
            var tmList = _tmService.GetAll();
            foreach (var tm in tmList)
            {
                var logs = GetForwardingLogs(tm.Id).ToList();
                var lastLog = logs.FirstOrDefault();
                var currentStage = lastLog?.ToStage ?? (tm.Status == TaxMappingRequestStatus.Approved ? WorkflowStage.Completed : (WorkflowStage)15);
                int stageId = (int)currentStage;
                bool isClaimed = lastLog?.IsClaimed ?? false;
                bool isDeferred = lastLog?.IsDeferred ?? false;
                bool isDone = lastLog?.ToStage == WorkflowStage.Completed || tm.Status == TaxMappingRequestStatus.Approved;

                var (statusText, isApproval, isReleased) = ResolveTransactionStatus(
                    3,
                    DivisionType.Division2_GIS_TaxMapping,
                    stageId,
                    isClaimed,
                    isDeferred,
                    isDone,
                    lastLog,
                    tm.Status.ToString());

                items.Add(new WorkflowQueueItemDto
                {
                    RequestId = tm.Id,
                    RequestType = "TaxMappingRequest",
                    DisplayType = "Tax Mapping Request",
                    TrackingNumber = tm.RequestNumber,
                    OwnerOrApplicant = tm.ApplicantName,
                    ReferenceNumber = tm.PropertyIndexNumber,
                    Division = DivisionType.Division2_GIS_TaxMapping,
                    CurrentStage = isReleased ? WorkflowStage.Completed : currentStage,
                    CurrentStageName = GetStageDisplayName(currentStage),
                    AssignedToRole = lastLog?.AssignedToRole ?? "Receiving (GIS)",
                    AssignedUserId = lastLog?.AssignedUserId,
                    AssignedUserName = lastLog?.AssignedUserName,
                    IsClaimed = isClaimed,
                    IsDeferred = isDeferred,
                    IsApprovalStage = isApproval,
                    StatusText = statusText,
                    TotalAmount = tm.TotalFeesAmount,
                    DateSubmitted = tm.RequestDate,
                    LastForwardedBy = lastLog?.ForwardedByName,
                    LastForwardedDate = lastLog?.Timestamp,
                    LastRemarks = lastLog?.Remarks,
                    ORNumber = tm.ORNumber,
                    ORDateIssued = tm.ORDateIssued
                });
            }
        }

        return items;
    }

    public WorkflowDivisionSetting GetDivisionSetting(DivisionType division)
    {
        return _context.WorkflowDivisionSettings.FirstOrDefault(s => s.Division == division)
            ?? new WorkflowDivisionSetting { Division = division };
    }

    public void UpdateDivisionSetting(WorkflowDivisionSetting setting)
    {
        var existing = _context.WorkflowDivisionSettings.FirstOrDefault(s => s.Division == setting.Division);
        if (existing != null)
        {
            existing.DivisionName = setting.DivisionName;
            existing.Description = setting.Description;
            existing.RequireAppraiserReview = setting.RequireAppraiserReview;
            existing.RequireMapperVerification = setting.RequireMapperVerification;
            existing.RequireHeadAssessorApproval = setting.RequireHeadAssessorApproval;
            existing.SlaHoursTarget = setting.SlaHoursTarget;
            existing.AutoNotifyNextStage = setting.AutoNotifyNextStage;
            existing.UpdatedDate = DateTime.Now;
        }
        else
        {
            setting.UpdatedDate = DateTime.Now;
            _context.WorkflowDivisionSettings.Add(setting);
        }
        _context.SaveChanges();
    }

    public WorkflowDivisionStatsDto GetDivisionStats(DivisionType division, Guid? currentUserId = null)
    {
        var queue = BuildQueueItems(division);
        return new WorkflowDivisionStatsDto
        {
            Division = division,
            TotalInQueue = queue.Count,
            PoolCount = queue.Count(i => !i.IsClaimed && i.CurrentStage != WorkflowStage.Completed),
            MyTransactionsCount = currentUserId.HasValue ? queue.Count(i => i.IsClaimed && i.AssignedUserId == currentUserId.Value && i.CurrentStage != WorkflowStage.Completed) : 0,
            DeferredCount = queue.Count(i => i.IsDeferred || i.CurrentStage == WorkflowStage.Returned),
            Stage1Count = queue.Count(i => i.CurrentStage == WorkflowStage.ClerkReceiving),
            Stage2Count = queue.Count(i => i.CurrentStage == WorkflowStage.AppraiserReview),
            Stage3Count = queue.Count(i => i.CurrentStage == WorkflowStage.MapperVerification),
            Stage4Count = queue.Count(i => i.CurrentStage == WorkflowStage.AssessorApproval),
            CompletedCount = queue.Count(i => i.CurrentStage == WorkflowStage.Completed)
        };
    }

    public bool HasOfficialReceipt(Guid requestId, string requestType)
    {
        return requestType switch
        {
            "TaxMappingRequest" => !string.IsNullOrWhiteSpace(_tmService.GetById(requestId)?.ORNumber),
            "LandHoldingRequest" => !string.IsNullOrWhiteSpace(_lhService.GetById(requestId)?.ORNumber),
            "TaxDeclarationRequest" => !string.IsNullOrWhiteSpace(_tdService.GetById(requestId)?.ORNumber),
            "PropertyAssessmentRequest" => !string.IsNullOrWhiteSpace(_context.PropertyAssessmentRequests.FirstOrDefault(r => r.Id == requestId)?.ORNumber),
            _ => true
        };
    }

    public void UpdateOfficialReceipt(Guid requestId, string requestType, string orNumber, DateTime? orDateIssued)
    {
        if (string.IsNullOrWhiteSpace(orNumber)) return;

        if (requestType == "TaxMappingRequest")
        {
            var item = _tmService.GetById(requestId);
            if (item != null)
            {
                item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue) item.ORDateIssued = orDateIssued.Value;
                _tmService.Update(item, null, "");
            }
        }
        else if (requestType == "LandHoldingRequest")
        {
            var item = _lhService.GetById(requestId);
            if (item != null)
            {
                item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue) item.ORDateIssued = orDateIssued.Value;
                _lhService.Update(item, null, "");
            }
        }
        else if (requestType == "TaxDeclarationRequest")
        {
            var item = _tdService.GetById(requestId);
            if (item != null)
            {
                item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue) item.ORDateIssued = orDateIssued.Value;
                _tdService.Update(item, null, "");
            }
        }
        else if (requestType == "PropertyAssessmentRequest")
        {
            var item = _context.PropertyAssessmentRequests.FirstOrDefault(r => r.Id == requestId);
            if (item != null)
            {
                item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue) item.ORDateIssued = orDateIssued.Value;
                _context.SaveChanges();
            }
        }
    }

    public TransactionTimelineDto? GetTransactionTimeline(Guid? requestId, string? trackingNumber = null)
    {
        Guid targetId = requestId ?? Guid.Empty;
        string reqType = "";
        string trk = trackingNumber?.Trim() ?? "";
        string owner = "";
        string refNum = "";
        DateTime dateSubmitted = DateTime.Now;
        string? orNumber = null;
        DateTime? orDateIssued = null;
        decimal totalAmount = 0;
        DivisionType division = DivisionType.Division1_TaxAssessmentAndLandHolding;

        // 1. Locate record in TaxMappingRequest
        var tm = targetId != Guid.Empty ? _tmService.GetById(targetId) : (!string.IsNullOrEmpty(trk) ? _tmService.GetAll().FirstOrDefault(t => t.RequestNumber.Equals(trk, StringComparison.OrdinalIgnoreCase)) : null);
        if (tm != null)
        {
            targetId = tm.Id;
            reqType = "TaxMappingRequest";
            trk = tm.RequestNumber;
            owner = tm.ApplicantName;
            refNum = tm.PropertyIndexNumber ?? "";
            dateSubmitted = tm.RequestDate;
            orNumber = tm.ORNumber;
            orDateIssued = tm.ORDateIssued;
            totalAmount = tm.TotalFeesAmount;
            division = DivisionType.Division2_GIS_TaxMapping;
        }
        else
        {
            // Search in TaxDeclarationRequest
            var td = targetId != Guid.Empty ? _tdService.GetById(targetId) : (!string.IsNullOrEmpty(trk) ? _tdService.GetAll().FirstOrDefault(t => t.TrackingNumber.Equals(trk, StringComparison.OrdinalIgnoreCase)) : null);
            if (td != null)
            {
                targetId = td.Id;
                reqType = "TaxDeclarationRequest";
                trk = td.TrackingNumber;
                owner = td.OwnerName;
                refNum = td.TaxDeclarationNumber ?? "";
                dateSubmitted = td.RequestDate;
                orNumber = td.ORNumber;
                orDateIssued = td.ORDateIssued;
                totalAmount = td.TotalFeesAmount;
                division = DivisionType.Division3_RecordsAndCertification;
            }
            else
            {
                // Search in LandHoldingRequest
                var lh = targetId != Guid.Empty ? _lhService.GetById(targetId) : (!string.IsNullOrEmpty(trk) ? _lhService.GetAll().FirstOrDefault(t => t.TrackingNumber.Equals(trk, StringComparison.OrdinalIgnoreCase)) : null);
                if (lh != null)
                {
                    targetId = lh.Id;
                    reqType = "LandHoldingRequest";
                    trk = lh.TrackingNumber;
                    owner = lh.OwnerName;
                    refNum = lh.TaxDeclarationNumber ?? "";
                    dateSubmitted = lh.RequestDate;
                    orNumber = lh.ORNumber;
                    orDateIssued = lh.ORDateIssued;
                    totalAmount = lh.TotalFeesAmount;
                    division = DivisionType.Division3_RecordsAndCertification;
                }
                else
                {
                    // Search in RequestForInspection
                    var rfi = targetId != Guid.Empty ? _context.RequestForInspections.FirstOrDefault(r => r.Id == targetId) : (!string.IsNullOrEmpty(trk) ? _context.RequestForInspections.FirstOrDefault(r => r.TrackingNumber == trk) : null);
                    if (rfi != null)
                    {
                        targetId = rfi.Id;
                        reqType = "RequestForInspection";
                        trk = rfi.TrackingNumber;
                        owner = rfi.OwnerName ?? "Property Owner";
                        refNum = rfi.PropertyIndexNumber ?? rfi.TaxDeclarationNumber ?? "";
                        dateSubmitted = rfi.RequestDate;
                        division = DivisionType.Division1_TaxAssessmentAndLandHolding;
                    }
                    else
                    {
                        // Search in PropertyAssessmentRequest
                        var rpa = targetId != Guid.Empty ? _context.PropertyAssessmentRequests.FirstOrDefault(r => r.Id == targetId) : (!string.IsNullOrEmpty(trk) ? _context.PropertyAssessmentRequests.FirstOrDefault(r => r.TrackingNumber == trk) : null);
                        if (rpa != null)
                        {
                            targetId = rpa.Id;
                            reqType = "PropertyAssessmentRequest";
                            trk = rpa.TrackingNumber;
                            owner = rpa.NewOwnerName ?? rpa.PrevOwner ?? "Property Owner";
                            refNum = rpa.PrevPIN ?? rpa.PrevTDN ?? "";
                            dateSubmitted = rpa.RequestDate;
                            division = DivisionType.Division1_TaxAssessmentAndLandHolding;
                        }
                    }
                }
            }
        }

        if (targetId == Guid.Empty) return null;

        // 2. Fetch forwarding logs
        var logs = _context.DocumentForwardingLogs
            .Where(l => l.RequestId == targetId)
            .OrderBy(l => l.Timestamp)
            .ToList();

        var latestLog = logs.LastOrDefault();
        int defaultInitial = division switch
        {
            DivisionType.Division2_GIS_TaxMapping => 15,
            DivisionType.Division3_RecordsAndCertification => 25,
            _ => 1
        };

        int currentStageId = latestLog != null && (int)latestLog.ToStage > 0 ? (int)latestLog.ToStage : defaultInitial;
        bool isClaimed = latestLog?.IsClaimed ?? false;
        Guid? assignedUserId = latestLog?.AssignedUserId;
        string? assignedUserName = latestLog?.AssignedUserName;
        string assignedRole = latestLog?.AssignedToRole ?? (division switch
        {
            DivisionType.Division2_GIS_TaxMapping => "Receiving (GIS)",
            DivisionType.Division3_RecordsAndCertification => "Receiving (Records)",
            _ => "Receiving"
        });
        bool isDeferred = latestLog?.IsDeferred ?? false;
        bool isCompleted = latestLog?.ToStage == WorkflowStage.Completed || currentStageId == 999 
            || (latestLog?.ActionTaken == "Released" && (currentStageId == 10 || currentStageId == 19 || currentStageId == 30));

        int transactionTypeId = (reqType ?? "").ToLower() switch
        {
            "requestforinspection" or "inspection" or "rfi" => 1,
            "propertyassessmentrequest" or "propertyassessment" or "rpa" => 2,
            "taxmappingrequest" or "taxmapping" => 3,
            "taxdeclarationrequest" or "taxdeclaration" or "td" => 4,
            "landholdingrequest" or "landholding" or "lh" => 5,
            _ => 1
        };

        var (overallStatus, isApprovalStage, isReleased) = ResolveTransactionStatus(
            transactionTypeId,
            division,
            currentStageId,
            isClaimed,
            isDeferred,
            isCompleted,
            latestLog,
            null);

        isCompleted = isReleased;

        var currentStatus = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == currentStageId);
        string stageName = currentStatus != null ? FormatStatusDisplayName(currentStatus.Name) : $"Step {currentStageId}";

        // Elapsed processing duration
        var span = DateTime.Now - dateSubmitted;
        string elapsedText = span.TotalDays >= 1 
            ? $"{(int)span.TotalDays}d {span.Hours}h in progress" 
            : span.TotalHours >= 1 
                ? $"{(int)span.TotalHours}h {span.Minutes}m in progress" 
                : $"{Math.Max(1, (int)span.TotalMinutes)}m in progress";

        // Current location narrative
        string locDesc = isCompleted 
            ? "Completed & Issued (Final Release)" 
            : isDeferred 
                ? $"Returned / Deferred for Revision at {stageName}" 
                : isApprovalStage
                    ? $"For Approval — Under Review & Signatory Evaluation at {stageName}"
                    : isClaimed && !string.IsNullOrEmpty(assignedUserName) 
                        ? $"On Going — Claimed by {assignedUserName} ({assignedRole})" 
                        : $"Pending — In {stageName} Pool awaiting checkout";

        // 3. Workflow sequential steps for division
        var divisionStatuses = _context.TransactionStatuses
            .Where(s => s.DivisionId == (int)division && s.TransactionStatusId > 0)
            .OrderBy(s => s.TransactionStatusId)
            .ToList();

        var visitedStages = new HashSet<int>(logs.Select(l => (int)l.FromStage).Concat(logs.Select(l => (int)l.ToStage)));
        visitedStages.Add(defaultInitial);

        var stepDtos = new List<TimelineStepDto>();
        foreach (var st in divisionStatuses)
        {
            string state = "pending";
            if (isCompleted)
            {
                state = "completed";
            }
            else if (st.TransactionStatusId == currentStageId)
            {
                state = isDeferred ? "deferred" : (isApprovalStage ? "approval" : "current");
            }
            else if (visitedStages.Contains(st.TransactionStatusId) && st.TransactionStatusId < currentStageId)
            {
                state = "completed";
            }

            bool isStepApproval = _context.WorkflowSequences.Any(w =>
                (w.TransactionTypeId == transactionTypeId || w.TransactionTypeId == (int)division) &&
                w.CurrentStatusId == st.TransactionStatusId &&
                w.IsApproval);

            if (!isStepApproval && !_context.WorkflowSequences.Any(w => (w.TransactionTypeId == transactionTypeId || w.TransactionTypeId == (int)division) && w.CurrentStatusId == st.TransactionStatusId))
            {
                isStepApproval = st.Name.Contains("Approval", StringComparison.OrdinalIgnoreCase) || st.TransactionStatusId == 6 || st.TransactionStatusId == 8 || st.TransactionStatusId == 18 || st.TransactionStatusId == 29;
            }

            var matchingLog = logs.LastOrDefault(l => (int)l.ToStage == st.TransactionStatusId || (int)l.FromStage == st.TransactionStatusId);

            stepDtos.Add(new TimelineStepDto
            {
                StepId = st.TransactionStatusId,
                StepName = FormatStatusDisplayName(st.Name),
                AssignedRole = st.Name,
                State = state,
                IsApproval = isStepApproval,
                Timestamp = matchingLog?.Timestamp,
                HandledBy = matchingLog?.AssignedUserName ?? matchingLog?.ForwardedByName,
                Remarks = matchingLog?.Remarks
            });
        }

        // 4. Chronological event trail
        var eventDtos = new List<TimelineEventDto>();

        // Initial creation event
        eventDtos.Add(new TimelineEventDto
        {
            LogId = Guid.Empty,
            Timestamp = dateSubmitted,
            FromStageId = 0,
            FromStageName = "Citizen / Taxpayer Filing",
            ToStageId = defaultInitial,
            ToStageName = divisionStatuses.FirstOrDefault(s => s.TransactionStatusId == defaultInitial)?.Name ?? "Receiving",
            ForwardedByName = owner,
            ForwardedByRole = "Applicant / Taxpayer",
            AssignedToRole = assignedRole,
            ActionTaken = "Filing & Intake",
            Remarks = $"Initial transaction registration filed for {trk}. Total assessment fees: ₱{totalAmount:N2}.",
            IsClaimed = false,
            IsDeferred = false,
            ORNumber = orNumber,
            ORDateIssued = orDateIssued
        });

        foreach (var l in logs)
        {
            var fromSt = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == (int)l.FromStage);
            var toSt = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == (int)l.ToStage);

            eventDtos.Add(new TimelineEventDto
            {
                LogId = l.Id,
                Timestamp = l.Timestamp,
                FromStageId = (int)l.FromStage,
                FromStageName = fromSt != null ? FormatStatusDisplayName(fromSt.Name) : l.FromStage.ToString(),
                ToStageId = (int)l.ToStage,
                ToStageName = toSt != null ? FormatStatusDisplayName(toSt.Name) : l.ToStage.ToString(),
                ForwardedByName = l.ForwardedByName,
                ForwardedByRole = l.ForwardedByRole,
                AssignedToRole = l.AssignedToRole,
                ActionTaken = l.ActionTaken,
                Remarks = l.Remarks,
                AssignedUserName = l.AssignedUserName,
                IsClaimed = l.IsClaimed,
                IsDeferred = l.IsDeferred,
                ORNumber = orNumber,
                ORDateIssued = orDateIssued
            });
        }

        string displayType = (reqType ?? "") switch
        {
            "TaxMappingRequest" => "Tax Mapping Request (TMR)",
            "TaxDeclarationRequest" => "Tax Declaration Certified Record",
            "LandHoldingRequest" => "Land Holding Certificate",
            "RequestForInspection" => "Request for Inspection (RFI)",
            "PropertyAssessmentRequest" => "Property Assessment (RPA)",
            _ => reqType ?? "General Request"
        };

        string divName = division switch
        {
            DivisionType.Division1_TaxAssessmentAndLandHolding => "Division 1: Appraisal & Assessment",
            DivisionType.Division2_GIS_TaxMapping => "Division 2: Tax Mapping & GIS",
            DivisionType.Division3_RecordsAndCertification => "Division 3: Records & Certification",
            _ => "General Assessor"
        };

        return new TransactionTimelineDto
        {
            RequestId = targetId,
            TrackingNumber = trk ?? "",
            RequestType = reqType ?? "",
            DisplayType = displayType,
            OwnerOrApplicant = owner,
            PropertyReference = refNum,
            DateSubmitted = dateSubmitted,
            ORNumber = orNumber,
            ORDateIssued = orDateIssued,
            TotalAmount = totalAmount,
            Division = division,
            DivisionName = divName,
            CurrentStageId = currentStageId,
            CurrentStageName = stageName,
            CurrentLocationDescription = locDesc,
            AssignedToRole = assignedRole,
            AssignedUserId = assignedUserId,
            AssignedUserName = assignedUserName,
            IsClaimed = isClaimed,
            IsDeferred = isDeferred,
            IsCompleted = isCompleted,
            IsApprovalStage = isApprovalStage,
            OverallStatus = overallStatus,
            ElapsedTimeText = elapsedText,
            WorkflowSteps = stepDtos,
            TimelineEvents = eventDtos.OrderByDescending(e => e.Timestamp).ToList()
        };
    }

    private void SyncPropertyAssessmentPartitionTaxDeclarations(PropertyAssessmentRequest request)
    {
        if (request == null) return;

        List<AssessmentPartitionItem>? partitions = null;
        List<string> tdnList = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.GeneratedPartitionsJson))
        {
            try
            {
                partitions = PartitionJsonHelper.ParseList(request.GeneratedPartitionsJson);
                if (partitions != null)
                {
                    tdnList = partitions
                        .Where(p => !string.IsNullOrWhiteSpace(p.SystemGeneratedTDN))
                        .Select(p => p.SystemGeneratedTDN)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct()
                        .ToList();
                }
            }
            catch { }
        }

        var targetStatus = request.Status == PropertyAssessmentStatus.Approved 
            ? AssessmentStatus.Active 
            : AssessmentStatus.Pending;

        var trackingToken = request.TrackingNumber;

        var tds = _context.TaxDeclarations
            .Where(t => (tdnList.Count > 0 && tdnList.Contains(t.TaxDeclarationNumber)) ||
                        (!string.IsNullOrEmpty(trackingToken) && t.Memoranda != null && t.Memoranda.Contains(trackingToken)))
            .ToList();

        foreach (var td in tds)
        {
            if (td.Status == AssessmentStatus.Pending || td.Status == AssessmentStatus.Active)
            {
                td.Status = targetStatus;
            }
        }

        // Handle Mother Parcel Cancellation & Superseding Workflow (e.g. Subdivision - SD, Consolidation - CS, Physical Change - PC, Transfer - TR, Reclassification - RC)
        var isSubdivision = string.Equals(request.TransactionTypeCode, "SD", StringComparison.OrdinalIgnoreCase);
        var isConsolidation = string.Equals(request.TransactionTypeCode, "CS", StringComparison.OrdinalIgnoreCase);
        var isPhysicalChange = string.Equals(request.TransactionTypeCode, "PC", StringComparison.OrdinalIgnoreCase);
        var isTransfer = string.Equals(request.TransactionTypeCode, "TR", StringComparison.OrdinalIgnoreCase);
        var isReclassification = string.Equals(request.TransactionTypeCode, "RC", StringComparison.OrdinalIgnoreCase);
        var isMotherCancellationTx = isSubdivision || isConsolidation || isPhysicalChange || isTransfer || isReclassification;

        var motherIds = new HashSet<Guid>();
        if (request.RefTaxDeclarationId.HasValue && request.RefTaxDeclarationId.Value != Guid.Empty)
        {
            motherIds.Add(request.RefTaxDeclarationId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.SelectedRefPropertyIdsJson))
        {
            try
            {
                var parsedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(request.SelectedRefPropertyIdsJson);
                if (parsedIds != null)
                {
                    foreach (var mId in parsedIds)
                    {
                        if (mId != Guid.Empty) motherIds.Add(mId);
                    }
                }
            }
            catch { }
        }

        if (partitions != null)
        {
            foreach (var p in partitions)
            {
                if (p.BasePropertyId.HasValue && p.BasePropertyId.Value != Guid.Empty)
                {
                    motherIds.Add(p.BasePropertyId.Value);
                }
            }
        }

        var newTdIds = tds.Select(t => t.Id).ToHashSet();
        var motherTds = _context.TaxDeclarations
            .Where(t => motherIds.Contains(t.Id) && !newTdIds.Contains(t.Id))
            .ToList();

        if (motherTds.Count == 0 && (!string.IsNullOrWhiteSpace(request.PrevTDN) || !string.IsNullOrWhiteSpace(request.PrevPIN)))
        {
            var cleanPrevTdn = request.PrevTDN?.Trim();
            var cleanPrevPin = request.PrevPIN?.Trim();
            motherTds = _context.TaxDeclarations
                .Where(t => !newTdIds.Contains(t.Id) &&
                            ((!string.IsNullOrEmpty(cleanPrevTdn) && t.TaxDeclarationNumber == cleanPrevTdn) ||
                             (!string.IsNullOrEmpty(cleanPrevPin) && t.PropertyIndexNumber == cleanPrevPin)))
                .ToList();
        }

        if (request.Status == PropertyAssessmentStatus.Approved)
        {
            var newLotTdns = tds.Select(t => t.TaxDeclarationNumber).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            var newLotsDisplay = newLotTdns.Count > 0 ? string.Join(", ", newLotTdns) : (isConsolidation ? "Consolidated Lot TD" : (isPhysicalChange ? "Renovated/Expanded Property TD" : (isTransfer ? "Transferred Property TD" : (isReclassification ? "Reclassified Property TD" : "Segregated Lot TDs"))));

            foreach (var motherTd in motherTds)
            {
                if (isMotherCancellationTx)
                {
                    motherTd.Status = AssessmentStatus.Cancelled;
                    motherTd.IsCancelledAndSuperseded = true;
                    motherTd.CancellationReason = isSubdivision 
                        ? "Subdivision (SD) · Segregation of Land" 
                        : (isConsolidation 
                            ? "Consolidation (CS) · Lot Integration" 
                            : (isPhysicalChange 
                                ? "Reassessment Due to Physical Change (PC) · Renovation / Expansion" 
                                : (isTransfer
                                    ? "Transfer (TR) · Administrative Title Conveyance"
                                    : (isReclassification
                                        ? "Reclassification (RC) · Actual Ground Use Conversion"
                                        : "Cancelled & Superseded"))));
                    motherTd.DateCancelled = DateTime.Now;
                    motherTd.SupersededByTdns = newLotsDisplay;
                    motherTd.CancelledByRequestId = request.Id;

                    var stampMarker = "*** CANCELLED & SUPERSEDED ***";
                    if (string.IsNullOrWhiteSpace(motherTd.Memoranda) || !motherTd.Memoranda.Contains(stampMarker))
                    {
                        var cancelStamp = isSubdivision
                            ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                              $"Cancelled by Subdivision (SD) · Segregation of Land under Assessment Request #{request.TrackingNumber} " +
                              $"pursuant to LRA/DENR-approved plan on {DateTime.Now:MMMM dd, yyyy}.\n" +
                              $"Superseded by New Segregated Lot TDs: {newLotsDisplay}.\n" +
                              $"Tax liabilities and delinquencies apportioned independently."
                            : (isConsolidation
                                ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                                  $"Cancelled by Consolidation (CS) · Lot Integration under Assessment Request #{request.TrackingNumber} " +
                                  $"pursuant to field inspection and LRA/DENR-approved consolidation plan on {DateTime.Now:MMMM dd, yyyy}.\n" +
                                  $"Superseded by Consolidated TD: {newLotsDisplay}.\n" +
                                  $"Real property tax receivables in the Treasury are collapsed from multiple ledgers into a unified bill."
                                : (isPhysicalChange
                                    ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                                      $"Cancelled due to Reassessment Due to Physical Change (PC) · Renovation / Expansion under Assessment Request #{request.TrackingNumber} on {DateTime.Now:MMMM dd, yyyy}.\n" +
                                      $"Superseded by New Reassessed TD: {newLotsDisplay}.\n" +
                                      $"Structural renovation/expansion logged with upward Fair Market Value and Assessed Value adjustment."
                                    : (isTransfer
                                        ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                                          $"Cancelled due to Transfer (TR) · Administrative Title Conveyance under Assessment Request #{request.TrackingNumber} on {DateTime.Now:MMMM dd, yyyy}.\n" +
                                          $"Superseded by Replacement TD: {newLotsDisplay} issued to Transferee: {request.NewOwnerName ?? "New Owner"}.\n" +
                                          $"Executed upon presentation of TCT, BIR CAR/eCAR, and Transfer Tax Receipt. Billing ledgers redirected to transferee."
                                        : (isReclassification
                                            ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                                              $"Cancelled due to Reclassification (RC) · Actual Ground Use Conversion under Assessment Request #{request.TrackingNumber} on {DateTime.Now:MMMM dd, yyyy}.\n" +
                                              $"Superseded by Reclassified TD: {newLotsDisplay}.\n" +
                                              $"Ground physical use converted pursuant to field inspection under Sec. 217, RA 7160. Statutory assessment level adjusted without requiring title re-titling."
                                            : $"\n\n*** CANCELLED & SUPERSEDED ***\nCancelled under Assessment Request #{request.TrackingNumber} on {DateTime.Now:MMMM dd, yyyy}.\nSuperseded by: {newLotsDisplay}."))));
                        motherTd.Memoranda = string.IsNullOrWhiteSpace(motherTd.Memoranda)
                            ? cancelStamp.TrimStart()
                            : (motherTd.Memoranda + cancelStamp);
                    }
                }
            }

            var primaryMother = motherTds.FirstOrDefault();
            var motherTdnList = string.Join(", ", motherTds.Select(m => m.TaxDeclarationNumber).Where(n => !string.IsNullOrWhiteSpace(n)));
            var motherPinList = string.Join(", ", motherTds.Select(m => m.PropertyIndexNumber).Where(n => !string.IsNullOrWhiteSpace(n)));

            foreach (var td in tds)
            {
                td.Status = AssessmentStatus.Active;
                if (primaryMother != null)
                {
                    if (string.IsNullOrWhiteSpace(td.PreviousTdNo)) 
                        td.PreviousTdNo = isConsolidation && !string.IsNullOrWhiteSpace(motherTdnList) ? motherTdnList : primaryMother.TaxDeclarationNumber;
                    if (string.IsNullOrWhiteSpace(td.PreviousPin)) 
                        td.PreviousPin = isConsolidation && !string.IsNullOrWhiteSpace(motherPinList) ? motherPinList : primaryMother.PropertyIndexNumber;
                    if (string.IsNullOrWhiteSpace(td.PreviousOwner)) 
                        td.PreviousOwner = primaryMother.OwnerName;
                    if (td.PreviousMarketValue <= 0) 
                        td.PreviousMarketValue = isConsolidation ? motherTds.Sum(m => m.MarketValue) : primaryMother.MarketValue;
                    if (td.PreviousAssessedValue <= 0) 
                        td.PreviousAssessedValue = isConsolidation ? motherTds.Sum(m => m.AssessedValue) : primaryMother.AssessedValue;

                    if (isTransfer)
                    {
                        // Retains identical land area, building classifications, and PIN, while redirecting billing ledgers to transferee
                        if (!string.IsNullOrWhiteSpace(primaryMother.PropertyIndexNumber) && (string.IsNullOrWhiteSpace(td.PropertyIndexNumber) || td.PropertyIndexNumber.EndsWith("-A")))
                        {
                            td.PropertyIndexNumber = primaryMother.PropertyIndexNumber;
                        }
                        if (td.AreaSqMeters <= 0 && primaryMother.AreaSqMeters > 0)
                        {
                            td.AreaSqMeters = primaryMother.AreaSqMeters;
                        }
                    }
                }
                if (isSubdivision && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("apportioned independently"))
                {
                    td.Memoranda += " Tax liabilities and delinquencies apportioned independently.";
                }
                if (isConsolidation && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("collapsed from multiple ledgers"))
                {
                    td.Memoranda += " Real property tax receivables in the Treasury are collapsed from multiple ledgers into a unified bill.";
                }
                if (isPhysicalChange && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("upward adjustment"))
                {
                    td.Memoranda += " Upward adjustment in Fair Market Value and Assessed Value following structural renovation/expansion.";
                }
                if (isTransfer && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("Administrative Title Conveyance"))
                {
                    td.Memoranda += " Transferred under Administrative Title Conveyance (TR) pursuant to TCT, BIR CAR/eCAR, and Transfer Tax Receipt. Retains identical PIN and classifications. Billing ledgers redirected to transferee.";
                }
                if (isReclassification && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("Actual Ground Use Conversion"))
                {
                    td.Memoranda += " Reclassified due to Actual Ground Use Conversion (RC) under Sec. 217 of R.A. 7160. Statutory assessment level and annual property tax dues adjusted based on confirmed economic ground activity.";
                }
            }
        }
        else
        {
            foreach (var motherTd in motherTds)
            {
                if (motherTd.IsCancelledAndSuperseded &&
                    (motherTd.CancelledByRequestId == request.Id || (motherTd.Memoranda != null && motherTd.Memoranda.Contains(request.TrackingNumber))))
                {
                    motherTd.Status = AssessmentStatus.Active;
                    motherTd.IsCancelledAndSuperseded = false;
                    motherTd.CancellationReason = null;
                    motherTd.DateCancelled = null;
                    motherTd.SupersededByTdns = null;
                    motherTd.CancelledByRequestId = null;
                }
            }
        }
    }
}
