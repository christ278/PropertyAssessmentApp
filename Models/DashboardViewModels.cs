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

public class ExecutiveAnalyticsViewModel
{
    public List<(string Code, string Name)> Barangays { get; set; } = new();
    public List<string> ActualUses { get; set; } = new();
    public List<int> AssessmentYears { get; set; } = new();
    public List<string> Sections { get; set; } = new();
}

public class DashboardAnalyticsFilterDto
{
    public string Preset { get; set; } = "YTD";
    public int? AssessmentYear { get; set; }
    public int PropertyKind { get; set; } = 0; // 0 = All, 1 = Land, 2 = Building, 3 = Machinery
    public string? BarangayFilter { get; set; }
    public string? SectionFilter { get; set; }
    public string? ActualUseFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
}

public class ExecutiveAnalyticsResponseDto
{
    public ExecutiveKpiMetricsDto Kpis { get; set; } = new();
    public RpuDistributionDto RpuDistribution { get; set; } = new();
    public List<MonthlyVolumeTrendDto> MonthlyTrends { get; set; } = new();
    public List<TransactionDistributionDto> TransactionDistributions { get; set; } = new();
    public List<ActualUseDistributionDto> ActualUseDistributions { get; set; } = new();
    public List<BarangayAnalyticsRowDto> BarangayLeaderboard { get; set; } = new();
    public List<BarangayAnalyticsRowDto> SummaryTableRows { get; set; } = new();
    public ClassificationRollSummaryDto ClassificationSummary { get; set; } = new();
    public AssessmentRollPagedResponseDto AssessmentRollPage { get; set; } = new();
    public List<string> AvailableSections { get; set; } = new();
    public string FilterSummary { get; set; } = string.Empty;
}

public class ExecutiveKpiMetricsDto
{
    public int TotalRpus { get; set; }
    public string TotalRpusComparisonText { get; set; } = "↑ 2.35% vs. 2025 (74,256)";
    public decimal TotalAssessmentRollValue { get; set; }
    public decimal TotalMarketValue { get; set; }
    public int TotalActiveDeclarations { get; set; }
    public int ProcessedTransactionsCount { get; set; }
    public string TransactionsComparisonText { get; set; } = "↑ 14.4% vs. 2025 (201)";
    public decimal TaxableRatioPct { get; set; } = 100m;
    public decimal ExemptRatioPct { get; set; } = 0m;
    public decimal TotalTaxableAssessedValue { get; set; }
    public decimal TotalExemptAssessedValue { get; set; }
}

public class RpuDistributionDto
{
    public int TotalRpus { get; set; }
    public int LandCount { get; set; }
    public decimal LandPct { get; set; }
    public int BuildingCount { get; set; }
    public decimal BuildingPct { get; set; }
    public int MachineryCount { get; set; }
    public decimal MachineryPct { get; set; }
}

public class ClassificationRollRowDto
{
    public string PropertyClassification { get; set; } = string.Empty;
    public decimal LandAreaSqm { get; set; }
    public int RpuLand { get; set; }
    public int RpuBldg { get; set; }
    public int RpuMach { get; set; }
    public int TotalRpu { get; set; }
    public decimal MarketValue { get; set; }
    public decimal AssessedValue { get; set; }
}

public class ClassificationRollSummaryDto
{
    public List<ClassificationRollRowDto> TaxableRows { get; set; } = new();
    public ClassificationRollRowDto TaxableTotals { get; set; } = new();
    public List<ClassificationRollRowDto> ExemptRows { get; set; } = new();
    public ClassificationRollRowDto ExemptTotals { get; set; } = new();
    public decimal TotalLandAreaSqm { get; set; }
    public int TotalProperties { get; set; }
    public int TaxablePropertiesCount { get; set; }
    public decimal TaxablePropertiesPct { get; set; } = 96.0m;
    public int ExemptPropertiesCount { get; set; }
    public decimal ExemptPropertiesPct { get; set; } = 4.0m;
}

public class AssessmentRollDetailedRecordDto
{
    public string TdNo { get; set; } = string.Empty;
    public string PreviousTdNo { get; set; } = "-";
    public string Section { get; set; } = "-";
    public string ParcelNo { get; set; } = "-";
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerAddress { get; set; } = "-";
    public string PrevOwner { get; set; } = "-";
    public string CadNo { get; set; } = "-";
    public string SurveyNo { get; set; } = "-";
    public string TitleNo { get; set; } = "-";
    public string PreviousPin { get; set; } = "-";
    public string Classification { get; set; } = "Residential";
    public string Kind { get; set; } = "Land";
    public string ActualUse { get; set; } = "Residential";
    public decimal AreaSqm { get; set; }
    public decimal MarketValue { get; set; }
    public decimal AssessedValue { get; set; }
    public int BldgCount { get; set; }
    public int MachCount { get; set; }
    public string Memoranda { get; set; } = "-";
    public string EffectivityDate { get; set; } = "-";
}

public class AssessmentRollPagedResponseDto
{
    public List<AssessmentRollDetailedRecordDto> Records { get; set; } = new();
    public int TotalRecords { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public int TotalPages { get; set; } = 1;
}

public class MonthlyVolumeTrendDto
{
    public string MonthLabel { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal AddedAssessedValue { get; set; }
}

public class TransactionDistributionDto
{
    public string TransactionCode { get; set; } = string.Empty;
    public string TransactionName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAssessedValue { get; set; }
    public decimal Percentage { get; set; }
}

public class ActualUseDistributionDto
{
    public string ActualUseName { get; set; } = string.Empty;
    public decimal TotalMarketValue { get; set; }
    public decimal TotalAssessedValue { get; set; }
    public int ParcelCount { get; set; }
    public decimal Percentage { get; set; }
}

public class BarangayAnalyticsRowDto
{
    public string BarangayCode { get; set; } = string.Empty;
    public string BarangayName { get; set; } = string.Empty;
    public int TotalParcelCount { get; set; }
    public int ActiveCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalLandArea { get; set; }
    public decimal TotalMarketValue { get; set; }
    public decimal TotalAssessedValue { get; set; }
}
