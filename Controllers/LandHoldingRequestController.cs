using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Helpers;
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
    private readonly ApplicationDbContext _context;
    private readonly IRPTCertificatePdfService _certPdfService;

    public LandHoldingRequestController(
        ILandHoldingRequestService lhRequestService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IOwnerService ownerService,
        IWebHostEnvironment env,
        IWorkflowNotifier workflowNotifier,
        IDocumentWorkflowService workflowService,
        ApplicationDbContext context,
        IRPTCertificatePdfService certPdfService)
    {
        _lhRequestService = lhRequestService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _ownerService = ownerService;
        _env = env;
        _workflowNotifier = workflowNotifier;
        _workflowService = workflowService;
        _context = context;
        _certPdfService = certPdfService;
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

    // GET: /LandHoldingRequest/PrintCertificate/5
    [HttpGet]
    public IActionResult PrintCertificate(Guid id, int? reportId = null)
    {
        var (certificateModel, errorResult) = BuildCertificateForRequest(id, reportId);
        if (errorResult != null) return errorResult;

        ViewBag.CustomPdfUrl = Url.Action(nameof(PrintCertificatePdf), "LandHoldingRequest", new { id, reportId = certificateModel!.ReportId });
        ViewBag.CustomDownloadPdfUrl = Url.Action(nameof(PrintCertificatePdf), "LandHoldingRequest", new { id, reportId = certificateModel.ReportId, download = true });
        ViewBag.CustomPrintHtmlUrl = Url.Action(nameof(PrintCertificateHtml), "LandHoldingRequest", new { id, reportId = certificateModel.ReportId });
        ViewBag.CustomBackUrl = Url.Action("MyTransactions", "Workflow");
        ViewBag.CustomBackLabel = "Back to My Transactions";

        return View("~/Views/RPTReport/ReportDetails.cshtml", certificateModel);
    }

    // GET: /LandHoldingRequest/PrintCertificatePdf/5
    [HttpGet]
    public IActionResult PrintCertificatePdf(Guid id, int? reportId = null, bool download = false)
    {
        var (certificateModel, errorResult) = BuildCertificateForRequest(id, reportId);
        if (errorResult != null) return errorResult;

        var pdfBytes = _certPdfService.GenerateCertificatePdf(certificateModel!);
        var tdn = certificateModel!.Details.Items.FirstOrDefault()?.TaxDeclarationNo
            ?? certificateModel.AssessmentDetails?.TaxDeclarationNo
            ?? certificateModel.NoImprovementDetails?.ArpNo
            ?? id.ToString("N");
        var safeTitle = string.Join("_", (certificateModel.RawReportTitle ?? "Certificate").Split(Path.GetInvalidFileNameChars()));
        var safeTdn = string.Join("_", tdn.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{safeTitle}_{safeTdn}.pdf";

        if (download)
        {
            return File(pdfBytes, "application/pdf", fileName);
        }

        return File(pdfBytes, "application/pdf");
    }

    // GET: /LandHoldingRequest/PrintCertificateHtml/5
    [HttpGet]
    public IActionResult PrintCertificateHtml(Guid id, int? reportId = null)
    {
        var (certificateModel, errorResult) = BuildCertificateForRequest(id, reportId);
        if (errorResult != null) return errorResult;

        return View("~/Views/RPTReport/Print.cshtml", certificateModel);
    }

    private (OfficialCertificateViewModel? Model, IActionResult? ErrorResult) BuildCertificateForRequest(Guid id, int? reportId)
    {
        var item = _lhRequestService.GetById(id);
        if (item == null) return (null, NotFound());

        int targetReportId = reportId ?? (
            item.CertNoLandholdings ? 5 :
            item.CertNoImprovementFromDeath ? 2 :
            item.CertAggregateAgriLandholdings ? 7 :
            item.CertTotalLandholdings ? 6 :
            item.CertTypeTaxDeclaration ? 1 :
            6);

        var report = _context.RPTReports.FirstOrDefault(r => r.RPTReportId == targetReportId)
            ?? _context.RPTReports.FirstOrDefault();

        if (report == null)
        {
            return (null, NotFound("No RPT Report template configured."));
        }

        var tdnList = new List<string>();
        var idList = new List<Guid>();

        if (item.TaxDeclarationId.HasValue)
        {
            idList.Add(item.TaxDeclarationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(item.SelectedPropertiesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(item.SelectedPropertiesJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (el.TryGetProperty("id", out var idProp) || el.TryGetProperty("Id", out idProp))
                        {
                            if (Guid.TryParse(idProp.GetString(), out var parsedId) && !idList.Contains(parsedId))
                            {
                                idList.Add(parsedId);
                            }
                        }

                        if (el.TryGetProperty("taxDeclarationNumber", out var tdnProp) || el.TryGetProperty("TaxDeclarationNumber", out tdnProp))
                        {
                            var tdnVal = tdnProp.GetString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(tdnVal) && !tdnList.Contains(tdnVal, StringComparer.OrdinalIgnoreCase))
                            {
                                tdnList.Add(tdnVal);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore malformed JSON and fallback to TaxDeclarationNumber
            }
        }

        if (!string.IsNullOrWhiteSpace(item.TaxDeclarationNumber))
        {
            foreach (var part in item.TaxDeclarationNumber.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!string.IsNullOrWhiteSpace(part) && !tdnList.Contains(part, StringComparer.OrdinalIgnoreCase))
                {
                    tdnList.Add(part);
                }
            }
        }

        var matchedProperties = new List<TaxDeclaration>();
        if (idList.Count > 0 || tdnList.Count > 0)
        {
            matchedProperties = _context.TaxDeclarations
                .Include(t => t.LandDetails)
                .Where(t => idList.Contains(t.Id) || tdnList.Contains(t.TaxDeclarationNumber))
                .OrderBy(t => t.TaxDeclarationNumber)
                .ToList();
        }

        TaxDeclaration primaryTd;
        IEnumerable<TaxDeclaration> relatedProperties;

        if (matchedProperties.Count > 0)
        {
            primaryTd = matchedProperties[0];
            relatedProperties = matchedProperties;
        }
        else
        {
            primaryTd = new TaxDeclaration
            {
                TaxDeclarationNumber = item.TaxDeclarationNumber ?? string.Empty,
                OwnerName = item.OwnerName ?? string.Empty,
                OwnerAddress = item.PropertyLocation ?? string.Empty,
                PropertyIndexNumber = item.PropertyIndexNumber ?? string.Empty,
                StreetNo = item.PropertyLocation ?? string.Empty
            };
            relatedProperties = Enumerable.Empty<TaxDeclaration>();
        }

        var certFee = item.TotalFeesAmount > 0 ? item.TotalFeesAmount : (decimal?)null;
        var certificateModel = ReportPrintHelper.BuildCertificateModel(
            primaryTd,
            report,
            item.RequesterName,
            item.Purposes,
            item.ORNumber,
            certFee,
            relatedProperties);

        return (certificateModel, null);
    }
}
