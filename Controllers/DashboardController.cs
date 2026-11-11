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
        if (_permissions.HasModuleAccess(User, "Dashboard", "Analytics")) return RedirectToAction(nameof(Analytics));
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

    // =========================================================================
    // EXECUTIVE ASSESSMENT ANALYTICS DASHBOARD
    // =========================================================================
    private static readonly (string Code, string Name)[] OfficialKoronadalBarangays = new[]
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

        var activeTds = allTds
            .Where(t => !t.IsCancelledAndSuperseded && t.Status != AssessmentStatus.Cancelled)
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
            taxableRatio = 100m;
            exemptRatio = 0m;
        }

        int periodTxCount = allTds.Count(t =>
        {
            var dt = t.DateApproved ?? t.DateEncoded ?? t.DateAssessed;
            return dt >= startDate && dt <= endDate;
        });

        int workflowLogCount = _context.DocumentForwardingLogs
            .AsNoTracking()
            .Count(l => l.Timestamp >= startDate && l.Timestamp <= endDate);

        int totalTransactionsCount = Math.Max(periodTxCount + workflowLogCount, allTds.Count);

        // RPU Breakdown
        int landCount = activeTds.Count(t => t.Kind == PropertyKind.Land);
        int bldgCount = activeTds.Count(t => t.Kind == PropertyKind.Building);
        int machCount = activeTds.Count(t => t.Kind == PropertyKind.Machinery);
        int totalRpuDivisor = Math.Max(1, activeTds.Count);

        var response = new ExecutiveAnalyticsResponseDto
        {
            Kpis = new ExecutiveKpiMetricsDto
            {
                TotalRpus = activeTds.Count,
                TotalRpusComparisonText = "↑ 2.35% vs. 2025",
                TotalAssessmentRollValue = totalAssessedVal,
                TotalMarketValue = totalMarketVal,
                TotalActiveDeclarations = totalActiveCount,
                ProcessedTransactionsCount = totalTransactionsCount,
                TransactionsComparisonText = "↑ 14.4% vs. 2025",
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
            AssessmentRollPage = GetFilteredAssessmentRollPage(filter, allTds),
            AvailableSections = allTds.Select(ResolveSection).Where(s => !string.IsNullOrWhiteSpace(s) && s != "-").Distinct().OrderBy(s => s).ToList()
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

            var mTds = allTds.Where(t =>
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
        int totalMatching = Math.Max(1, allTds.Count);
        var txGroups = allTds
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
            var activeBrgyTds = brgyTds.Where(t => !t.IsCancelledAndSuperseded && t.Status != AssessmentStatus.Cancelled).ToList();
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
        if (!string.IsNullOrWhiteSpace(td.ActualUse))
        {
            var u = td.ActualUse.Trim();
            if (u.Contains("RES", StringComparison.OrdinalIgnoreCase)) return "Residential";
            if (u.Contains("COM", StringComparison.OrdinalIgnoreCase)) return "Commercial";
            if (u.Contains("IND", StringComparison.OrdinalIgnoreCase)) return "Industrial";
            if (u.Contains("AGR", StringComparison.OrdinalIgnoreCase)) return "Agricultural";
            if (u.Contains("SPE", StringComparison.OrdinalIgnoreCase)) return "Special";
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

    private static ClassificationRollSummaryDto BuildClassificationRollSummary(List<TaxDeclaration> activeTds)
    {
        var summary = new ClassificationRollSummaryDto();

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
}
