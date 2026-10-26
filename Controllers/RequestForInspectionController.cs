using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class RequestForInspectionController : Controller
{
    private readonly IRequestForInspectionService _rfiService;
    private readonly IAssessmentService _assessmentService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly IOwnerService _ownerService;
    private readonly IWebHostEnvironment _env;

    public RequestForInspectionController(
        IRequestForInspectionService rfiService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IOwnerService ownerService,
        IWebHostEnvironment env)
    {
        _rfiService = rfiService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _ownerService = ownerService;
        _env = env;
    }

    // GET: /RequestForInspection
    public IActionResult Index(string? search, RequestForInspectionStatus? status)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.TotalCount = _rfiService.GetTotalCount();
        ViewBag.PendingCount = _rfiService.GetPendingCount();
        ViewBag.ApprovedCount = _rfiService.GetApprovedCount();

        var list = _rfiService.GetAll(search, status);
        return View(list);
    }

    // GET: /RequestForInspection/Create
    public IActionResult Create()
    {
        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();

        var now = DateTime.Now;
        var trackingNo = $"RFI-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var model = new RequestForInspection
        {
            TrackingNumber = trackingNo,
            TransactionCode = "INSP",
            TransactionNo = $"TX-{now:yyyyMMdd}-{new Random().Next(100, 999)}",
            ORDateIssued = now,
            Status = RequestForInspectionStatus.Receiving
        };

        return View(model);
    }

    // POST: /RequestForInspection/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(RequestForInspection request, IFormFile? fileUpload)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _rfiService.Add(request, fileUpload, webRoot);
                TempData["SuccessMessage"] = $"Request for Inspection '{request.TrackingNumber}' successfully filed.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error saving inspection request: {ex.Message}");
            }
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(request);
    }

    // GET: /RequestForInspection/Details/5
    public IActionResult Details(Guid id)
    {
        var item = _rfiService.GetById(id);
        if (item == null) return NotFound();
        return View(item);
    }

    // GET: /RequestForInspection/Edit/5
    public IActionResult Edit(Guid id)
    {
        var item = _rfiService.GetById(id);
        if (item == null) return NotFound();

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(item);
    }

    // POST: /RequestForInspection/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, RequestForInspection request, IFormFile? fileUpload)
    {
        if (id != request.Id) return BadRequest();

        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _rfiService.Update(request, fileUpload, webRoot);
                TempData["SuccessMessage"] = $"Request for Inspection '{request.TrackingNumber}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating request: {ex.Message}");
            }
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(request);
    }

    // POST: /RequestForInspection/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(Guid id, RequestForInspectionStatus newStatus, string? remarks, string? staff)
    {
        _rfiService.UpdateStatus(id, newStatus, remarks, staff);
        TempData["SuccessMessage"] = $"Inspection Request workflow stage updated to '{newStatus}'.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /RequestForInspection/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        _rfiService.Delete(id);
        TempData["SuccessMessage"] = "Inspection Request deleted.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /RequestForInspection/DownloadAttachment/5
    public IActionResult DownloadAttachment(Guid id)
    {
        var item = _rfiService.GetById(id);
        if (item == null || string.IsNullOrEmpty(item.PhysicalStoragePath) || !System.IO.File.Exists(item.PhysicalStoragePath))
        {
            TempData["ErrorMessage"] = "Attachment file not found on server.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var bytes = System.IO.File.ReadAllBytes(item.PhysicalStoragePath);
        var contentType = "application/octet-stream";
        return File(bytes, contentType, item.AttachmentFileName ?? "inspection_attachment");
    }
}
