namespace PropertyAssessmentApp.Models;

public class UserPerformanceFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DivisionId { get; set; }
    public string? RequestType { get; set; }
    public string? Search { get; set; }
}

public class DivisionComparisonDto
{
    public int DivisionId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
    public int FinishedCount { get; set; }
    public int OngoingCount { get; set; }
    public int PendingCount { get; set; }
    public int DeferredCount { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AvgTurnaroundDays { get; set; }
    public decimal ShareOfFinishedPercent { get; set; }
    public int OnTimeFinishedCount { get; set; }
    public int OverdueFinishedCount { get; set; }
    public decimal SlaComplianceRate { get; set; }
    public double AvgWorkingMinutes { get; set; }
}

public class RequestTypeDivisionStatDto
{
    public string RequestType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int FinishedCount { get; set; }
    public decimal CompletionRate { get; set; }
}

public class UserPerformanceRowDto
{
    public int Rank { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string RoleDisplay { get; set; } = string.Empty;
    public int? DivisionId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public int UniqueTransactionsProcessed { get; set; }
    public int TotalActions { get; set; }
    public int ForwardedCount { get; set; }
    public int ApprovedOrReleasedCount { get; set; }
    public int DeferredOrReturnedCount { get; set; }
    public int ActiveClaimedCount { get; set; }
    public int OverdueCount { get; set; }
    public bool HasRoleMaxTime { get; set; }
    public DateTime? LastActiveDate { get; set; }
    public string LastActiveText => LastActiveDate.HasValue ? LastActiveDate.Value.ToString("MMM dd, yyyy hh:mm tt") : "—";
    public bool IsActive { get; set; } = true;
}

public class UserPerformanceDashboardDataDto
{
    public string PeriodLabel { get; set; } = "All Time";
    public int TotalActionsProcessed { get; set; }
    public int UniqueTransactionsProcessed { get; set; }
    public int ActiveContributors { get; set; }
    public int TotalActiveUsers { get; set; }
    public int TotalTransactionsInPeriod { get; set; }
    public int TotalFinishedInPeriod { get; set; }
    public decimal OverallCompletionRate { get; set; }
    public string TopDivisionName { get; set; } = "—";
    public int TopDivisionFinished { get; set; }
    public decimal TopDivisionRate { get; set; }
    public string TopUserName { get; set; } = "—";
    public string TopUserRole { get; set; } = string.Empty;
    public int TopUserTransactions { get; set; }

    public List<DivisionComparisonDto> DivisionStats { get; set; } = new();
    public List<string> TrendLabels { get; set; } = new();
    public List<int> Div1FinishedTrend { get; set; } = new();
    public List<int> Div2FinishedTrend { get; set; } = new();
    public List<int> Div3FinishedTrend { get; set; } = new();
    public List<RequestTypeDivisionStatDto> RequestTypeStats { get; set; } = new();
    public List<UserPerformanceRowDto> UserLeaderboard { get; set; } = new();
}
