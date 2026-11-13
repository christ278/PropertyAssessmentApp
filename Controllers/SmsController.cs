using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

[Authorize]
public class SmsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPhilSmsService _philSmsService;
    private readonly IOwnerService _ownerService;
    private readonly IPermissionService _permissionService;

    public SmsController(
        ApplicationDbContext context,
        IPhilSmsService philSmsService,
        IOwnerService ownerService,
        IPermissionService permissionService)
    {
        _context = context;
        _philSmsService = philSmsService;
        _ownerService = ownerService;
        _permissionService = permissionService;
    }

    // GET: /Sms or /Sms/Index
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1,
        int pageSize = 15)
    {
        if (!_permissionService.CanAccess(User, "Sms", "Index"))
        {
            TempData["ErrorMessage"] = "Access Denied: Your user account does not have permission to view SMS Delivery Logs.";
            return RedirectToAction("Index", "Home");
        }

        var query = _context.SmsNotificationLogs
            .Include(l => l.Owner)
            .AsNoTracking()
            .AsQueryable();

        // 1. Overall & Filtered Stats
        ViewBag.TotalSent = await _context.SmsNotificationLogs.CountAsync();
        ViewBag.TotalSuccess = await _context.SmsNotificationLogs.CountAsync(l => l.Status == SmsDeliveryStatus.Success);
        ViewBag.TotalFailed = await _context.SmsNotificationLogs.CountAsync(l => l.Status == SmsDeliveryStatus.Failed);
        
        var today = DateTime.Today;
        ViewBag.SentToday = await _context.SmsNotificationLogs.CountAsync(l => l.SentAt >= today && l.SentAt < today.AddDays(1));

        // 2. Filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            var clean = search.Trim();
            query = query.Where(l =>
                l.RecipientNumber.Contains(clean) ||
                (l.RecipientName != null && l.RecipientName.Contains(clean)) ||
                l.Message.Contains(clean) ||
                (l.SentBy != null && l.SentBy.Contains(clean)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Status == SmsDeliveryStatus.Success);
            }
            else if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Status == SmsDeliveryStatus.Failed);
            }
        }

        if (fromDate.HasValue)
        {
            var start = fromDate.Value.Date;
            query = query.Where(l => l.SentAt >= start);
        }

        if (toDate.HasValue)
        {
            var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(l => l.SentAt <= end);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

        var items = await query
            .OrderByDescending(l => l.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentSearch = search;
        ViewBag.CurrentStatus = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalFilteredCount = totalCount;
        ViewBag.PageSize = pageSize;

        // Top 30 owners for Quick Compose selection
        ViewBag.QuickOwners = await _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive && !string.IsNullOrEmpty(o.ContactNo))
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.CorporateName)
            .Take(30)
            .ToListAsync();

        return View(items);
    }

    // POST: /Sms/Send
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid? ownerId, string recipient, string? recipientName, string message)
    {
        if (!_permissionService.CanAccess(User, "Sms", "Index"))
        {
            return Json(new { success = false, message = "Access Denied: You do not have permission to send SMS messages." });
        }

        Owner? owner = null;
        if (ownerId.HasValue && ownerId.Value != Guid.Empty)
        {
            owner = _ownerService.GetById(ownerId.Value);
        }

        var targetNumber = !string.IsNullOrWhiteSpace(recipient)
            ? recipient.Trim()
            : (owner?.ContactNo ?? string.Empty);

        var targetName = !string.IsNullOrWhiteSpace(recipientName)
            ? recipientName.Trim()
            : (owner?.DisplayName ?? string.Empty);

        var senderUser = User.Identity?.Name ?? "Assessor Staff";

        var result = await _philSmsService.SendSmsAsync(
            recipient: targetNumber,
            message: message,
            senderId: null,
            ownerId: owner?.Id,
            recipientName: targetName,
            sentBy: senderUser);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            normalizedRecipient = result.NormalizedRecipient,
            logId = result.LogId
        });
    }

    // POST: /Sms/Resend
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resend(Guid id)
    {
        if (!_permissionService.CanAccess(User, "Sms", "Index"))
        {
            return Json(new { success = false, message = "Access Denied: You do not have permission to resend SMS messages." });
        }

        var log = await _context.SmsNotificationLogs
            .Include(l => l.Owner)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log == null)
        {
            return Json(new { success = false, message = "SMS Log record not found." });
        }

        var senderUser = User.Identity?.Name ?? "Assessor Staff";

        var result = await _philSmsService.SendSmsAsync(
            recipient: log.RecipientNumber,
            message: log.Message,
            senderId: log.SenderId,
            ownerId: log.OwnerId,
            recipientName: log.RecipientName,
            sentBy: $"{senderUser} (Resend)");

        return Json(new
        {
            success = result.Success,
            message = result.Success
                ? $"SMS successfully resent to {result.NormalizedRecipient ?? log.RecipientNumber}!"
                : $"Resend failed: {result.Message}",
            logId = result.LogId
        });
    }

    // GET: /Sms/Details/{id}
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        if (!_permissionService.CanAccess(User, "Sms", "Index"))
        {
            return Forbid();
        }

        var log = await _context.SmsNotificationLogs
            .Include(l => l.Owner)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log == null)
        {
            return NotFound();
        }

        return Json(new
        {
            id = log.Id,
            recipientNumber = log.RecipientNumber,
            recipientName = log.RecipientName ?? log.Owner?.DisplayName ?? "N/A",
            ownerId = log.OwnerId,
            rptin = log.Owner?.RptinNumber ?? "N/A",
            message = log.Message,
            senderId = log.SenderId ?? "PhilSMS",
            status = log.Status.ToString(),
            gatewayResponse = log.GatewayResponse ?? "None",
            sentAt = log.SentAt.ToString("MMM dd, yyyy hh:mm:ss tt"),
            sentBy = log.SentBy ?? "System"
        });
    }

    // GET: /Sms/SearchOwners
    [HttpGet]
    public async Task<IActionResult> SearchOwners(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            var top = await _context.TaxpayerOwners
                .AsNoTracking()
                .Where(o => o.IsActive && !string.IsNullOrEmpty(o.ContactNo))
                .OrderBy(o => o.LastName)
                .Take(15)
                .Select(o => new
                {
                    id = o.Id,
                    name = o.DisplayName,
                    phone = o.ContactNo,
                    rptin = o.RptinNumber
                })
                .ToListAsync();
            return Json(top);
        }

        var clean = term.Trim().ToLower();
        var results = await _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive &&
                (o.LastName.ToLower().Contains(clean) ||
                 o.FirstName.ToLower().Contains(clean) ||
                 (o.CorporateName != null && o.CorporateName.ToLower().Contains(clean)) ||
                 (o.ContactNo != null && o.ContactNo.Contains(clean)) ||
                 (o.RptinNumber != null && o.RptinNumber.Contains(clean))))
            .Take(20)
            .Select(o => new
            {
                id = o.Id,
                name = o.DisplayName,
                phone = o.ContactNo,
                rptin = o.RptinNumber
            })
            .ToListAsync();

        return Json(results);
    }
}

