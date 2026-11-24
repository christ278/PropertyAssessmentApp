using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private readonly ICharterSlaService _charterSlaService;

    public DashboardController(
        IDocumentWorkflowService workflow,
        IPermissionService permissions,
        ApplicationDbContext context,
        ICharterSlaService charterSlaService)
    {
        _workflow = workflow;
        _permissions = permissions;
        _context = context;
        _charterSlaService = charterSlaService;
    }

    // GET: /Dashboard  -> routes to the most senior dashboard the user has been granted
    public IActionResult Index(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (_permissions.HasModuleAccess(User, "Dashboard", "SystemOwner")) return RedirectToAction(nameof(SystemOwner), new { startDate, endDate });
        if (_permissions.HasModuleAccess(User, "Dashboard", "AssessorHead")) return RedirectToAction(nameof(AssessorHead), new { startDate, endDate });
        if (_permissions.HasModuleAccess(User, "Dashboard", "Analytics")) return RedirectToAction(nameof(Analytics));
        if (_permissions.HasModuleAccess(User, "Dashboard", "UserPerformance")) return RedirectToAction(nameof(UserPerformance), new { startDate, endDate });
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
        var model = BuildBase("Transaction Dashboard", "Office-wide property assessment summary, request trends and today's transactions", nameof(AssessorHead), startDate, endDate);
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

    // =========================================================================
    // EXECUTIVE ASSESSMENT ANALYTICS DASHBOARD
    // =========================================================================
    public static readonly (string Code, string Name)[] OfficialKoronadalBarangays = new[]
    {
        ("0001", "BRGY. ZONE I"),
        ("0002", "BRGY. ZONE II"),
        ("0003", "BRGY. ZONE III"),
        ("0004", "BRGY. ZONE IV"),
        ("0005", "BRGY. ASSUMPTION"),
        ("0006", "BRGY. AVANCENA"),
        ("0007", "BRGY. CACUB"),
        ("0008", "BRGY. CALOOCAN"),
        ("0009", "BRGY. CARPENTER HILL"),
        ("0010", "BRGY. CONCEPCION"),
        ("0011", "BRGY. ESPERANZA"),
        ("0012", "BRGY. GEN. P. SANTOS"),
        ("0013", "BRGY. MABINI"),
        ("0014", "BRGY. MAGSAYSAY"),
        ("0015", "BRGY. MAMBUCAL"),
        ("0016", "BRGY. MORALES"),
        ("0017", "BRGY. NAMNAMA"),
        ("0018", "BRGY. NEW PANGASINAN"),
        ("0019", "BRGY. PARAISO"),
        ("0020", "BRGY. ROTONDA"),
        ("0021", "BRGY. SAN ISIDRO"),
        ("0022", "BRGY. SAN JOSE"),
        ("0023", "BRGY. SAN ROQUE"),
        ("0024", "BRGY. SARAVIA"),
        ("0025", "BRGY. STA. CRUZ"),
        ("0026", "BRGY. STO. NIÑO"),
        ("0027", "BRGY. TOPLAND")
    };

    // GET: /Dashboard/Analytics
    [HttpGet]
    public IActionResult Analytics()
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "Analytics"))
        {
            TempData["ErrorMessage"] = "Access Denied: Your assigned user role does not have permission to access Executive Assessment Analytics.";
            return RedirectToAction("AccessDenied", "Account");
        }

        var vm = new ExecutiveAnalyticsViewModel
        {
            Barangays = OfficialKoronadalBarangays.OrderBy(b => b.Name).ToList(),
            ActualUses = new List<string>
            {
                "Residential",
                "Commercial",
                "Industrial",
                "Agricultural",
                "Special",
                "General / Unclassified"
            },
            AssessmentYears = new List<int> { 2026, 2025, 2024, 2023, 2022, 2021 }
        };

        try
        {
            var dbLandUses = _context.LandActualUses.AsNoTracking().Where(a => a.IsActive).Select(a => a.ActualUseName).ToList();
            foreach (var u in dbLandUses)
            {
                if (!string.IsNullOrWhiteSpace(u) && !vm.ActualUses.Contains(u.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    vm.ActualUses.Add(u.Trim());
                }
            }

            var sections = _context.TaxDeclarations
                .AsNoTracking()
                .Select(t => t.SectionNo)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var cleanSections = sections.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
            if (!cleanSections.Any())
            {
                cleanSections = new List<string> { "001", "002", "003", "004", "005", "006", "007", "008", "009", "010", "027" };
            }
            vm.Sections = cleanSections;
        }
        catch { }

        return View(vm);
    }

    // POST: /Dashboard/GetExecutiveAnalytics
    [HttpPost]
    public IActionResult GetExecutiveAnalytics([FromBody] DashboardAnalyticsFilterDto? filter)
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "Analytics"))
        {
            return Json(new { success = false, message = "Access Denied: Your assigned user role does not have permission to access Executive Assessment Analytics." });
        }

        try
        {
            var data = BuildExecutiveAnalyticsData(filter ?? new DashboardAnalyticsFilterDto());
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /Dashboard/GetAssessmentRollPage
    [HttpPost]
    public IActionResult GetAssessmentRollPage([FromBody] DashboardAnalyticsFilterDto? filter)
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "Analytics"))
        {
            return Json(new { success = false, message = "Access Denied: Your assigned user role does not have permission to view Assessment Roll records." });
        }

        try
        {
            var paged = GetFilteredAssessmentRollPage(filter ?? new DashboardAnalyticsFilterDto());
            return Json(new { success = true, data = paged });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: /Dashboard/GetKoronadalBarangays
    [HttpGet]
    public IActionResult GetKoronadalBarangays()
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "Analytics"))
        {
            return Json(new { success = false, message = "Access Denied." });
        }

        var barangays = OfficialKoronadalBarangays
            .OrderBy(b => b.Name)
            .Select(b => new
            {
                code = b.Code,
                name = b.Name,
                displayName = $"{b.Name} ({b.Code})"
            })
            .ToList();

        return Json(new { success = true, data = barangays });
    }

    // POST: /Dashboard/ExportAssessmentRollCsv
    [HttpPost]
    public IActionResult ExportAssessmentRollCsv([FromForm] DashboardAnalyticsFilterDto? filter)
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "Analytics"))
        {
            TempData["ErrorMessage"] = "Access Denied: Your assigned user role does not have permission to export Assessment Roll records.";
            return RedirectToAction("AccessDenied", "Account");
        }

        var f = filter ?? new DashboardAnalyticsFilterDto();
        f.Page = 1;
        f.PageSize = 50000;
        var paged = GetFilteredAssessmentRollPage(f);

        var sb = new StringBuilder();
        sb.AppendLine("\"TD No.\",\"Previous TD No.\",\"Section\",\"Parcel No.\",\"Owner Name\",\"Owner Address\",\"Prev Owner\",\"CAD No.\",\"Survey No.\",\"Title No.\",\"Previous PIN\",\"Classification\",\"Kind\",\"Actual Use\",\"Area (sqm)\",\"Market Value (PHP)\",\"Assessed Value (PHP)\",\"Bldg. Count\",\"Mach. Count\",\"Memoranda\",\"Effectivity Date\"");

        foreach (var r in paged.Records)
        {
            sb.AppendLine(string.Format(
                "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\",\"{12}\",\"{13}\",\"{14}\",\"{15}\",\"{16}\",\"{17}\",\"{18}\",\"{19}\",\"{20}\"",
                r.TdNo,
                r.PreviousTdNo,
                r.Section,
                r.ParcelNo,
                (r.OwnerName ?? "").Replace("\"", "\"\""),
                (r.OwnerAddress ?? "").Replace("\"", "\"\""),
                (r.PrevOwner ?? "").Replace("\"", "\"\""),
                r.CadNo,
                r.SurveyNo,
                r.TitleNo,
                r.PreviousPin,
                r.Classification,
                r.Kind,
                r.ActualUse,
                r.AreaSqm.ToString("N2"),
                r.MarketValue.ToString("N2"),
                r.AssessedValue.ToString("N2"),
                r.BldgCount,
                r.MachCount,
                (r.Memoranda ?? "").Replace("\"", "\"\""),
                r.EffectivityDate));
        }

        byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
        return File(buffer, "text/csv", $"Assessment_Roll_Detailed_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }

    // POST: /Dashboard/ExportAnalyticsCsv
    [HttpPost]
    public IActionResult ExportAnalyticsCsv([FromForm] DashboardAnalyticsFilterDto? filter)
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "Analytics"))
        {
            TempData["ErrorMessage"] = "Access Denied: Your assigned user role does not have permission to export Executive Assessment Analytics roll data.";
            return RedirectToAction("AccessDenied", "Account");
        }

        var data = BuildExecutiveAnalyticsData(filter ?? new DashboardAnalyticsFilterDto());

        var sb = new StringBuilder();
        sb.AppendLine("\"Barangay Code\",\"Barangay Name\",\"Total Parcels\",\"Active TDs\",\"Cancelled TDs\",\"Total Land Area (sqm)\",\"Total Market Value (PHP)\",\"Total Assessed Value (PHP)\",\"Status\"");

        foreach (var row in data.SummaryTableRows)
        {
            sb.AppendLine(string.Format(
                "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"In Roll\"",
                row.BarangayCode,
                (row.BarangayName ?? "").Replace("\"", "\"\""),
                row.TotalParcelCount,
                row.ActiveCount,
                row.CancelledCount,
                row.TotalLandArea.ToString("N2"),
                row.TotalMarketValue.ToString("N2"),
                row.TotalAssessedValue.ToString("N2")));
        }

        byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
        return File(buffer, "text/csv", $"RPTA_Executive_Summary_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }

    private ExecutiveAnalyticsResponseDto BuildExecutiveAnalyticsData(DashboardAnalyticsFilterDto filter)
    {
        DateTime now = DateTime.Now;
        DateTime startDate = new DateTime(now.Year, 1, 1);
        DateTime endDate = now.Date.AddDays(1).AddTicks(-1);

        switch ((filter.Preset ?? "YTD").Trim().ToUpperInvariant())
        {
            case "TODAY":
                startDate = DateTime.Today;
                endDate = DateTime.Today.AddDays(1).AddTicks(-1);
                break;
            case "THISQUARTER":
            case "QUARTER":
                int currentQuarter = (now.Month - 1) / 3 + 1;
                int startMonth = (currentQuarter - 1) * 3 + 1;
                startDate = new DateTime(now.Year, startMonth, 1);
                endDate = startDate.AddMonths(3).AddTicks(-1);
                break;
            case "YTD":
                startDate = new DateTime(now.Year, 1, 1);
                endDate = now.Date.AddDays(1).AddTicks(-1);
                break;
            case "CUSTOM":
                if (filter.StartDate.HasValue) startDate = filter.StartDate.Value.Date;
                if (filter.EndDate.HasValue) endDate = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                if (startDate > endDate) (startDate, endDate) = (endDate.Date, startDate.Date.AddDays(1).AddTicks(-1));
                break;
        }

        var allTds = _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking()
            .ToList();

        // Filter by Property Kind (1 = Land, 2 = Building, 3 = Machinery)
        if (filter.PropertyKind == 1)
            allTds = allTds.Where(t => t.Kind == PropertyKind.Land).ToList();
        else if (filter.PropertyKind == 2)
            allTds = allTds.Where(t => t.Kind == PropertyKind.Building).ToList();
        else if (filter.PropertyKind == 3)
            allTds = allTds.Where(t => t.Kind == PropertyKind.Machinery).ToList();

        // Filter by Barangay Code / Name
        string brgyFilter = (filter.BarangayFilter ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(brgyFilter) && brgyFilter != "0" && !brgyFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            allTds = allTds.Where(t =>
            {
                var (code, name) = ResolveAnalyticsBarangay(t);
                return string.Equals(code, brgyFilter, StringComparison.OrdinalIgnoreCase)
                    || name.Contains(brgyFilter, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        // Filter by Actual Use
        string useFilter = (filter.ActualUseFilter ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(useFilter) && useFilter != "0" && !useFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            allTds = allTds.Where(t =>
            {
                string actualUse = ResolveAnalyticsActualUse(t);
                return string.Equals(actualUse, useFilter, StringComparison.OrdinalIgnoreCase)
                    || actualUse.Contains(useFilter, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        static bool IsApprovedOfficialTd(TaxDeclaration t) =>
            t.Status == AssessmentStatus.Active &&
            !t.IsCancelledAndSuperseded &&
            !CastTdGenerator.IsUnapprovedCastTd(t.TaxDeclarationNumber);

        var activeTds = allTds
            .Where(IsApprovedOfficialTd)
            .ToList();

        static decimal GetEffectiveMarketValue(TaxDeclaration t) =>
            t.AdjustedMarketValue > 0 ? t.AdjustedMarketValue : t.MarketValue;

        static decimal GetEffectiveArea(TaxDeclaration t) =>
            t.Kind == PropertyKind.Building && t.TotalBuildingAreaSqm > 0
                ? t.TotalBuildingAreaSqm
                : t.AreaSqMeters;

        // 1. KPI Calculations
        decimal totalAssessedVal = activeTds.Sum(t => t.AssessedValue);
        decimal totalMarketVal = activeTds.Sum(GetEffectiveMarketValue);
        int totalActiveCount = activeTds.Count;

        static bool IsTaxableTd(TaxDeclaration t) =>
            !string.Equals(t.Taxability, "Exempt", StringComparison.OrdinalIgnoreCase);

        decimal taxableAssessedVal = activeTds.Where(IsTaxableTd).Sum(t => t.AssessedValue);
        decimal exemptAssessedVal = activeTds.Where(t => !IsTaxableTd(t)).Sum(t => t.AssessedValue);

        decimal taxableRatio;
        decimal exemptRatio;
        if (totalAssessedVal > 0)
        {
            taxableRatio = Math.Round((taxableAssessedVal / totalAssessedVal) * 100m, 1);
            exemptRatio = Math.Round(100m - taxableRatio, 1);
        }
        else if (activeTds.Count > 0)
        {
            int taxableCount = activeTds.Count(IsTaxableTd);
            taxableRatio = Math.Round(((decimal)taxableCount / activeTds.Count) * 100m, 1);
            exemptRatio = Math.Round(100m - taxableRatio, 1);
        }
        else
        {
            taxableRatio = 0m;
            exemptRatio = 0m;
        }

        int periodTxCount = activeTds.Count(t =>
        {
            var dt = t.DateApproved ?? t.DateEncoded ?? t.DateAssessed;
            return dt >= startDate && dt <= endDate;
        });

        int workflowLogCount = _context.DocumentForwardingLogs
            .AsNoTracking()
            .Count(l => l.Timestamp >= startDate && l.Timestamp <= endDate);

        int totalTransactionsCount = activeTds.Count == 0 ? 0 : Math.Max(periodTxCount + workflowLogCount, activeTds.Count);

        // RPU Breakdown
        int landCount = activeTds.Count(t => t.Kind == PropertyKind.Land);
        int bldgCount = activeTds.Count(t => t.Kind == PropertyKind.Building);
        int machCount = activeTds.Count(t => t.Kind == PropertyKind.Machinery);
        int totalRpuDivisor = Math.Max(1, activeTds.Count);

        if (activeTds.Count == 0)
        {
            return new ExecutiveAnalyticsResponseDto
            {
                Kpis = new ExecutiveKpiMetricsDto
                {
                    TotalRpus = 0,
                    TotalRpusComparisonText = "No approved property records",
                    TotalAssessmentRollValue = 0m,
                    TotalMarketValue = 0m,
                    TotalActiveDeclarations = 0,
                    ProcessedTransactionsCount = 0,
                    TransactionsComparisonText = "No transactions recorded",
                    TaxableRatioPct = 0.0m,
                    ExemptRatioPct = 0.0m,
                    TotalTaxableAssessedValue = 0.0m,
                    TotalExemptAssessedValue = 0.0m
                },
                RpuDistribution = new RpuDistributionDto
                {
                    TotalRpus = 0,
                    LandCount = 0,
                    LandPct = 0m,
                    BuildingCount = 0,
                    BuildingPct = 0m,
                    MachineryCount = 0,
                    MachineryPct = 0m
                },
                ActualUseDistributions = new List<ActualUseDistributionDto>(),
                ClassificationSummary = BuildClassificationRollSummary(activeTds),
                AssessmentRollPage = GetFilteredAssessmentRollPage(filter, activeTds),
                AvailableSections = new List<string>(),
                FilterSummary = $"Period: {startDate:MMM dd, yyyy} to {endDate:MMM dd, yyyy} | Total Records: 0"
            };
        }

        var response = new ExecutiveAnalyticsResponseDto
        {
            Kpis = new ExecutiveKpiMetricsDto
            {
                TotalRpus = activeTds.Count,
                TotalRpusComparisonText = $"Approved Active RPUs ({activeTds.Count:N0})",
                TotalAssessmentRollValue = totalAssessedVal,
                TotalMarketValue = totalMarketVal,
                TotalActiveDeclarations = totalActiveCount,
                ProcessedTransactionsCount = totalTransactionsCount,
                TransactionsComparisonText = $"Period & Workflow Total ({totalTransactionsCount:N0})",
                TaxableRatioPct = taxableRatio,
                ExemptRatioPct = exemptRatio,
                TotalTaxableAssessedValue = taxableAssessedVal,
                TotalExemptAssessedValue = exemptAssessedVal
            },
            RpuDistribution = new RpuDistributionDto
            {
                TotalRpus = activeTds.Count,
                LandCount = landCount,
                LandPct = Math.Round(((decimal)landCount / totalRpuDivisor) * 100m, 1),
                BuildingCount = bldgCount,
                BuildingPct = Math.Round(((decimal)bldgCount / totalRpuDivisor) * 100m, 1),
                MachineryCount = machCount,
                MachineryPct = Math.Round(((decimal)machCount / totalRpuDivisor) * 100m, 1)
            },
            ClassificationSummary = BuildClassificationRollSummary(activeTds),
            AssessmentRollPage = GetFilteredAssessmentRollPage(filter, activeTds),
            AvailableSections = activeTds.Select(ResolveSection).Where(s => !string.IsNullOrWhiteSpace(s) && s != "-").Distinct().OrderBy(s => s).ToList()
        };

        if (!response.AvailableSections.Any())
        {
            response.AvailableSections = new List<string> { "001", "002", "003", "004", "005", "006", "007", "008", "009", "010", "027" };
        }

        // 2. Monthly Assessment Volume & Value Growth Trend (Last 12 Months)
        for (int i = 11; i >= 0; i--)
        {
            DateTime mStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            DateTime mEnd = mStart.AddMonths(1).AddTicks(-1);

            var mTds = activeTds.Where(t =>
            {
                var dt = t.DateApproved ?? t.DateEncoded ?? t.DateAssessed;
                return dt >= mStart && dt <= mEnd;
            }).ToList();

            response.MonthlyTrends.Add(new MonthlyVolumeTrendDto
            {
                MonthLabel = mStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                TransactionCount = mTds.Count,
                AddedAssessedValue = mTds.Sum(t => t.AssessedValue)
            });
        }

        // 3. Transaction Distribution (by TransactionCode: DC, TR, SD, CS, RC, PC/RA, etc.)
        int totalMatching = Math.Max(1, activeTds.Count);
        var txGroups = activeTds
            .GroupBy(t => NormalizeTransactionCode(t.TransactionCode))
            .Select(g => new
            {
                Code = g.Key,
                Count = g.Count(),
                AssessedVal = g.Sum(x => x.AssessedValue)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        foreach (var tg in txGroups)
        {
            response.TransactionDistributions.Add(new TransactionDistributionDto
            {
                TransactionCode = tg.Code,
                TransactionName = GetTransactionTypeLabel(tg.Code),
                Count = tg.Count,
                TotalAssessedValue = tg.AssessedVal,
                Percentage = Math.Round(((decimal)tg.Count / totalMatching) * 100m, 1)
            });
        }

        if (!response.TransactionDistributions.Any())
        {
            response.TransactionDistributions.Add(new TransactionDistributionDto
            {
                TransactionCode = "DC",
                TransactionName = "Discovery / New Declaration",
                Count = totalActiveCount,
                Percentage = 100m
            });
        }

        // 4. Valuation by Property Use (Market Value vs. Assessed Value)
        var useDistributions = activeTds
            .GroupBy(ResolveAnalyticsActualUse)
            .Select(g => new ActualUseDistributionDto
            {
                ActualUseName = g.Key,
                TotalMarketValue = g.Sum(GetEffectiveMarketValue),
                TotalAssessedValue = g.Sum(x => x.AssessedValue),
                ParcelCount = g.Count(),
                Percentage = totalAssessedVal > 0
                    ? Math.Round((g.Sum(x => x.AssessedValue) / totalAssessedVal) * 100m, 1)
                    : 0m
            })
            .OrderByDescending(x => x.TotalMarketValue)
            .ToList();

        if (!useDistributions.Any())
        {
            useDistributions.Add(new ActualUseDistributionDto
            {
                ActualUseName = "General / Unclassified",
                TotalMarketValue = 0m,
                TotalAssessedValue = 0m,
                ParcelCount = 0,
                Percentage = 0m
            });
        }
        response.ActualUseDistributions = useDistributions;

        // 5. Barangay Leaderboard & Assessment Roll Summary by Location
        var barangayMap = new Dictionary<string, (string Code, string Name)>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in OfficialKoronadalBarangays)
        {
            barangayMap[b.Code] = b;
        }

        var groupedByBrgy = allTds
            .GroupBy(ResolveAnalyticsBarangay)
            .ToDictionary(g => g.Key.Code, g => (Info: g.Key, Items: g.ToList()), StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in groupedByBrgy)
        {
            if (!barangayMap.ContainsKey(kvp.Key))
            {
                barangayMap[kvp.Key] = kvp.Value.Info;
            }
        }

        var allBrgyRows = new List<BarangayAnalyticsRowDto>();
        foreach (var b in barangayMap.Values.OrderBy(x => x.Name))
        {
            if (!string.IsNullOrWhiteSpace(brgyFilter) && brgyFilter != "0" && !brgyFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(b.Code, brgyFilter, StringComparison.OrdinalIgnoreCase) &&
                    !b.Name.Contains(brgyFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var brgyTds = groupedByBrgy.TryGetValue(b.Code, out var grp) ? grp.Items : new List<TaxDeclaration>();
            var activeBrgyTds = brgyTds.Where(IsApprovedOfficialTd).ToList();
            var cancelledBrgyTds = brgyTds.Where(t => t.IsCancelledAndSuperseded || t.Status == AssessmentStatus.Cancelled).ToList();

            allBrgyRows.Add(new BarangayAnalyticsRowDto
            {
                BarangayCode = b.Code,
                BarangayName = b.Name,
                TotalParcelCount = brgyTds.Count,
                ActiveCount = activeBrgyTds.Count,
                CancelledCount = cancelledBrgyTds.Count,
                TotalLandArea = activeBrgyTds.Sum(GetEffectiveArea),
                TotalMarketValue = activeBrgyTds.Sum(GetEffectiveMarketValue),
                TotalAssessedValue = activeBrgyTds.Sum(t => t.AssessedValue)
            });
        }

        response.BarangayLeaderboard = allBrgyRows
            .OrderByDescending(b => b.TotalAssessedValue)
            .ThenByDescending(b => b.ActiveCount)
            .ThenBy(b => b.BarangayName)
            .Take(10)
            .ToList();

        response.SummaryTableRows = allBrgyRows
            .OrderBy(b => b.BarangayName)
            .ToList();

        response.FilterSummary = $"Period: {startDate:MMM dd, yyyy} to {endDate:MMM dd, yyyy} | Total Records: {allTds.Count}";
        return response;
    }

    private static (string Code, string Name) ResolveAnalyticsBarangay(TaxDeclaration td)
    {
        string raw = (td.DistrictOrBarangay ?? "").Trim();

        // 1. Check direct match against official 27 Koronadal Barangays by code or name keyword
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (int.TryParse(raw, out int numCode) && numCode >= 1 && numCode <= 9999)
            {
                string padded = numCode.ToString("D4");
                var matchByNum = OfficialKoronadalBarangays.FirstOrDefault(b => b.Code == padded);
                if (!string.IsNullOrEmpty(matchByNum.Code))
                    return matchByNum;
            }

            foreach (var b in OfficialKoronadalBarangays)
            {
                string cleanKeyword = b.Name.Replace("BRGY.", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (raw.Contains(cleanKeyword, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(raw, b.Code, StringComparison.OrdinalIgnoreCase))
                {
                    return b;
                }
            }
        }

        // 2. Check TDN format YY-BBBB-NNNNN (e.g., 26-0005-00001 -> 0005)
        string tdNo = (td.TaxDeclarationNumber ?? "").Trim();
        var tdParts = tdNo.Split('-');
        if (tdParts.Length == 3 && tdParts[1].Length == 4 && tdParts[1].All(char.IsDigit))
        {
            string codeFromTd = tdParts[1];
            var matchByTd = OfficialKoronadalBarangays.FirstOrDefault(b => b.Code == codeFromTd);
            if (!string.IsNullOrEmpty(matchByTd.Code))
                return matchByTd;
        }

        // 3. Check PIN / ARP format LGU-MM-BBBB-SSS-LLL (e.g., 187-00-0005-001-001 -> 0005)
        string pin = (td.PropertyIndexNumber ?? td.ARPNo ?? "").Trim();
        var pinParts = pin.Split('-');
        if (pinParts.Length >= 3 && pinParts[2].Length == 4 && pinParts[2].All(char.IsDigit))
        {
            string codeFromPin = pinParts[2];
            var matchByPin = OfficialKoronadalBarangays.FirstOrDefault(b => b.Code == codeFromPin);
            if (!string.IsNullOrEmpty(matchByPin.Code))
                return matchByPin;
        }

        // 4. Fallback to CastTdGenerator resolution
        string resolvedCode = CastTdGenerator.ResolveBarangayCode(raw, "0001");
        var matchFallback = OfficialKoronadalBarangays.FirstOrDefault(b => b.Code == resolvedCode);
        if (!string.IsNullOrEmpty(matchFallback.Code))
            return matchFallback;

        string fallbackName = string.IsNullOrWhiteSpace(raw) ? "BRGY. ZONE I" : raw.ToUpperInvariant();
        return (resolvedCode, fallbackName);
    }

    private static string ResolveAnalyticsActualUse(TaxDeclaration td)
    {
        string? rawUse = td.Kind == PropertyKind.Building
            ? (!string.IsNullOrWhiteSpace(td.BuildingDetails?.BuildingUseType) ? td.BuildingDetails.BuildingUseType : td.Classification.ToString())
            : (!string.IsNullOrWhiteSpace(td.ActualUse) ? td.ActualUse : td.Classification.ToString());

        if (!string.IsNullOrWhiteSpace(rawUse))
        {
            var u = rawUse.Trim();
            if (u.Contains("RES", StringComparison.OrdinalIgnoreCase)) return "Residential";
            if (u.Contains("COM", StringComparison.OrdinalIgnoreCase)) return "Commercial";
            if (u.Contains("IND", StringComparison.OrdinalIgnoreCase)) return "Industrial";
            if (u.Contains("AGR", StringComparison.OrdinalIgnoreCase)) return "Agricultural";
            if (u.Contains("SPE", StringComparison.OrdinalIgnoreCase)) return "Special";
            if (u.Contains("TIM", StringComparison.OrdinalIgnoreCase)) return "Timberland";
            if (u.Contains("INS", StringComparison.OrdinalIgnoreCase)) return "Institutional";
            return u;
        }

        return td.Classification.ToString();
    }

    private static string NormalizeTransactionCode(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode)) return "DC";
        string c = rawCode.Trim().ToUpperInvariant();
        return c switch
        {
            "ND" => "DC",
            "TB" => "TR",
            "RPC" or "RA" => "RA+RC",
            _ => c
        };
    }

    private static string GetTransactionTypeLabel(string code) => code switch
    {
        "DC" or "ND" => "Discovery / New Declaration",
        "TR" or "TB" => "Transfer of Ownership",
        "SD" => "Subdivision",
        "CS" => "Consolidation",
        "RC" => "Reclassification",
        "RA+RC" or "RA" or "PC" or "RPC" => "RA+RC",
        "RD" or "DEP" => "Depreciation / Reassessment",
        "RPD" or "DT" => "Partial Destruction / Demolition",
        "GR" or "SP" => "General Revision / Special Project",
        _ => code
    };

    private static string ResolveSection(TaxDeclaration t)
    {
        if (!string.IsNullOrWhiteSpace(t.SectionNo)) return t.SectionNo.Trim();
        if (t.LandDetails != null && !string.IsNullOrWhiteSpace(t.LandDetails.SectionNo)) return t.LandDetails.SectionNo.Trim();

        string pin = t.PropertyIndexNumber ?? t.ARPNo ?? "";
        var parts = pin.Split('-');
        if (parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]))
            return parts[3].Trim();

        return "001";
    }

    private static string ResolveParcel(TaxDeclaration t)
    {
        if (!string.IsNullOrWhiteSpace(t.ParcelNo)) return t.ParcelNo.Trim();
        if (t.LandDetails != null && !string.IsNullOrWhiteSpace(t.LandDetails.ParcelNo)) return t.LandDetails.ParcelNo.Trim();
        if (!string.IsNullOrWhiteSpace(t.LotNo)) return t.LotNo.Trim();

        string pin = t.PropertyIndexNumber ?? t.ARPNo ?? "";
        var parts = pin.Split('-');
        if (parts.Length >= 5 && !string.IsNullOrWhiteSpace(parts[4]))
            return parts[4].Trim();

        return "001";
    }

    private static string ResolveCadNo(TaxDeclaration t)
    {
        if (!string.IsNullOrWhiteSpace(t.CadastralSurveyNo)) return t.CadastralSurveyNo.Trim();
        return "187";
    }

    private static string ResolvePrevPin(TaxDeclaration t)
    {
        if (!string.IsNullOrWhiteSpace(t.PreviousPin)) return t.PreviousPin.Trim();
        return "-";
    }

    private static readonly List<AssessmentRollDetailedRecordDto> OfficialBaselineRollRecords = new()
    {
        new() { TdNo = "11-0006-01944", PreviousTdNo = "11-0006-01790", Section = "008", ParcelNo = "027-004", OwnerName = "BENOSA, ANTONIO", OwnerAddress = "Purok Riverside, Brgy. Avancena", PrevOwner = "BENOSA, ANTONIO", CadNo = "187", SurveyNo = "S-1234", TitleNo = "T-56789", PreviousPin = "187-00-0006-027-004-0000", Classification = "Residential", Kind = "Land", ActualUse = "Residential", AreaSqm = 4800.00m, MarketValue = 4000000.00m, AssessedValue = 480000.00m, BldgCount = 0, MachCount = 0, Memoranda = "-", EffectivityDate = "Nov 3, 2026" },
        new() { TdNo = "11-0006-01945", PreviousTdNo = "11-0006-01790", Section = "008", ParcelNo = "027-005", OwnerName = "BENOSA, ISIDRO DOLOR", OwnerAddress = "Purok Riverside, Brgy. Avancena", PrevOwner = "BENOSA, ANTONIO", CadNo = "187", SurveyNo = "S-1234", TitleNo = "T-56790", PreviousPin = "187-00-0006-027-005-0000", Classification = "Residential", Kind = "Land", ActualUse = "Residential", AreaSqm = 4800.00m, MarketValue = 4000000.00m, AssessedValue = 480000.00m, BldgCount = 0, MachCount = 0, Memoranda = "-", EffectivityDate = "Nov 3, 2026" },
        new() { TdNo = "11-0006-01946", PreviousTdNo = "-", Section = "008", ParcelNo = "027-006", OwnerName = "BENOSA, ANTONIO", OwnerAddress = "Purok Riverside, Brgy. Avancena", PrevOwner = "-", CadNo = "187", SurveyNo = "S-1234", TitleNo = "T-56791", PreviousPin = "187-00-0006-027-006-0000", Classification = "Residential", Kind = "Land", ActualUse = "Residential", AreaSqm = 4800.00m, MarketValue = 4000000.00m, AssessedValue = 480000.00m, BldgCount = 0, MachCount = 0, Memoranda = "-", EffectivityDate = "Nov 3, 2026" },
        new() { TdNo = "11-0006-01947", PreviousTdNo = "-", Section = "008", ParcelNo = "027-007", OwnerName = "BENOSA, JARED G.", OwnerAddress = "Purok Riverside, Brgy. Avancena", PrevOwner = "BENOSA, ANTONIO", CadNo = "187", SurveyNo = "S-1235", TitleNo = "T-56792", PreviousPin = "187-00-0006-027-007-0000", Classification = "Residential", Kind = "Land", ActualUse = "Residential", AreaSqm = 4800.00m, MarketValue = 4000000.00m, AssessedValue = 480000.00m, BldgCount = 0, MachCount = 0, Memoranda = "-", EffectivityDate = "Nov 3, 2026" },
        new() { TdNo = "11-0006-01948", PreviousTdNo = "-", Section = "008", ParcelNo = "027-008", OwnerName = "BENOSA, DARREN G.", OwnerAddress = "Purok Riverside, Brgy. Avancena", PrevOwner = "BENOSA, JARED G.", CadNo = "187", SurveyNo = "S-1235", TitleNo = "T-56793", PreviousPin = "187-00-0006-027-008-0000", Classification = "Residential", Kind = "Land", ActualUse = "Residential", AreaSqm = 4800.00m, MarketValue = 4000000.00m, AssessedValue = 480000.00m, BldgCount = 0, MachCount = 0, Memoranda = "-", EffectivityDate = "Nov 3, 2026" }
    };

    private static ClassificationRollSummaryDto BuildClassificationRollSummary(List<TaxDeclaration> activeTds)
    {
        var summary = new ClassificationRollSummaryDto();

        if (activeTds == null || activeTds.Count == 0)
        {
            summary.TaxableTotals = new ClassificationRollRowDto { PropertyClassification = "Total Taxable Properties" };
            summary.ExemptTotals = new ClassificationRollRowDto { PropertyClassification = "Total Exempted Properties" };
            return summary;
        }

        static decimal GetEffectiveMarketValue(TaxDeclaration t) =>
            t.AdjustedMarketValue > 0 ? t.AdjustedMarketValue : t.MarketValue;

        static decimal GetEffectiveArea(TaxDeclaration t) =>
            t.Kind == PropertyKind.Building && t.TotalBuildingAreaSqm > 0
                ? t.TotalBuildingAreaSqm
                : t.AreaSqMeters;

        static bool IsTaxableTd(TaxDeclaration t) =>
            !string.Equals(t.Taxability, "Exempt", StringComparison.OrdinalIgnoreCase);

        var taxableStandardNames = new[]
        {
            "RESIDENTIAL",
            "AGRICULTURAL",
            "COMMERCIAL",
            "INDUSTRIAL",
            "TIMBERLAND",
            "CULTURAL",
            "HOSPITAL",
            "SCIENTIFIC",
            "RECREATIONAL",
            "OTHERS",
            "Local Water District"
        };

        var exemptStandardNames = new[]
        {
            "GOVERNMENT",
            "RELIGIOUS",
            "CHARITABLE"
        };

        var taxableTds = activeTds.Where(IsTaxableTd).ToList();
        var exemptTds = activeTds.Where(t => !IsTaxableTd(t)).ToList();

        // 1. Build Taxable Rows
        foreach (var name in taxableStandardNames)
        {
            var matching = taxableTds.Where(t =>
                string.Equals(t.Classification.ToString(), name, StringComparison.OrdinalIgnoreCase) ||
                (t.Classification == PropertyClassification.Special && name.Equals("Local Water District", StringComparison.OrdinalIgnoreCase)) ||
                (t.Classification == PropertyClassification.Special && name.Equals("OTHERS", StringComparison.OrdinalIgnoreCase) && !taxableTds.Any(x => x.Classification.ToString().Equals("OTHERS", StringComparison.OrdinalIgnoreCase)))
            ).ToList();

            int rpuLand = matching.Count(t => t.Kind == PropertyKind.Land);
            int rpuBldg = matching.Count(t => t.Kind == PropertyKind.Building);
            int rpuMach = matching.Count(t => t.Kind == PropertyKind.Machinery);

            summary.TaxableRows.Add(new ClassificationRollRowDto
            {
                PropertyClassification = name,
                LandAreaSqm = matching.Sum(GetEffectiveArea),
                RpuLand = rpuLand,
                RpuBldg = rpuBldg,
                RpuMach = rpuMach,
                TotalRpu = rpuLand + rpuBldg + rpuMach,
                MarketValue = matching.Sum(GetEffectiveMarketValue),
                AssessedValue = matching.Sum(t => t.AssessedValue)
            });
        }

        // Add any non-standard taxable classifications
        var otherTaxable = taxableTds
            .Where(t => !taxableStandardNames.Any(s => s.Equals(t.Classification.ToString(), StringComparison.OrdinalIgnoreCase)))
            .GroupBy(t => t.Classification.ToString())
            .ToList();

        foreach (var grp in otherTaxable)
        {
            int rpuLand = grp.Count(t => t.Kind == PropertyKind.Land);
            int rpuBldg = grp.Count(t => t.Kind == PropertyKind.Building);
            int rpuMach = grp.Count(t => t.Kind == PropertyKind.Machinery);

            summary.TaxableRows.Add(new ClassificationRollRowDto
            {
                PropertyClassification = grp.Key,
                LandAreaSqm = grp.Sum(GetEffectiveArea),
                RpuLand = rpuLand,
                RpuBldg = rpuBldg,
                RpuMach = rpuMach,
                TotalRpu = rpuLand + rpuBldg + rpuMach,
                MarketValue = grp.Sum(GetEffectiveMarketValue),
                AssessedValue = grp.Sum(t => t.AssessedValue)
            });
        }

        // 2. Build Exempt Rows
        foreach (var name in exemptStandardNames)
        {
            var matching = exemptTds.Where(t =>
                string.Equals(t.Classification.ToString(), name, StringComparison.OrdinalIgnoreCase) ||
                (t.Classification == PropertyClassification.Special && name.Equals("GOVERNMENT", StringComparison.OrdinalIgnoreCase))
            ).ToList();

            int rpuLand = matching.Count(t => t.Kind == PropertyKind.Land);
            int rpuBldg = matching.Count(t => t.Kind == PropertyKind.Building);
            int rpuMach = matching.Count(t => t.Kind == PropertyKind.Machinery);

            summary.ExemptRows.Add(new ClassificationRollRowDto
            {
                PropertyClassification = name,
                LandAreaSqm = matching.Sum(GetEffectiveArea),
                RpuLand = rpuLand,
                RpuBldg = rpuBldg,
                RpuMach = rpuMach,
                TotalRpu = rpuLand + rpuBldg + rpuMach,
                MarketValue = matching.Sum(GetEffectiveMarketValue),
                AssessedValue = matching.Sum(t => t.AssessedValue)
            });
        }

        var otherExempt = exemptTds
            .Where(t => !exemptStandardNames.Any(s => s.Equals(t.Classification.ToString(), StringComparison.OrdinalIgnoreCase)))
            .GroupBy(t => t.Classification.ToString())
            .ToList();

        foreach (var grp in otherExempt)
        {
            int rpuLand = grp.Count(t => t.Kind == PropertyKind.Land);
            int rpuBldg = grp.Count(t => t.Kind == PropertyKind.Building);
            int rpuMach = grp.Count(t => t.Kind == PropertyKind.Machinery);

            summary.ExemptRows.Add(new ClassificationRollRowDto
            {
                PropertyClassification = grp.Key,
                LandAreaSqm = grp.Sum(GetEffectiveArea),
                RpuLand = rpuLand,
                RpuBldg = rpuBldg,
                RpuMach = rpuMach,
                TotalRpu = rpuLand + rpuBldg + rpuMach,
                MarketValue = grp.Sum(GetEffectiveMarketValue),
                AssessedValue = grp.Sum(t => t.AssessedValue)
            });
        }

        // Totals
        summary.TaxableTotals = new ClassificationRollRowDto
        {
            PropertyClassification = "Total Taxable Properties",
            LandAreaSqm = summary.TaxableRows.Sum(r => r.LandAreaSqm),
            RpuLand = summary.TaxableRows.Sum(r => r.RpuLand),
            RpuBldg = summary.TaxableRows.Sum(r => r.RpuBldg),
            RpuMach = summary.TaxableRows.Sum(r => r.RpuMach),
            TotalRpu = summary.TaxableRows.Sum(r => r.TotalRpu),
            MarketValue = summary.TaxableRows.Sum(r => r.MarketValue),
            AssessedValue = summary.TaxableRows.Sum(r => r.AssessedValue)
        };

        summary.ExemptTotals = new ClassificationRollRowDto
        {
            PropertyClassification = "Total Exempted Properties",
            LandAreaSqm = summary.ExemptRows.Sum(r => r.LandAreaSqm),
            RpuLand = summary.ExemptRows.Sum(r => r.RpuLand),
            RpuBldg = summary.ExemptRows.Sum(r => r.RpuBldg),
            RpuMach = summary.ExemptRows.Sum(r => r.RpuMach),
            TotalRpu = summary.ExemptRows.Sum(r => r.TotalRpu),
            MarketValue = summary.ExemptRows.Sum(r => r.MarketValue),
            AssessedValue = summary.ExemptRows.Sum(r => r.AssessedValue)
        };

        summary.TotalLandAreaSqm = activeTds.Sum(GetEffectiveArea);
        summary.TotalProperties = activeTds.Count;
        summary.TaxablePropertiesCount = taxableTds.Count;
        summary.ExemptPropertiesCount = exemptTds.Count;

        if (activeTds.Count > 0)
        {
            summary.TaxablePropertiesPct = Math.Round(((decimal)taxableTds.Count / activeTds.Count) * 100m, 1);
            summary.ExemptPropertiesPct = Math.Round(100m - summary.TaxablePropertiesPct, 1);
        }

        return summary;
    }

    private AssessmentRollPagedResponseDto GetFilteredAssessmentRollPage(DashboardAnalyticsFilterDto filter, List<TaxDeclaration>? preloaded = null)
    {
        var query = preloaded ?? _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking()
            .Where(t => t.Status == AssessmentStatus.Active && !t.IsCancelledAndSuperseded)
            .ToList()
            .Where(t => !CastTdGenerator.IsUnapprovedCastTd(t.TaxDeclarationNumber))
            .ToList();

        // 1. Assessment Year
        if (filter.AssessmentYear.HasValue && filter.AssessmentYear.Value > 0)
        {
            query = query.Where(t =>
            {
                var dt = t.DateApproved ?? t.DateEncoded ?? t.DateAssessed;
                return dt.Year == filter.AssessmentYear.Value;
            }).ToList();
        }

        // 2. Property Kind
        if (filter.PropertyKind == 1) query = query.Where(t => t.Kind == PropertyKind.Land).ToList();
        else if (filter.PropertyKind == 2) query = query.Where(t => t.Kind == PropertyKind.Building).ToList();
        else if (filter.PropertyKind == 3) query = query.Where(t => t.Kind == PropertyKind.Machinery).ToList();

        // 3. Barangay Location Filter
        string brgy = (filter.BarangayFilter ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(brgy) && brgy != "0" && !brgy.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t =>
            {
                var (code, name) = ResolveAnalyticsBarangay(t);
                return string.Equals(code, brgy, StringComparison.OrdinalIgnoreCase) ||
                       name.Contains(brgy, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        // 4. Section Number Filter
        string sec = (filter.SectionFilter ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(sec) && sec != "0" && !sec.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => string.Equals(ResolveSection(t), sec, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // 5. Search Text Filter (TD No, Owner Name, PIN, Previous TD, Survey No, Title No)
        string search = (filter.Search ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                (t.TaxDeclarationNumber != null && t.TaxDeclarationNumber.ToLower().Contains(search)) ||
                (t.OwnerName != null && t.OwnerName.ToLower().Contains(search)) ||
                (t.PropertyIndexNumber != null && t.PropertyIndexNumber.ToLower().Contains(search)) ||
                (t.ARPNo != null && t.ARPNo.ToLower().Contains(search)) ||
                (t.PreviousTdNo != null && t.PreviousTdNo.ToLower().Contains(search)) ||
                (t.SurveyNo != null && t.SurveyNo.ToLower().Contains(search)) ||
                (t.TitleNo != null && t.TitleNo.ToLower().Contains(search)) ||
                (t.PreviousOwner != null && t.PreviousOwner.ToLower().Contains(search))
            ).ToList();
        }

        int totalCount = query.Count;
        int page = Math.Max(1, filter.Page);
        int pageSize = filter.PageSize > 0 ? filter.PageSize : 5;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

        if (totalCount == 0)
        {
            return new AssessmentRollPagedResponseDto
            {
                Records = new List<AssessmentRollDetailedRecordDto>(),
                TotalRecords = 0,
                Page = 1,
                PageSize = pageSize,
                TotalPages = 1
            };
        }

        var pagedItems = query
            .OrderByDescending(t => t.DateApproved ?? t.DateEncoded ?? t.DateAssessed)
            .ThenBy(t => t.TaxDeclarationNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t =>
            {
                var (bCode, bName) = ResolveAnalyticsBarangay(t);
                string ownerAddr = !string.IsNullOrWhiteSpace(t.OwnerAddress)
                    ? t.OwnerAddress
                    : $"Brgy. {bName.Replace("BRGY.", "").Trim()}, City of Koronadal";

                return new AssessmentRollDetailedRecordDto
                {
                    TdNo = t.TaxDeclarationNumber ?? "-",
                    PreviousTdNo = string.IsNullOrWhiteSpace(t.PreviousTdNo) ? "-" : t.PreviousTdNo,
                    Section = ResolveSection(t),
                    ParcelNo = ResolveParcel(t),
                    OwnerName = t.OwnerName ?? "-",
                    OwnerAddress = ownerAddr,
                    PrevOwner = string.IsNullOrWhiteSpace(t.PreviousOwner) ? "-" : t.PreviousOwner,
                    CadNo = ResolveCadNo(t),
                    SurveyNo = string.IsNullOrWhiteSpace(t.SurveyNo) ? "-" : t.SurveyNo,
                    TitleNo = string.IsNullOrWhiteSpace(t.TitleNo) ? "-" : t.TitleNo,
                    PreviousPin = ResolvePrevPin(t),
                    Classification = t.Classification.ToString(),
                    Kind = t.Kind.ToString(),
                    ActualUse = ResolveAnalyticsActualUse(t),
                    AreaSqm = t.Kind == PropertyKind.Building && t.TotalBuildingAreaSqm > 0 ? t.TotalBuildingAreaSqm : t.AreaSqMeters,
                    MarketValue = t.AdjustedMarketValue > 0 ? t.AdjustedMarketValue : t.MarketValue,
                    AssessedValue = t.AssessedValue,
                    BldgCount = t.Kind == PropertyKind.Building ? 1 : 0,
                    MachCount = t.Kind == PropertyKind.Machinery ? 1 : 0,
                    Memoranda = string.IsNullOrWhiteSpace(t.Memoranda) ? "-" : t.Memoranda,
                    EffectivityDate = (t.DateApproved ?? t.DateEncoded ?? t.DateAssessed).ToString("MMM d, yyyy")
                };
            })
            .ToList();

        return new AssessmentRollPagedResponseDto
        {
            Records = pagedItems,
            TotalRecords = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    // =========================================================================
    // USER PERFORMANCE & DIVISION COMPLETION STATISTICS DASHBOARD
    // =========================================================================

    [HttpGet]
    public IActionResult UserPerformance(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "UserPerformance"))
        {
            TempData["ErrorMessage"] = "Access Denied: Your assigned user role does not have permission to access User Performance & Division Statistics.";
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewBag.InitialStartDate = startDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.InitialEndDate = endDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        return View();
    }

    [HttpPost]
    public IActionResult GetUserPerformanceData([FromBody] UserPerformanceFilterDto? filter)
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "UserPerformance"))
        {
            return Json(new { success = false, message = "Access Denied: Your assigned user role does not have permission to access User Performance & Division Statistics." });
        }

        try
        {
            var data = BuildUserPerformanceData(filter ?? new UserPerformanceFilterDto());
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Failed to load user performance data: {ex.Message}" });
        }
    }

    [HttpGet]
    public IActionResult ExportUserPerformanceCsv(DateTime? startDate = null, DateTime? endDate = null, int? divisionId = null, string? requestType = null, string? search = null)
    {
        if (!_permissions.HasModuleAccess(User, "Dashboard", "UserPerformance"))
        {
            return Forbid();
        }

        var data = BuildUserPerformanceData(new UserPerformanceFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
            DivisionId = divisionId,
            RequestType = requestType,
            Search = search
        });

        static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
                return $"\"{val.Replace("\"", "\"\"")}\"";
            return val;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Rank,Full Name,Username,Role,Division,Transactions Processed,Forwarded,Overdue,Last Active");
        foreach (var row in data.UserLeaderboard)
        {
            sb.AppendLine(string.Join(",",
                row.Rank,
                EscapeCsv(row.FullName),
                EscapeCsv(row.Username),
                EscapeCsv(row.RoleDisplay),
                EscapeCsv(row.DivisionName),
                row.ActiveClaimedCount,
                row.ForwardedCount,
                row.OverdueCount,
                EscapeCsv(row.LastActiveText)));
        }

        sb.AppendLine();
        sb.AppendLine("Division Comparison");
        sb.AppendLine("Division,Total Transactions,Finished,Ongoing,Pending,Deferred,Completion Rate (%),Avg Turnaround (Days),Share of Finished (%)");
        foreach (var d in data.DivisionStats)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(d.DivisionName),
                d.TotalTransactions,
                d.FinishedCount,
                d.OngoingCount,
                d.PendingCount,
                d.DeferredCount,
                d.CompletionRate.ToString("0.0", CultureInfo.InvariantCulture),
                d.AvgTurnaroundDays.ToString("0.0", CultureInfo.InvariantCulture),
                d.ShareOfFinishedPercent.ToString("0.0", CultureInfo.InvariantCulture)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"User_Performance_Division_Stats_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }

    private UserPerformanceDashboardDataDto BuildUserPerformanceData(UserPerformanceFilterDto filter)
    {
        DateTime? start = filter.StartDate?.Date;
        DateTime? end = filter.EndDate?.Date;
        if (start.HasValue && end.HasValue && start > end)
        {
            (start, end) = (end, start);
        }

        string periodLabel = (start.HasValue || end.HasValue)
            ? $"{(start.HasValue ? start.Value.ToString("MMM dd, yyyy") : "Start")} – {(end.HasValue ? end.Value.ToString("MMM dd, yyyy") : "Today")}"
            : "All Time";

        // 1. Unified workflow items for division completion comparison & active claimed counts
        var allQueueItems = GetAllItems();

        if (!string.IsNullOrWhiteSpace(filter.RequestType))
        {
            allQueueItems = allQueueItems
                .Where(i => string.Equals(i.RequestType, filter.RequestType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var dateFilteredQueue = allQueueItems;
        if (start.HasValue)
            dateFilteredQueue = dateFilteredQueue.Where(i => i.DateSubmitted.Date >= start.Value).ToList();
        if (end.HasValue)
            dateFilteredQueue = dateFilteredQueue.Where(i => i.DateSubmitted.Date <= end.Value).ToList();

        static bool IsItemFinished(WorkflowQueueItemDto i) =>
            string.Equals(i.StatusText, "Released", StringComparison.OrdinalIgnoreCase) ||
            i.CurrentStage == WorkflowStage.Completed;

        var divisions = new[]
        {
            (Id: 1, ShortName: "Division 1", Name: "Division 1: Tax Assessment & Land Holding"),
            (Id: 2, ShortName: "Division 2", Name: "Division 2: GIS & Tax Mapping"),
            (Id: 3, ShortName: "Division 3", Name: "Division 3: Records & Certification")
        };

        int totalFinishedAllDivs = dateFilteredQueue.Count(IsItemFinished);

        var divisionStats = new List<DivisionComparisonDto>();
        foreach (var div in divisions)
        {
            var divItems = dateFilteredQueue.Where(i => (int)i.Division == div.Id).ToList();
            int finished = divItems.Count(IsItemFinished);
            int deferred = divItems.Count(i => !IsItemFinished(i) && (i.IsDeferred || i.CurrentStage == WorkflowStage.Returned));
            int ongoing = divItems.Count(i => !IsItemFinished(i) && !i.IsDeferred && i.CurrentStage != WorkflowStage.Returned && i.IsClaimed);
            int pending = divItems.Count(i => !IsItemFinished(i) && !i.IsDeferred && i.CurrentStage != WorkflowStage.Returned && !i.IsClaimed);

            var turnaroundSamples = divItems
                .Where(i => IsItemFinished(i) && i.LastForwardedDate.HasValue && i.LastForwardedDate.Value >= i.DateSubmitted)
                .Select(i => (decimal)(i.LastForwardedDate!.Value - i.DateSubmitted).TotalDays)
                .ToList();

            decimal avgTurnaround = turnaroundSamples.Any() ? Math.Round(turnaroundSamples.Average(), 1) : 0m;
            decimal completionRate = divItems.Count > 0 ? Math.Round((decimal)finished / divItems.Count * 100m, 1) : 0m;
            decimal share = totalFinishedAllDivs > 0 ? Math.Round((decimal)finished / totalFinishedAllDivs * 100m, 1) : 0m;

            var finishedWithSla = divItems.Where(i => IsItemFinished(i) && i.SlaStatus != null).ToList();
            int onTime = finishedWithSla.Count(i => i.SlaStatus!.SlaState is "OnTrack" or "DueSoon");
            int overdue = finishedWithSla.Count(i => i.SlaStatus!.SlaState == "Overdue");
            int withTarget = onTime + overdue;
            decimal slaCompliance = withTarget > 0 ? Math.Round((decimal)onTime / withTarget * 100m, 1) : 0m;
            double avgWorkingMins = finishedWithSla.Any() ? Math.Round(finishedWithSla.Average(i => i.SlaStatus!.ElapsedWorkingMinutes), 1) : 0;

            divisionStats.Add(new DivisionComparisonDto
            {
                DivisionId = div.Id,
                ShortName = div.ShortName,
                DivisionName = div.Name,
                TotalTransactions = divItems.Count,
                FinishedCount = finished,
                OngoingCount = ongoing,
                PendingCount = pending,
                DeferredCount = deferred,
                CompletionRate = completionRate,
                AvgTurnaroundDays = avgTurnaround,
                ShareOfFinishedPercent = share,
                OnTimeFinishedCount = onTime,
                OverdueFinishedCount = overdue,
                SlaComplianceRate = slaCompliance,
                AvgWorkingMinutes = avgWorkingMins
            });
        }

        // Request type breakdown (filtered by Division if selected)
        var scopedQueueForReqTypes = (filter.DivisionId.HasValue && filter.DivisionId.Value > 0)
            ? dateFilteredQueue.Where(i => (int)i.Division == filter.DivisionId.Value).ToList()
            : dateFilteredQueue;

        static string FormatRequestTypeName(string rt) => rt switch
        {
            "PropertyAssessmentRequest" => "Property Assessment (RPA)",
            "RequestForInspection" => "Request for Inspection (RFI)",
            "LandHoldingRequest" => "Land Holding",
            "TaxDeclarationRequest" => "Tax Declaration / Certification",
            "TaxMappingRequest" => "GIS Tax Mapping",
            _ => rt
        };

        var requestTypeStats = scopedQueueForReqTypes
            .GroupBy(i => i.RequestType)
            .Select(g =>
            {
                int total = g.Count();
                int fin = g.Count(IsItemFinished);
                return new RequestTypeDivisionStatDto
                {
                    RequestType = g.Key,
                    DisplayName = FormatRequestTypeName(g.Key),
                    TotalCount = total,
                    FinishedCount = fin,
                    CompletionRate = total > 0 ? Math.Round((decimal)fin / total * 100m, 1) : 0m
                };
            })
            .OrderByDescending(x => x.TotalCount)
            .ToList();

        // Finished trend over time by division
        var finishedItemsAll = allQueueItems.Where(IsItemFinished).ToList();
        var trendEnd = end ?? DateTime.Now.Date;
        var trendStart = start ?? trendEnd.AddDays(-13);
        int spanDays = (int)(trendEnd - trendStart).TotalDays + 1;

        var trendLabels = new List<string>();
        var div1Trend = new List<int>();
        var div2Trend = new List<int>();
        var div3Trend = new List<int>();

        if (spanDays <= 45)
        {
            for (var day = trendStart; day <= trendEnd; day = day.AddDays(1))
            {
                trendLabels.Add(day.ToString("MMM dd"));
                div1Trend.Add(finishedItemsAll.Count(i => (int)i.Division == 1 && (i.LastForwardedDate ?? i.DateSubmitted).Date == day));
                div2Trend.Add(finishedItemsAll.Count(i => (int)i.Division == 2 && (i.LastForwardedDate ?? i.DateSubmitted).Date == day));
                div3Trend.Add(finishedItemsAll.Count(i => (int)i.Division == 3 && (i.LastForwardedDate ?? i.DateSubmitted).Date == day));
            }
        }
        else
        {
            var mStart = new DateTime(trendStart.Year, trendStart.Month, 1);
            for (var m = mStart; m <= trendEnd; m = m.AddMonths(1))
            {
                var nextM = m.AddMonths(1);
                trendLabels.Add(m.ToString("MMM yyyy"));
                div1Trend.Add(finishedItemsAll.Count(i => (int)i.Division == 1 && (i.LastForwardedDate ?? i.DateSubmitted) >= m && (i.LastForwardedDate ?? i.DateSubmitted) < nextM));
                div2Trend.Add(finishedItemsAll.Count(i => (int)i.Division == 2 && (i.LastForwardedDate ?? i.DateSubmitted) >= m && (i.LastForwardedDate ?? i.DateSubmitted) < nextM));
                div3Trend.Add(finishedItemsAll.Count(i => (int)i.Division == 3 && (i.LastForwardedDate ?? i.DateSubmitted) >= m && (i.LastForwardedDate ?? i.DateSubmitted) < nextM));
            }
        }

        // 2. DocumentForwardingLogs for user performance
        var logsQuery = _context.DocumentForwardingLogs.AsNoTracking().AsQueryable();
        if (start.HasValue)
            logsQuery = logsQuery.Where(l => l.Timestamp >= start.Value);
        if (end.HasValue)
            logsQuery = logsQuery.Where(l => l.Timestamp < end.Value.AddDays(1));
        if (filter.DivisionId.HasValue && filter.DivisionId.Value > 0)
            logsQuery = logsQuery.Where(l => (int)l.Division == filter.DivisionId.Value);
        if (!string.IsNullOrWhiteSpace(filter.RequestType))
            logsQuery = logsQuery.Where(l => l.RequestType == filter.RequestType);

        var logs = logsQuery.ToList();

        // Filter out system-generated placeholder entries if any
        var userLogs = logs
            .Where(l => !string.IsNullOrWhiteSpace(l.ForwardedByName) &&
                        !string.Equals(l.ForwardedByName.Trim(), "System User", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(l.ForwardedByName.Trim(), "System Intake", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var users = _context.UserAccounts.AsNoTracking().ToList();
        var activeClaimedItems = allQueueItems
            .Where(i => i.IsClaimed && !IsItemFinished(i))
            .ToList();

        var slaSetting = _charterSlaService.GetScheduleSetting();
        var slaHolidays = _charterSlaService.GetHolidays(activeOnly: true);
        var sequences = _context.WorkflowSequences.AsNoTracking().ToList();
        var queueByReq = allQueueItems
            .GroupBy(i => i.RequestId)
            .ToDictionary(g => g.Key, g => g.First());

        var involvedReqIds = userLogs.Select(l => l.RequestId).Distinct().ToList();
        var allReqLogsById = involvedReqIds.Any()
            ? _context.DocumentForwardingLogs.AsNoTracking()
                .Where(l => involvedReqIds.Contains(l.RequestId))
                .OrderBy(l => l.Timestamp)
                .ToList()
                .GroupBy(l => l.RequestId)
                .ToDictionary(g => g.Key, g => g.ToList())
            : new Dictionary<Guid, List<DocumentForwardingLog>>();

        static int GetTxTypeId(string requestType) => requestType switch
        {
            "RequestForInspection" => 1,
            "PropertyAssessmentRequest" => 2,
            "TaxMappingRequest" => 3,
            "TaxDeclarationRequest" => 4,
            "LandHoldingRequest" => 5,
            _ => 0
        };

        int GetStageMaxMinutes(int txTypeId, int stageId) =>
            sequences
                .Where(s => s.TransactionTypeId == txTypeId && s.CurrentStatusId == stageId && s.EstimatedMinutes > 0)
                .Select(s => s.EstimatedMinutes)
                .FirstOrDefault();

        static string GetDivisionShortName(int? divId) => divId switch
        {
            1 => "Division 1: Assessment & Land Holding",
            2 => "Division 2: GIS & Tax Mapping",
            3 => "Division 3: Records & Certification",
            _ => "Unassigned"
        };

        static bool IsApprovalOrReleaseLog(DocumentForwardingLog l) =>
            l.IsForApproval ||
            l.ActionTaken.Contains("Approve", StringComparison.OrdinalIgnoreCase) ||
            l.ActionTaken.Contains("Release", StringComparison.OrdinalIgnoreCase) ||
            l.ToStage is WorkflowStage.AssessorApproval or WorkflowStage.ApprovalGIS or WorkflowStage.ApprovalRecords
                or WorkflowStage.Releasing or WorkflowStage.ReleasingGIS or WorkflowStage.ReleasingRecords or WorkflowStage.Completed;

        static bool IsDeferredOrReturnedLog(DocumentForwardingLog l) =>
            l.IsDeferred ||
            l.ActionTaken.Contains("Defer", StringComparison.OrdinalIgnoreCase) ||
            l.ActionTaken.Contains("Return", StringComparison.OrdinalIgnoreCase) ||
            l.ToStage is WorkflowStage.Deferred or WorkflowStage.Returned;

        // Match logs to UserAccount records; also include any historical ForwardedByName not present in UserAccounts
        var matchedLogIds = new HashSet<Guid>();
        var leaderboard = new List<UserPerformanceRowDto>();

        foreach (var u in users)
        {
            if (filter.DivisionId.HasValue && filter.DivisionId.Value > 0 && u.DivisionId != filter.DivisionId.Value)
                continue;

            var uLogs = userLogs.Where(l =>
                string.Equals(l.ForwardedByName.Trim(), u.FullName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(l.ForwardedByName.Trim(), u.Username.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var l in uLogs) matchedLogIds.Add(l.Id);

            var claimedItems = activeClaimedItems.Where(i =>
                i.AssignedUserId == u.Id ||
                (!string.IsNullOrWhiteSpace(i.AssignedUserName) &&
                 (string.Equals(i.AssignedUserName.Trim(), u.FullName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(i.AssignedUserName.Trim(), u.Username.Trim(), StringComparison.OrdinalIgnoreCase))))
                .ToList();
            int claimed = claimedItems.Count;

            var overdueReqIds = new HashSet<Guid>();
            bool hasRoleMaxTime = u.WorkflowRoleId.HasValue && u.WorkflowRoleId.Value > 0 &&
                sequences.Any(s => s.CurrentStatusId == u.WorkflowRoleId.Value && s.EstimatedMinutes > 0);

            foreach (var ci in claimedItems)
            {
                int txTypeId = GetTxTypeId(ci.RequestType);
                int maxMins = GetStageMaxMinutes(txTypeId, (int)ci.CurrentStage);
                if (maxMins > 0 && ci.SlaStatus != null && ci.SlaStatus.StageElapsedWorkingMinutes > maxMins)
                {
                    overdueReqIds.Add(ci.RequestId);
                }
            }

            var forwardedLogs = uLogs.Where(l => !IsDeferredOrReturnedLog(l)).ToList();
            foreach (var fl in forwardedLogs)
            {
                int txTypeId = GetTxTypeId(fl.RequestType);
                int maxMins = GetStageMaxMinutes(txTypeId, (int)fl.FromStage);
                if (maxMins <= 0) continue;

                DateTime stageStart = queueByReq.TryGetValue(fl.RequestId, out var qItem) ? qItem.DateSubmitted : fl.Timestamp;
                if (allReqLogsById.TryGetValue(fl.RequestId, out var reqLogs))
                {
                    var arrival = reqLogs.LastOrDefault(rl => rl.Timestamp < fl.Timestamp && rl.ToStage == fl.FromStage && !rl.IsDeferred);
                    if (arrival != null) stageStart = arrival.Timestamp;
                }

                double elapsedMins = _charterSlaService.CalculateWorkingMinutes(stageStart, fl.Timestamp, slaSetting, slaHolidays);
                if (elapsedMins > maxMins)
                {
                    overdueReqIds.Add(fl.RequestId);
                }
            }

            string roleLabel = !string.IsNullOrWhiteSpace(u.WorkflowRoleName)
                ? u.WorkflowRoleName
                : u.Role.ToString();

            leaderboard.Add(new UserPerformanceRowDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                RoleDisplay = roleLabel,
                DivisionId = u.DivisionId,
                DivisionName = GetDivisionShortName(u.DivisionId),
                UniqueTransactionsProcessed = uLogs.Select(l => l.RequestId).Distinct().Count(),
                TotalActions = uLogs.Count,
                ForwardedCount = forwardedLogs.Select(l => l.RequestId).Distinct().Count(),
                ApprovedOrReleasedCount = uLogs.Count(IsApprovalOrReleaseLog),
                DeferredOrReturnedCount = uLogs.Count(IsDeferredOrReturnedLog),
                ActiveClaimedCount = claimed,
                OverdueCount = overdueReqIds.Count,
                HasRoleMaxTime = hasRoleMaxTime,
                LastActiveDate = uLogs.Any() ? uLogs.Max(l => l.Timestamp) : null,
                IsActive = u.IsActive
            });
        }

        // Include any ForwardedByName not matched to a UserAccount (e.g., legacy/renamed users)
        var unmatchedGroups = userLogs
            .Where(l => !matchedLogIds.Contains(l.Id))
            .GroupBy(l => l.ForwardedByName.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var grp in unmatchedGroups)
        {
            var gLogs = grp.ToList();
            var latest = gLogs.OrderByDescending(l => l.Timestamp).First();
            int divId = (int)latest.Division;

            leaderboard.Add(new UserPerformanceRowDto
            {
                UserId = null,
                FullName = grp.Key,
                Username = "-",
                RoleDisplay = latest.ForwardedByRole,
                DivisionId = divId,
                DivisionName = GetDivisionShortName(divId),
                UniqueTransactionsProcessed = gLogs.Select(l => l.RequestId).Distinct().Count(),
                TotalActions = gLogs.Count,
                ForwardedCount = gLogs.Count(l => !IsDeferredOrReturnedLog(l)),
                ApprovedOrReleasedCount = gLogs.Count(IsApprovalOrReleaseLog),
                DeferredOrReturnedCount = gLogs.Count(IsDeferredOrReturnedLog),
                ActiveClaimedCount = 0,
                LastActiveDate = gLogs.Max(l => l.Timestamp),
                IsActive = true
            });
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var q = filter.Search.Trim();
            leaderboard = leaderboard.Where(r =>
                r.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.RoleDisplay.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.DivisionName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        leaderboard = leaderboard
            .OrderByDescending(r => r.ForwardedCount)
            .ThenByDescending(r => r.ActiveClaimedCount)
            .ThenBy(r => r.FullName)
            .ToList();

        for (int i = 0; i < leaderboard.Count; i++)
        {
            leaderboard[i].Rank = i + 1;
        }

        var topDiv = divisionStats.OrderByDescending(d => d.FinishedCount).ThenByDescending(d => d.CompletionRate).FirstOrDefault();
        var topUser = leaderboard.FirstOrDefault(u => u.UniqueTransactionsProcessed > 0);
        int totalQueueInPeriod = dateFilteredQueue.Count;
        decimal overallRate = totalQueueInPeriod > 0 ? Math.Round((decimal)totalFinishedAllDivs / totalQueueInPeriod * 100m, 1) : 0m;

        return new UserPerformanceDashboardDataDto
        {
            PeriodLabel = periodLabel,
            TotalActionsProcessed = userLogs.Count,
            UniqueTransactionsProcessed = userLogs.Select(l => l.RequestId).Distinct().Count(),
            ActiveContributors = leaderboard.Count(u => u.UniqueTransactionsProcessed > 0),
            TotalActiveUsers = users.Count(u => u.IsActive),
            TotalTransactionsInPeriod = totalQueueInPeriod,
            TotalFinishedInPeriod = totalFinishedAllDivs,
            OverallCompletionRate = overallRate,
            TopDivisionName = topDiv != null && topDiv.FinishedCount > 0 ? topDiv.ShortName : "—",
            TopDivisionFinished = topDiv?.FinishedCount ?? 0,
            TopDivisionRate = topDiv?.CompletionRate ?? 0m,
            TopUserName = topUser?.FullName ?? "—",
            TopUserRole = topUser?.RoleDisplay ?? string.Empty,
            TopUserTransactions = topUser?.UniqueTransactionsProcessed ?? 0,
            DivisionStats = divisionStats,
            TrendLabels = trendLabels,
            Div1FinishedTrend = div1Trend,
            Div2FinishedTrend = div2Trend,
            Div3FinishedTrend = div3Trend,
            RequestTypeStats = requestTypeStats,
            UserLeaderboard = leaderboard
        };
    }
}
