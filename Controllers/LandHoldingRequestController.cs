using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class LandHoldingRequestController : Controller
{
    private readonly ILandHoldingRequestService _lhRequestService;
    private readonly IAssessmentService _assessmentService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly IOwnerService _ownerService;
    private readonly IWebHostEnvironment _env;
    private readonly IWorkflowNotifier _workflowNotifier;

    public LandHoldingRequestController(
        ILandHoldingRequestService lhRequestService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IOwnerService ownerService,
        IWebHostEnvironment env,
        IWorkflowNotifier workflowNotifier)
    {
        _lhRequestService = lhRequestService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _ownerService = ownerService;
        _env = env;
        _workflowNotifier = workflowNotifier;
    }

    // GET: /LandHoldingRequest
    public IActionResult Index(string? search, LandHoldingRequestStatus? status)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.TotalCount = _lhRequestService.GetTotalCount();
        ViewBag.PendingCount = _lhRequestService.GetPendingCount();
        ViewBag.ApprovedCount = _lhRequestService.GetApprovedCount();

        var requests = _lhRequestService.GetAll(search, status);
        return View(requests);
    }

    // GET: /LandHoldingRequest/Create
    public IActionResult Create()
    {
        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();

        var now = DateTime.Now;
        var trackingNo = $"LH-{now:yyyyMMddHHmmss}-{now.Second:D2}";

        var model = new LandHoldingRequest
        {
            TrackingNumber = trackingNo,
            ORDateIssued = now
        };

        return View(model);
    }

    // POST: /LandHoldingRequest/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LandHoldingRequest request, IFormFile? fileUpload)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _lhRequestService.Add(request, fileUpload, webRoot);
                await _workflowNotifier.NotifyTransactionCreatedAsync(request.Id, "LandHoldingRequest", request.TrackingNumber, 3, request.OwnerName);
                TempData["SuccessMessage"] = $"Land Holding Request {request.TrackingNumber} successfully filed.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error saving land holding request: {ex.Message}");
            }
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(request);
    }

    // GET: /LandHoldingRequest/Details/5
    public IActionResult Details(Guid id)
    {
        var request = _lhRequestService.GetById(id);
        if (request == null)
        {
            return NotFound();
        }
        return View(request);
    }

    // GET: /LandHoldingRequest/Edit/5
    public IActionResult Edit(Guid id)
    {
        var request = _lhRequestService.GetById(id);
        if (request == null)
        {
            return NotFound();
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(request);
    }

    // POST: /LandHoldingRequest/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, LandHoldingRequest request, IFormFile? fileUpload)
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
                _lhRequestService.Update(request, fileUpload, webRoot);
                TempData["SuccessMessage"] = $"Land Holding Request {request.TrackingNumber} updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating land holding request: {ex.Message}");
            }
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.OrderPayments = _orderPaymentService.GetAll();
        return View(request);
    }

    // POST: /LandHoldingRequest/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        _lhRequestService.Delete(id);
        TempData["SuccessMessage"] = "Request deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /LandHoldingRequest/DownloadAttachment/5
    public IActionResult DownloadAttachment(Guid id)
    {
        var item = _lhRequestService.GetById(id);
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

        // Try lookup owner to get spouse name if available
        var owner = _ownerService.GetAll().FirstOrDefault(o => o.DisplayName.Equals(prop.OwnerName, StringComparison.OrdinalIgnoreCase));
        var spouseName = "";

        return Json(new
        {
            taxDeclarationNumber = prop.TaxDeclarationNumber,
            propertyIndexNumber = prop.PropertyIndexNumber,
            ownerName = prop.OwnerName,
            spouseName = spouseName,
            contactNo = prop.OwnerPhone,
            propertyLocation = $"{prop.StreetNo}, {prop.DistrictOrBarangay}, {prop.Municipality}, {prop.ProvinceCity}"
        });
    }
}
