using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Models;

public class DashboardViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string ActionName { get; set; } = "AssessorHead";

    // Date filter
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Workflow status counters (shared by all dashboards)
    public int Total { get; set; }
    public int Pending { get; set; }
    public int OnGoing { get; set; }
    public int ForApproval { get; set; }
    public int Deferred { get; set; }
    public int Released { get; set; }

    // Staff dashboard
    public int PoolForMe { get; set; }
    public int MyClaimed { get; set; }

    // Per-division and per-request-type breakdowns
    public List<DashboardBreakdownItem> ByDivision { get; set; } = new();
    public List<DashboardBreakdownItem> ByRequestType { get; set; } = new();

    // Tables
    public List<WorkflowQueueItemDto> PrimaryList { get; set; } = new();
    public string PrimaryListTitle { get; set; } = "Recent Transactions";
    public List<WorkflowQueueItemDto> RecentList { get; set; } = new();

    // Property / Tax Declaration Summary (Assessor Head & System Owner)
    public int TotalTaxDeclarations { get; set; }
    public int TotalCancelledTD { get; set; }
    public int TotalLandTD { get; set; }
    public int TotalBuildingTD { get; set; }
    public int TotalMachineryTD { get; set; }

    // Today's Transactions (Assessor Head interactive status cards + filtered table)
    public List<WorkflowQueueItemDto> TodayPendingList { get; set; } = new();
    public List<WorkflowQueueItemDto> TodayOnGoingList { get; set; } = new();
    public List<WorkflowQueueItemDto> TodayApprovedList { get; set; } = new();
    public List<WorkflowQueueItemDto> TodayReleasedList { get; set; } = new();

    // System owner dashboard
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalRoles { get; set; }
    public int RolesWithAccess { get; set; }
    public int TotalOwners { get; set; }
    public int ActiveModules { get; set; }
    public List<DashboardBreakdownItem> UsersByRole { get; set; } = new();
    public List<DocumentForwardingLog> RecentActivity { get; set; } = new();

    // Analytics & Request Trend Graph
    public decimal TotalFees { get; set; }
    public decimal ReleasedFees { get; set; }
    public List<string> TrendLabels { get; set; } = new();
    public List<int> TrendSubmitted { get; set; } = new();
    public List<int> TrendReleased { get; set; } = new();
    public List<int> TrendTaxDec { get; set; } = new();
    public List<int> TrendTaxMapping { get; set; } = new();
    public List<int> TrendLandHolding { get; set; } = new();
    public List<int> TrendPropertyAssessment { get; set; } = new();
    public List<int> TrendPropertyInspection { get; set; } = new();

    public List<DashboardBreakdownItem> AgingBuckets { get; set; } = new();
    public List<DashboardBreakdownItem> Bottlenecks { get; set; } = new();
    public List<DashboardBreakdownItem> TopHandlers { get; set; } = new();
    public List<string> ActivityLabels { get; set; } = new();
    public List<int> ActivityTrend { get; set; } = new();
}

public class DashboardBreakdownItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
