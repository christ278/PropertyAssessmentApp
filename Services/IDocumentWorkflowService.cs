using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class WorkflowQueueItemDto
{
    public Guid RequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string DisplayType { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string OwnerOrApplicant { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public DivisionType Division { get; set; }
    public WorkflowStage CurrentStage { get; set; }
    public string CurrentStageName { get; set; } = string.Empty;
    public string AssignedToRole { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsDeferred { get; set; }
    public bool IsApprovalStage { get; set; }
    public bool IsForApproval { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime DateSubmitted { get; set; }
    public string? LastForwardedBy { get; set; }
    public DateTime? LastForwardedDate { get; set; }
    public string? LastRemarks { get; set; }
    public string? ORNumber { get; set; }
    public DateTime? ORDateIssued { get; set; }
}

public class WorkflowDivisionStatsDto
{
    public DivisionType Division { get; set; }
    public int TotalInQueue { get; set; }
    public int PoolCount { get; set; }
    public int MyTransactionsCount { get; set; }
    public int DeferredCount { get; set; }
    public int Stage1Count { get; set; }
    public int Stage2Count { get; set; }
    public int Stage3Count { get; set; }
    public int Stage4Count { get; set; }
    public int CompletedCount { get; set; }
}

public class NextWorkflowStageDto
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsApproval { get; set; }
    public bool IsComplete { get; set; }
}

public class TimelineStepDto
{
    public int StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string AssignedRole { get; set; } = string.Empty;
    public string State { get; set; } = "pending"; // "completed", "current", "pending", "deferred"
    public bool IsApproval { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? HandledBy { get; set; }
    public string? Remarks { get; set; }
}

public class TimelineEventDto
{
    public Guid LogId { get; set; }
    public DateTime Timestamp { get; set; }
    public int FromStageId { get; set; }
    public string FromStageName { get; set; } = string.Empty;
    public int ToStageId { get; set; }
    public string ToStageName { get; set; } = string.Empty;
    public string ForwardedByName { get; set; } = string.Empty;
    public string ForwardedByRole { get; set; } = string.Empty;
    public string AssignedToRole { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? AssignedUserName { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsDeferred { get; set; }
    public string? ORNumber { get; set; }
    public DateTime? ORDateIssued { get; set; }
}

public class TransactionTimelineDto
{
    public Guid RequestId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string DisplayType { get; set; } = string.Empty;
    public string OwnerOrApplicant { get; set; } = string.Empty;
    public string PropertyReference { get; set; } = string.Empty;
    public DateTime DateSubmitted { get; set; }
    public string? ORNumber { get; set; }
    public DateTime? ORDateIssued { get; set; }
    public decimal TotalAmount { get; set; }
    public DivisionType Division { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public int CurrentStageId { get; set; }
    public string CurrentStageName { get; set; } = string.Empty;
    public string CurrentLocationDescription { get; set; } = string.Empty;
    public string AssignedToRole { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsDeferred { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsApprovalStage { get; set; }
    public bool IsForApproval { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public string ElapsedTimeText { get; set; } = string.Empty;
    public List<TimelineStepDto> WorkflowSteps { get; set; } = new();
    public List<TimelineEventDto> TimelineEvents { get; set; } = new();
}

public interface IDocumentWorkflowService
{
    IEnumerable<DocumentForwardingLog> GetForwardingLogs(Guid requestId);
    IEnumerable<DocumentForwardingLog> GetDivisionLogs(DivisionType division);
    DocumentForwardingLog ForwardDocument(Guid requestId, string requestType, DivisionType division, WorkflowStage targetStage, string forwardedByName, string role, string remarks, string actionTaken = "Forwarded", string? orNumber = null, DateTime? orDateIssued = null);
    IEnumerable<NextWorkflowStageDto> GetNextStagesForRequest(Guid requestId, string requestType, Guid? currentUserId = null);
    bool HasOfficialReceipt(Guid requestId, string requestType);
    void UpdateOfficialReceipt(Guid requestId, string requestType, string orNumber, DateTime? orDateIssued);
    TransactionTimelineDto? GetTransactionTimeline(Guid? requestId, string? trackingNumber = null);
    
    // Departmental Pool & Ownership Queue Methods
    bool ClaimDocument(Guid requestId, string requestType, Guid userId, string userName);
    bool ReleaseDocumentToPool(Guid requestId, string requestType);
    bool DeferDocument(Guid requestId, string requestType, DivisionType division, Guid userId, string userName, string userRole, string remarks);
    bool TagDocumentForApproval(Guid requestId, string requestType, DivisionType division, Guid userId, string userName, string userRole, string remarks);
    
    IEnumerable<WorkflowQueueItemDto> GetQueueForDivision(DivisionType division, WorkflowStage? stageFilter = null, string? search = null);
    IEnumerable<WorkflowQueueItemDto> GetPoolQueueForDivision(DivisionType division, WorkflowStage? stageFilter = null, string? search = null);
    int GetUserPendingPoolCount(System.Security.Claims.ClaimsPrincipal? user);
    IEnumerable<WorkflowQueueItemDto> GetPersonalQueueForUser(Guid userId, DivisionType? divisionFilter = null, string? search = null);
    IEnumerable<WorkflowQueueItemDto> GetUnifiedQueue(
        int? divisionId = null, 
        string? mode = "pool", 
        WorkflowStage? stageFilter = null, 
        string? search = null, 
        string? requestType = null, 
        Guid? userId = null,
        int? userWorkflowRoleId = null,
        string? userWorkflowRoleName = null,
        bool isSuperAdminOrHead = false);
    
    WorkflowDivisionSetting GetDivisionSetting(DivisionType division);
    void UpdateDivisionSetting(WorkflowDivisionSetting setting);
    WorkflowDivisionStatsDto GetDivisionStats(DivisionType division, Guid? currentUserId = null);

    // Dynamic Stage Configuration
    IEnumerable<WorkflowStageSetting> GetStagesForDivision(DivisionType division);
    WorkflowStageSetting AddStage(DivisionType division, string stageName, string assignedRole, string? description, bool isApprovalStage);
    void UpdateStage(WorkflowStageSetting stage);
    void DeleteStage(Guid stageId);
    void MoveStage(Guid stageId, bool moveUp);
}
