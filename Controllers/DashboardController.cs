using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

/// <summary>
/// Three role-based dashboards. Access to each one is controlled by its own SystemModule
/// (DASHBOARD_USER, DASHBOARD_HEAD, DASHBOARD_ADMIN) in the Role Permissions screen.
/// </summary>
public class DashboardController : Controller
{
    private readonly IDocumentWorkflowService _workflow;
    private readonly IPermissionService _permissions;
    private readonly ApplicationDbContext _context;

    public DashboardController(IDocumentWorkflowService workflow, IPermissionService permissions, ApplicationDbContext context)
    {
        _workflow = workflow;
        _permissions = permissions;
        _context = context;
    }

    // GET: /Dashboard  -> routes to the most senior dashboard the user has been granted
    public IActionResult Index(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_permissions.HasModuleAccess(User, "Dashboard", "SystemOwner")) return RedirectToAction(nameof(SystemOwner), new { startDate, endDate });
        if (_permissions.HasModuleAccess(User, "Dashboard", "AssessorHead")) return RedirectToAction(nameof(AssessorHead), new { startDate, endDate });
        if (_permissions.HasModuleAccess(User, "Dashboard", "Staff")) return RedirectToAction(nameof(Staff), new { startDate, endDate });
        return View("NoDashboard");
    }

    // GET: /Dashboard/Staff  (ordinary users)
    public IActionResult Staff(DateTime? startDate = null, DateTime? endDate = null)
    {
        var items = GetAllItems();
        var model = BuildBase("My Dashboard", "Your queue and the transactions in your division", nameof(Staff), startDate, endDate);

        int? division = int.TryParse(User.FindFirst("DivisionId")?.Value, out var d) && d > 0 ? d : null;
        int? stepId = int.TryParse(User.FindFirst("WorkflowRoleId")?.Value, out var s) && s > 0 ? s : null;
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId);

        var scoped = division.HasValue ? items.Where(i => (int)i.Division == division.Value).ToList() : items;
        var filteredScoped = FilterByDate(scoped, startDate, endDate);
        Fill(model, filteredScoped, startDate, endDate);

        model.PoolForMe = filteredScoped.Count(i => !i.IsClaimed && !i.IsDeferred && i.StatusText != "Released"
                                            && (!stepId.HasValue || (int)i.CurrentStage == stepId.Value));
        model.MyClaimed = items.Count(i => i.IsClaimed && i.AssignedUserId == userId && i.StatusText != "Released");

        model.PrimaryListTitle = "My Claimed Transactions";
        model.PrimaryList = items.Where(i => i.IsClaimed && i.AssignedUserId == userId && i.StatusText != "Released")
                                 .OrderByDescending(i => i.LastForwardedDate ?? i.DateSubmitted).Take(8).ToList();
        model.RecentList = filteredScoped.OrderByDescending(i => i.LastForwardedDate ?? i.DateSubmitted).Take(8).ToList();
        return View(model);
    }

    // GET: /Dashboard/AssessorHead
    public IActionResult AssessorHead(DateTime? startDate = null, DateTime? endDate = null)
    {
        var allItems = GetAllItems();
        var items = FilterByDate(allItems, startDate, endDate);
        var model = BuildBase("Assessor Head Dashboard", "Office-wide property assessment summary, request trends and today's transactions", nameof(AssessorHead), startDate, endDate);
        Fill(model, items, startDate, endDate);
        FillPropertyCounts(model, startDate, endDate);
        FillTodayTransactions(model, allItems, startDate, endDate);

        return View(model);
    }

    // GET: /Dashboard/SystemOwner
    public IActionResult SystemOwner(DateTime? startDate = null, DateTime? endDate = null)
    {
        var items = FilterByDate(GetAllItems(), startDate, endDate);
        var model = BuildBase("System Owner Dashboard", "System health, users, access rights and recent activity", nameof(SystemOwner), startDate, endDate);
        Fill(model, items, startDate, endDate);
        FillPropertyCounts(model, startDate, endDate);

        model.TotalUsers = _context.UserAccounts.Count();
        model.ActiveUsers = _context.UserAccounts.Count(u => u.IsActive);
        model.TotalRoles = _context.UserRoleSettings.Count(r => r.IsActive);
        model.RolesWithAccess = _context.RoleModulePermissions.Select(p => p.RoleId).Distinct().Count();
        model.TotalOwners = _context.TaxpayerOwners.Count();
        model.ActiveModules = _context.SystemModules.Count(m => m.IsActive);
        model.UsersByRole = _context.UserAccounts.AsEnumerable()
            .GroupBy(u => u.Role)
            .Select(g => new DashboardBreakdownItem { Label = g.Key.ToString(), Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();

        var end = (endDate ?? DateTime.Now).Date;
        var start = (startDate ?? end.AddDays(-13)).Date;
        if (start > end) (start, end) = (end, start);

        var logs = _context.DocumentForwardingLogs
            .Where(l => l.Timestamp >= start && l.Timestamp < end.AddDays(1)).ToList();

        int spanDays = (int)(end - start).TotalDays + 1;
        if (spanDays <= 60)
        {
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                model.ActivityLabels.Add(day.ToString("MMM dd"));
                model.ActivityTrend.Add(logs.Count(l => l.Timestamp.Date == day));
            }
        }
        else
        {
            for (var m = new DateTime(start.Year, start.Month, 1); m <= end; m = m.AddMonths(1))
            {
                var nextM = m.AddMonths(1);
                model.ActivityLabels.Add(m.ToString("MMM yyyy"));
                model.ActivityTrend.Add(logs.Count(l => l.Timestamp >= m && l.Timestamp < nextM));
            }
        }

        model.TopHandlers = logs.Where(l => !string.IsNullOrWhiteSpace(l.ForwardedByName))
            .GroupBy(l => l.ForwardedByName)
            .Select(g => new DashboardBreakdownItem { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(6).ToList();
        model.RecentActivity = logs.OrderByDescending(l => l.Timestamp).Take(10).ToList();
        return View(model);
    }

    private List<WorkflowQueueItemDto> GetAllItems() =>
        _workflow.GetUnifiedQueue(divisionId: null, mode: null).ToList();

    private static List<WorkflowQueueItemDto> FilterByDate(List<WorkflowQueueItemDto> items, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue)
            items = items.Where(i => i.DateSubmitted.Date >= startDate.Value.Date).ToList();
        if (endDate.HasValue)
            items = items.Where(i => i.DateSubmitted.Date <= endDate.Value.Date).ToList();
        return items;
    }

    private static void FillTodayTransactions(DashboardViewModel m, List<WorkflowQueueItemDto> allItems, DateTime? startDate = null, DateTime? endDate = null)
    {
        bool hasFilter = startDate.HasValue || endDate.HasValue;
        DateTime start;
        DateTime end;

        if (hasFilter)
        {
            start = (startDate ?? DateTime.MinValue).Date;
            end = (endDate ?? DateTime.MaxValue).Date;
            if (start > end) (start, end) = (end, start);
        }
        else
        {
            start = DateTime.Now.Date;
            end = DateTime.Now.Date;
        }

        // Filter transactions submitted within date range or updated/forwarded within date range
        var matchedItems = allItems
            .Where(i =>
            {
                var submitted = i.DateSubmitted.Date;
                var forwarded = i.LastForwardedDate?.Date;
                bool matchSubmitted = submitted >= start && submitted <= end;
                bool matchForwarded = forwarded.HasValue && forwarded.Value >= start && forwarded.Value <= end;
                return matchSubmitted || matchForwarded;
            })
            .OrderByDescending(i => i.LastForwardedDate ?? i.DateSubmitted)
            .ToList();

        foreach (var item in matchedItems)
        {
            int stageId = (int)item.CurrentStage;
            bool isReleasingOrPrintingStage = stageId is 9 or 10 or 19 or 30;
            bool isReleased = item.StatusText == "Released" || item.CurrentStage == WorkflowStage.Completed;
            bool isApprovedUnclaimedInReleasing = !isReleased && !item.IsDeferred && isReleasingOrPrintingStage && !item.IsClaimed;

            if (isReleased)
            {
                // Released: all transaction released
                m.TodayReleasedList.Add(item);
            }
            else if (isApprovedUnclaimedInReleasing)
            {
                // Approved: all transaction approved but not claimed in releasing
                m.TodayApprovedList.Add(item);
            }
            else if (item.IsClaimed)
            {
                // On Going: all transaction already claimed and not approved/released
                m.TodayOnGoingList.Add(item);
            }
            else
            {
                // Pending: all transaction submitted that no one has claimed and not approved
                m.TodayPendingList.Add(item);
            }
        }
    }

    private void FillPropertyCounts(DashboardViewModel m, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.TaxDeclarations.AsQueryable();
        if (startDate.HasValue)
        {
            var s = startDate.Value.Date;
            query = query.Where(t => (t.DateEncoded ?? t.DateAssessed) >= s);
        }
        if (endDate.HasValue)
        {
            var e = endDate.Value.Date.AddDays(1);
            query = query.Where(t => (t.DateEncoded ?? t.DateAssessed) < e);
        }

        var list = query.Select(t => new { t.Kind, t.Status, t.IsCancelledAndSuperseded }).ToList();
        m.TotalTaxDeclarations = list.Count;
        m.TotalCancelledTD = list.Count(t => t.IsCancelledAndSuperseded || t.Status == AssessmentStatus.Cancelled);
        m.TotalLandTD = list.Count(t => t.Kind == PropertyKind.Land);
        m.TotalBuildingTD = list.Count(t => t.Kind == PropertyKind.Building);
        m.TotalMachineryTD = list.Count(t => t.Kind == PropertyKind.Machinery);
    }

    private DashboardViewModel BuildBase(string title, string subtitle, string actionName, DateTime? startDate, DateTime? endDate) => new()
    {
        Title = title,
        Subtitle = subtitle,
        ActionName = actionName,
        StartDate = startDate,
        EndDate = endDate,
        FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity?.Name ?? "",
        RoleName = User.FindFirst("WorkflowRoleName")?.Value is { Length: > 0 } wr ? wr : (User.FindFirst(ClaimTypes.Role)?.Value ?? "")
    };

    private static void Fill(DashboardViewModel m, List<WorkflowQueueItemDto> items, DateTime? startDate, DateTime? endDate)
    {
        m.Total = items.Count;
        m.Pending = items.Count(i => i.StatusText == "Pending");
        m.OnGoing = items.Count(i => i.StatusText == "On Going");
        m.ForApproval = items.Count(i => i.StatusText == "For Approval");
        m.Deferred = items.Count(i => i.StatusText == "Deferred");
        m.Released = items.Count(i => i.StatusText == "Released");

        var now = DateTime.Now;
        var open = items.Where(i => i.StatusText != "Released").ToList();
        var released = items.Where(i => i.StatusText == "Released").ToList();
        m.TotalFees = items.Sum(i => i.TotalAmount);
        m.ReleasedFees = released.Sum(i => i.TotalAmount);

        int Age(WorkflowQueueItemDto i) => (int)(now - i.DateSubmitted).TotalDays;
        m.AgingBuckets = new()
        {
            new() { Label = "0-3 days", Count = open.Count(i => Age(i) <= 3) },
            new() { Label = "4-7 days", Count = open.Count(i => Age(i) is >= 4 and <= 7) },
            new() { Label = "8-14 days", Count = open.Count(i => Age(i) is >= 8 and <= 14) },
            new() { Label = "15+ days", Count = open.Count(i => Age(i) >= 15) }
        };
        m.Bottlenecks = open.GroupBy(i => i.CurrentStageName)
            .Select(g => new DashboardBreakdownItem { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(6).ToList();

        // Trend range: use date filter if set, otherwise last 14 days
        var end = (endDate ?? now).Date;
        var start = (startDate ?? end.AddDays(-13)).Date;
        if (start > end) (start, end) = (end, start);

        int spanDays = (int)(end - start).TotalDays + 1;
        if (spanDays <= 60)
        {
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                m.TrendLabels.Add(day.ToString("MMM dd"));
                m.TrendSubmitted.Add(items.Count(i => i.DateSubmitted.Date == day));
                m.TrendReleased.Add(released.Count(i => (i.LastForwardedDate ?? i.DateSubmitted).Date == day));
                m.TrendTaxDec.Add(items.Count(i => i.RequestType == "TaxDeclarationRequest" && i.DateSubmitted.Date == day));
                m.TrendTaxMapping.Add(items.Count(i => i.RequestType == "TaxMappingRequest" && i.DateSubmitted.Date == day));
                m.TrendLandHolding.Add(items.Count(i => i.RequestType == "LandHoldingRequest" && i.DateSubmitted.Date == day));
                m.TrendPropertyAssessment.Add(items.Count(i => i.RequestType == "PropertyAssessmentRequest" && i.DateSubmitted.Date == day));
                m.TrendPropertyInspection.Add(items.Count(i => i.RequestType == "RequestForInspection" && i.DateSubmitted.Date == day));
            }
        }
        else
        {
            for (var mo = new DateTime(start.Year, start.Month, 1); mo <= end; mo = mo.AddMonths(1))
            {
                var nextMo = mo.AddMonths(1);
                m.TrendLabels.Add(mo.ToString("MMM yyyy"));
                m.TrendSubmitted.Add(items.Count(i => i.DateSubmitted >= mo && i.DateSubmitted < nextMo));
                m.TrendReleased.Add(released.Count(i => (i.LastForwardedDate ?? i.DateSubmitted) >= mo && (i.LastForwardedDate ?? i.DateSubmitted) < nextMo));
                m.TrendTaxDec.Add(items.Count(i => i.RequestType == "TaxDeclarationRequest" && i.DateSubmitted >= mo && i.DateSubmitted < nextMo));
                m.TrendTaxMapping.Add(items.Count(i => i.RequestType == "TaxMappingRequest" && i.DateSubmitted >= mo && i.DateSubmitted < nextMo));
                m.TrendLandHolding.Add(items.Count(i => i.RequestType == "LandHoldingRequest" && i.DateSubmitted >= mo && i.DateSubmitted < nextMo));
                m.TrendPropertyAssessment.Add(items.Count(i => i.RequestType == "PropertyAssessmentRequest" && i.DateSubmitted >= mo && i.DateSubmitted < nextMo));
                m.TrendPropertyInspection.Add(items.Count(i => i.RequestType == "RequestForInspection" && i.DateSubmitted >= mo && i.DateSubmitted < nextMo));
            }
        }

        m.ByDivision = items.GroupBy(i => i.Division)
            .Select(g => new DashboardBreakdownItem { Label = DivisionLabel(g.Key), Count = g.Count() })
            .OrderBy(x => x.Label).ToList();
        m.ByRequestType = items.GroupBy(i => string.IsNullOrEmpty(i.DisplayType) ? i.RequestType : i.DisplayType)
            .Select(g => new DashboardBreakdownItem { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();
    }

    private static string DivisionLabel(DivisionType d) => d switch
    {
        DivisionType.Division1_TaxAssessmentAndLandHolding => "Division 1: Tax Declaration & Land Holding",
        DivisionType.Division2_GIS_TaxMapping => "Division 2: GIS Tax Mapping",
        DivisionType.Division3_RecordsAndCertification => "Division 3: Records & Certification",
        _ => d.ToString()
    };
}
