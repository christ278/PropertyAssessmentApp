using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class TaxDeclarationRequestController : Controller
{
    private readonly ITaxDeclarationRequestService _tdRequestService;
    private readonly IAssessmentService _assessmentService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly IWebHostEnvironment _env;

    public TaxDeclarationRequestController(
        ITaxDeclarationRequestService tdRequestService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IWebHostEnvironment env)
    {
        _tdRequestService = tdRequestService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _env = env;
    }

    // GET: /TaxDeclarationRequest
    public IActionResult Index(string? search, RequestStatus? status)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.TotalCount = _tdRequestService.GetTotalCount();
        ViewBag.PendingCount = _tdRequestService.GetPendingCount();
        ViewBag.ApprovedCount = _tdRequestService.GetApprovedCount();

        var requests = _tdRequestService.GetAll(search, status);
        return View(requests);
    }

    // GET: /TaxDeclarationRequest/Create
    public IActionResult Create()
    {
        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();

        var now = DateTime.Now;
        var trackingNo = $"TD-{now:yyyyMMddHHmmss}-{now.Second:D2}";

        var model = new TaxDeclarationRequest
        {
            TrackingNumber = trackingNo,
            ORDateIssued = now
        };

        return View(model);
    }

    // POST: /TaxDeclarationRequest/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TaxDeclarationRequest request, IFormFile? fileUpload)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _tdRequestService.Add(request, fileUpload, webRoot);
                TempData["SuccessMessage"] = $"Tax Declaration Request {request.TrackingNumber} successfully filed.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error saving tax declaration request: {ex.Message}");
            }
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(request);
    }

    // GET: /TaxDeclarationRequest/Details/5
    public IActionResult Details(Guid id)
    {
        var request = _tdRequestService.GetById(id);
        if (request == null)
        {
            return NotFound();
        }
        return View(request);
    }

    // GET: /TaxDeclarationRequest/Edit/5
    public IActionResult Edit(Guid id)
    {
        var request = _tdRequestService.GetById(id);
        if (request == null)
        {
            return NotFound();
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(request);
    }

    // POST: /TaxDeclarationRequest/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, TaxDeclarationRequest request, IFormFile? fileUpload)
    {
        if (id != request.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _tdRequestService.Update(request, fileUpload, webRoot);
                TempData["SuccessMessage"] = $"Tax Declaration Request {request.TrackingNumber} updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating tax declaration request: {ex.Message}");
            }
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(request);
    }

    // POST: /TaxDeclarationRequest/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        _tdRequestService.Delete(id);
        TempData["SuccessMessage"] = "Request deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /TaxDeclarationRequest/DownloadAttachment/5
    public IActionResult DownloadAttachment(Guid id)
    {
        var item = _tdRequestService.GetById(id);
        if (item == null || string.IsNullOrEmpty(item.PhysicalStoragePath) || !System.IO.File.Exists(item.PhysicalStoragePath))
        {
            return NotFound("File attachment not found on server.");
        }

        var fileName = item.AttachmentFileName ?? "attachment_file";
        return PhysicalFile(item.PhysicalStoragePath, "application/octet-stream", fileName);
    }

    // AJAX Endpoint to get property details
    [HttpGet]
    public IActionResult GetPropertyDetails(Guid id)
    {
        var prop = _assessmentService.GetById(id);
        if (prop == null) return NotFound();

        return Json(new
        {
            taxDeclarationNumber = prop.TaxDeclarationNumber,
            ownerName = prop.OwnerName,
            ownerPhone = prop.OwnerPhone,
            lotNo = prop.LotNo,
            propertyLocation = $"{prop.StreetNo}, {prop.DistrictOrBarangay}, {prop.Municipality}, {prop.ProvinceCity}"
        });
    }
}
