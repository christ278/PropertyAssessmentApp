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
    private readonly IDocumentWorkflowService _workflowService;

    public LandHoldingRequestController(
        ILandHoldingRequestService lhRequestService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IOwnerService ownerService,
        IWebHostEnvironment env,
        IWorkflowNotifier workflowNotifier,
        IDocumentWorkflowService workflowService)
    {
        _lhRequestService = lhRequestService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _ownerService = ownerService;
        _env = env;
        _workflowNotifier = workflowNotifier;
        _workflowService = workflowService;
    }

    // GET: /LandHoldingRequest
    public IActionResult Index(string? search, LandHoldingRequestStatus? status)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.TotalCount = _lhRequestService.GetTotalCount();
        ViewBag.PendingCount = _lhRequestService.GetPendingCount();
        ViewBag.ApprovedCount = _lhRequestService.GetApprovedCount();

        var requests = _lhRequestService.GetAll(search, status).ToList();
        var claimedIds = new HashSet<Guid>();
        foreach (var r in requests)
        {
            var lastLog = _workflowService.GetForwardingLogs(r.Id).FirstOrDefault();
            if (lastLog?.IsClaimed == true)
            {
                claimedIds.Add(r.Id);
            }
        }
        ViewBag.ClaimedRequestIds = claimedIds;

        return View(requests);
    }

    // GET: /LandHoldingRequest/Create
    public IActionResult Create()
    {
        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.Owners = _ownerService.GetAll();
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
        if (string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            var now = DateTime.Now;
            request.TrackingNumber = $"LH-{now:yyyyMMddHHmmss}-{now.Second:D2}";
        }
        ModelState.Remove(nameof(request.TrackingNumber));

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
        ViewBag.Owners = _ownerService.GetAll();
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

        var lastLog = _workflowService.GetForwardingLogs(id).FirstOrDefault();
        ViewBag.IsClaimed = lastLog?.IsClaimed ?? false;
        ViewBag.ClaimedByName = lastLog?.AssignedUserName;

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

        var lastLog = _workflowService.GetForwardingLogs(id).FirstOrDefault();
        if (lastLog?.IsClaimed == true)
        {
            var claimedBy = string.IsNullOrWhiteSpace(lastLog.AssignedUserName) ? "another user" : lastLog.AssignedUserName;
            TempData["ErrorMessage"] = $"Transaction {request.TrackingNumber} has already been claimed by {claimedBy} and cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.Properties = _assessmentService.GetAll();
        ViewBag.Owners = _ownerService.GetAll();
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

        ModelState.Remove(nameof(request.TrackingNumber));

        var lastLog = _workflowService.GetForwardingLogs(id).FirstOrDefault();
        if (lastLog?.IsClaimed == true)
        {
            var claimedBy = string.IsNullOrWhiteSpace(lastLog.AssignedUserName) ? "another user" : lastLog.AssignedUserName;
            var msg = $"This transaction has already been claimed by {claimedBy} in the pool and cannot be changed.";
            ModelState.AddModelError(string.Empty, msg);
            ViewBag.ClaimedWarning = msg;
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
        ViewBag.Owners = _ownerService.GetAll();
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

    // AJAX Endpoint to get owner details and matching properties
    [HttpGet]
    public IActionResult GetOwnerDetails(Guid id)
    {
        var owner = _ownerService.GetById(id);
        if (owner == null) return NotFound();

        var ownerProperties = _assessmentService.GetAll()
            .Where(p => p.OwnerId == id ||
                        (!string.IsNullOrWhiteSpace(p.OwnerName) && p.OwnerName.Equals(owner.DisplayName, StringComparison.OrdinalIgnoreCase)))
            .Where(p => !p.IsCancelledAndSuperseded && p.Status != AssessmentStatus.Cancelled)
            .Select(p => new
            {
                id = p.Id,
                taxDeclarationNumber = p.TaxDeclarationNumber,
                propertyIndexNumber = p.PropertyIndexNumber,
                kind = p.Kind.ToString(),
                classification = p.Classification.ToString(),
                location = p.CompleteAddress,
                marketValue = p.MarketValue,
                assessedValue = p.AssessedValue,
                status = p.Status.ToString()
            })
            .ToList();

        return Json(new
        {
            ownerName = owner.DisplayName,
            spouseName = "",
            contactNo = owner.ContactNo ?? "",
            propertyLocation = owner.FullAddress,
            properties = ownerProperties
        });
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
