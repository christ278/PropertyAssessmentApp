using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

[Authorize]
public class AuditLogController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;

    public AuditLogController(ApplicationDbContext context, IPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    private bool CanViewAllLogs() =>
        _permissionService.CanAccess(User, "AuditLog", "Index")
        || User.IsInRole("SystemAdmin")
        || string.Equals(User.Identity?.Name, "admin", StringComparison.OrdinalIgnoreCase);

    private bool CanViewSelfLogs() =>
        _permissionService.CanAccess(User, "AuditLog", "Self");

    [HttpGet]
    public async Task<IActionResult> Self(string? entity = null, string? actionType = null, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        => await Index(entity, actionType, search, dateFrom, dateTo);

    [HttpGet]
    public async Task<IActionResult> Index(string? entity = null, string? actionType = null, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        bool viewAll = CanViewAllLogs();
        bool viewSelf = CanViewSelfLogs();

        if (!viewAll && !viewSelf)
        {
            TempData["ErrorMessage"] = "Access Denied: You do not have permission to view System Audit Logs.";
            return RedirectToAction("Index", "Home");
        }

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!viewAll)
        {
            var currentUsername = User.Identity?.Name ?? string.Empty;
            query = query.Where(a => a.ChangedBy == currentUsername);
        }

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.EntityName == entity.Trim());

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(a => a.Action == actionType.Trim());

        if (dateFrom.HasValue)
            query = query.Where(a => a.ChangedAt >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            query = query.Where(a => a.ChangedAt < dateTo.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.EntityName.Contains(term) ||
                a.RecordId.Contains(term) ||
                (a.ChangedBy != null && a.ChangedBy.Contains(term)) ||
                (a.ChangedByName != null && a.ChangedByName.Contains(term)) ||
                (a.ChangedColumnsJson != null && a.ChangedColumnsJson.Contains(term)) ||
                (a.NewValuesJson != null && a.NewValuesJson.Contains(term)) ||
                (a.OldValuesJson != null && a.OldValuesJson.Contains(term)));
        }

        var logs = await query
            .OrderByDescending(a => a.ChangedAt)
            .Take(500)
            .ToListAsync();

        var entityNames = await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        ViewBag.EntityNames = entityNames;
        ViewBag.IsSelfLogsOnly = !viewAll;
        ViewBag.SelectedEntity = entity ?? string.Empty;
        ViewBag.SelectedAction = actionType ?? string.Empty;
        ViewBag.Search = search ?? string.Empty;
        ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd") ?? string.Empty;

        return View(logs);
    }
}
