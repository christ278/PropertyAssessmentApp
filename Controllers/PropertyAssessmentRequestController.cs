using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class PropertyAssessmentRequestController : Controller
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };
    private readonly IPropertyAssessmentRequestService _rpaService;
    private readonly IAssessmentService _assessmentService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly IOwnerService _ownerService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PropertyAssessmentRequestController(
        IPropertyAssessmentRequestService rpaService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IOwnerService ownerService,
        ApplicationDbContext context,
        IWebHostEnvironment env)
    {
        _rpaService = rpaService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _ownerService = ownerService;
        _context = context;
        _env = env;
    }

    // GET: /PropertyAssessmentRequest
    public IActionResult Index(string? search, PropertyAssessmentStatus? status)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.TotalCount = _rpaService.GetTotalCount();
        ViewBag.PendingCount = _rpaService.GetPendingCount();
        ViewBag.ApprovedCount = _rpaService.GetApprovedCount();

        var list = _rpaService.GetAll(search, status);
        return View(list);
    }

    // GET: /PropertyAssessmentRequest/Create
    public IActionResult Create()
    {
        // Only APPROVED (Active) Tax Declarations, top 50 for fast initial page render
        ViewBag.Properties = _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking()
            .Where(t => t.Status == AssessmentStatus.Active)
            .OrderByDescending(t => t.DateAssessed)
            .Take(50)
            .ToList();

        ViewBag.OrderPayments = _orderPaymentService.GetAll();

        // Only ACTIVE Registered Owners, top 50 for fast initial page render
        ViewBag.RegisteredOwners = _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.CorporateName)
            .Take(50)
            .ToList();

        var now = DateTime.Now;
        var trackingNo = $"RPA-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var model = new PropertyAssessmentRequest
        {
            TrackingNumber = trackingNo,
            TransactionTypeCode = "DC",
            TransactionTypeName = "Discovery/New Declaration",
            Kind = "Land",
            Partitions = 1,
            ORDateIssued = now,
            Status = PropertyAssessmentStatus.Receiving
        };

        return View(model);
    }

    // POST: /PropertyAssessmentRequest/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PropertyAssessmentRequest request, IFormFile? fileUpload)
    {
        if (!string.IsNullOrWhiteSpace(request.TransactionTypeCode) &&
            PropertyAssessmentRequest.StandardTransactionCodes.TryGetValue(request.TransactionTypeCode.Trim().ToUpperInvariant(), out var stdName))
        {
            request.TransactionTypeName = stdName;
        }

        // Validate that at least one Reference Property is selected
        if (string.IsNullOrWhiteSpace(request.SelectedRefPropertyIdsJson) || request.SelectedRefPropertyIdsJson.Trim() == "[]")
        {
            if (!request.RefTaxDeclarationId.HasValue)
            {
                ModelState.AddModelError("RefTaxDeclarationId", "At least one Reference Property must be selected.");
            }
        }

        // Validate that O.R. Number is provided if the request is being forwarded beyond Receiving
        if (request.Status > PropertyAssessmentStatus.Receiving && string.IsNullOrWhiteSpace(request.ORNumber))
        {
            ModelState.AddModelError(nameof(request.ORNumber), "Official Receipt (O.R.) Number is required before forwarding this assessment request beyond Receiving.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _rpaService.Add(request, fileUpload, webRoot);
                TempData["SuccessMessage"] = $"Property Assessment Request '{request.TrackingNumber}' successfully filed. You can now claim it in the Assessment Workflow Pool.";
                return RedirectToAction("Pool", "Workflow", new { divisionId = 1 });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error saving assessment request: {ex.Message}");
            }
        }

        ViewBag.Properties = _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking()
            .Where(t => t.Status == AssessmentStatus.Active)
            .OrderByDescending(t => t.DateAssessed)
            .Take(50)
            .ToList();

        ViewBag.OrderPayments = _orderPaymentService.GetAll();

        ViewBag.RegisteredOwners = _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.CorporateName)
            .Take(50)
            .ToList();

        return View(request);
    }

    // GET: /PropertyAssessmentRequest/Details/5
    public IActionResult Details(Guid id)
    {
        var item = _rpaService.GetById(id);
        if (item == null) return NotFound();
        return View(item);
    }

    // GET: /PropertyAssessmentRequest/Edit/5
    public IActionResult Edit(Guid id)
    {
        var item = _rpaService.GetById(id);
        if (item == null) return NotFound();

        List<Guid> selectedIds = new();
        if (!string.IsNullOrWhiteSpace(item.SelectedRefPropertyIdsJson))
        {
            try { selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(item.SelectedRefPropertyIdsJson) ?? new(); } catch { }
        }
        if (item.RefTaxDeclarationId.HasValue && !selectedIds.Contains(item.RefTaxDeclarationId.Value))
        {
            selectedIds.Add(item.RefTaxDeclarationId.Value);
        }

        ViewBag.Properties = _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking()
            .Where(t => selectedIds.Contains(t.Id) || t.Status == AssessmentStatus.Active)
            .OrderByDescending(t => t.DateAssessed)
            .Take(50)
            .ToList();

        ViewBag.OrderPayments = _orderPaymentService.GetAll();

        ViewBag.RegisteredOwners = _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.CorporateName)
            .Take(50)
            .ToList();

        return View(item);
    }

    // POST: /PropertyAssessmentRequest/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, PropertyAssessmentRequest request, IFormFile? fileUpload)
    {
        if (id != request.Id) return BadRequest();

        if (!string.IsNullOrWhiteSpace(request.TransactionTypeCode) &&
            PropertyAssessmentRequest.StandardTransactionCodes.TryGetValue(request.TransactionTypeCode.Trim().ToUpperInvariant(), out var stdName))
        {
            request.TransactionTypeName = stdName;
        }

        // Validate that O.R. Number is provided if the request is being forwarded beyond Receiving
        if (request.Status > PropertyAssessmentStatus.Receiving && string.IsNullOrWhiteSpace(request.ORNumber))
        {
            ModelState.AddModelError(nameof(request.ORNumber), "Official Receipt (O.R.) Number is required before forwarding this assessment request beyond Receiving.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _rpaService.Update(request, fileUpload, webRoot);
                TempData["SuccessMessage"] = $"Property Assessment Request '{request.TrackingNumber}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating request: {ex.Message}");
            }
        }

        List<Guid> selectedIds = new();
        if (!string.IsNullOrWhiteSpace(request.SelectedRefPropertyIdsJson))
        {
            try { selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(request.SelectedRefPropertyIdsJson) ?? new(); } catch { }
        }
        if (request.RefTaxDeclarationId.HasValue && !selectedIds.Contains(request.RefTaxDeclarationId.Value))
        {
            selectedIds.Add(request.RefTaxDeclarationId.Value);
        }

        ViewBag.Properties = _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking()
            .Where(t => selectedIds.Contains(t.Id) || t.Status == AssessmentStatus.Active)
            .OrderByDescending(t => t.DateAssessed)
            .Take(50)
            .ToList();

        ViewBag.OrderPayments = _orderPaymentService.GetAll();

        ViewBag.RegisteredOwners = _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.CorporateName)
            .Take(50)
            .ToList();

        return View(request);
    }

    // GET: /PropertyAssessmentRequest/EditPartition
    public IActionResult EditPartition(Guid? requestId, int index, string? returnUrl)
    {
        ViewBag.RequestId = requestId;
        ViewBag.PartitionIndex = index;
        ViewBag.ReturnUrl = returnUrl;

        ViewBag.BuildingClassifications = _rpaService.GetBuildingClassifications()
            .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = b.ClassificationName,
                Text = $"{b.ClassificationCode} – {b.ClassificationName}"
            })
            .ToList();

        ViewBag.BuildingAdditionalItems = _rpaService.GetBuildingAdditionalItems();
        ViewBag.BuildingStructuralTypes = _rpaService.GetBuildingStructuralTypes();
        ViewBag.BuildingTypes = _rpaService.GetBuildingTypes();
        ViewBag.BuildingPropertyTypesAccordUse = _rpaService.GetBuildingPropertyTypesAccordUse();

        // Land Appraisal Reference Prerequisites (CAST Standard)
        ViewBag.LandClassifications = _context.LandClassifications.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToList();
        ViewBag.LandSubClassifications = _context.LandSubClassifications.AsNoTracking().Where(c => c.IsActive).ToList();
        ViewBag.LandActualUses = _context.LandActualUses.AsNoTracking().Where(c => c.IsActive).ToList();
        ViewBag.LandAssessmentLevels = _context.LandAssessmentLevels.AsNoTracking().Where(c => c.IsActive).ToList();
        ViewBag.LandAdjustments = _context.LandAdjustmentFactors.AsNoTracking().Where(c => c.IsActive).ToList();
        ViewBag.LandImprovementTypes = _context.LandImprovementTypes.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.TypeDescription).ToList();
        ViewBag.Barangays = _context.Barangays.AsNoTracking().OrderBy(b => b.Name).ToList();

        AssessmentPartitionItem partition = new AssessmentPartitionItem();

        if (requestId.HasValue)
        {
            var req = _rpaService.GetById(requestId.Value);
            if (req != null && !string.IsNullOrWhiteSpace(req.GeneratedPartitionsJson))
            {
                try
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<AssessmentPartitionItem>>(req.GeneratedPartitionsJson, _jsonOptions);
                    if (list != null && index >= 0 && index < list.Count)
                    {
                        partition = list[index];
                    }
                }
                catch { }
            }
        }

        return View(partition);
    }

    // POST: /PropertyAssessmentRequest/UploadPartitionSketch
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadPartitionSketch(IFormFile? sketchFile)
    {
        if (sketchFile == null || sketchFile.Length == 0)
        {
            return Json(new { success = false, message = "No file received." });
        }

        try
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "PropertyAssessment");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var ext = Path.GetExtension(sketchFile.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
            var fileName = $"sketch_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await sketchFile.CopyToAsync(stream);
            }

            var webPath = $"/uploads/PropertyAssessment/{fileName}";
            return Json(new { success = true, filePath = webPath, fileName = sketchFile.FileName });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /PropertyAssessmentRequest/SavePartition
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePartition(AssessmentPartitionItem item, int index, Guid? requestId, string? returnUrl, IFormFile? sketchFile)
    {
        // 1. Process uploaded sketch file if provided directly in form submit
        if (sketchFile != null && sketchFile.Length > 0)
        {
            try
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "PropertyAssessment");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var ext = Path.GetExtension(sketchFile.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                var fileName = $"sketch_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
                var fullPath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await sketchFile.CopyToAsync(stream);
                }

                item.BuildingFloorPlanSketchPath = $"/uploads/PropertyAssessment/{fileName}";
                item.BuildingFloorPlanSketchBase64 = null;
            }
            catch { }
        }
        // 2. If base64 sketch was submitted without a file or existing path, save base64 to disk as an image file
        else if (!string.IsNullOrWhiteSpace(item.BuildingFloorPlanSketchBase64) &&
                 string.IsNullOrWhiteSpace(item.BuildingFloorPlanSketchPath) &&
                 item.BuildingFloorPlanSketchBase64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "PropertyAssessment");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var parts = item.BuildingFloorPlanSketchBase64.Split(',');
                if (parts.Length > 1)
                {
                    var mimeHeader = parts[0];
                    var ext = ".png";
                    if (mimeHeader.Contains("jpeg") || mimeHeader.Contains("jpg")) ext = ".jpg";
                    else if (mimeHeader.Contains("webp")) ext = ".webp";
                    else if (mimeHeader.Contains("pdf")) ext = ".pdf";

                    var bytes = Convert.FromBase64String(parts[1]);
                    var fileName = $"sketch_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
                    var fullPath = Path.Combine(uploadsDir, fileName);
                    await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

                    item.BuildingFloorPlanSketchPath = $"/uploads/PropertyAssessment/{fileName}";
                    item.BuildingFloorPlanSketchBase64 = null;
                }
            }
            catch { }
        }

        // 3. Update in Database if requestId exists
        if (requestId.HasValue)
        {
            var req = _rpaService.GetById(requestId.Value);
            if (req != null)
            {
                try
                {
                    var list = !string.IsNullOrWhiteSpace(req.GeneratedPartitionsJson)
                        ? System.Text.Json.JsonSerializer.Deserialize<List<AssessmentPartitionItem>>(req.GeneratedPartitionsJson, _jsonOptions) ?? new()
                        : new List<AssessmentPartitionItem>();

                    if (index >= 0 && index < list.Count)
                    {
                        list[index] = item;
                    }
                    else
                    {
                        list.Add(item);
                    }
                    req.GeneratedPartitionsJson = System.Text.Json.JsonSerializer.Serialize(list, _jsonOptions);
                    _rpaService.Update(req, null, null);
                    TempData["SuccessMessage"] = $"Partition {item.SystemGeneratedTDN} ({item.PartitionIndex}) appraisal updated successfully!";
                }
                catch { }
            }
        }
        else
        {
            // Draft / Create Workflow: Pass serialized partition back via TempData for guaranteed persistence
            TempData["LastUpdatedPartitionJson"] = System.Text.Json.JsonSerializer.Serialize(item, _jsonOptions);
            TempData["LastUpdatedPartitionIndex"] = index;
            TempData["SuccessMessage"] = $"Partition appraisal updated successfully!";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (requestId.HasValue)
        {
            return RedirectToAction(nameof(Edit), new { id = requestId.Value });
        }

        return RedirectToAction(nameof(Create));
    }

    // POST: /PropertyAssessmentRequest/Partition/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Partition(Guid id, int partitionCount)
    {
        var createdTds = _rpaService.PartitionAssessment(id, partitionCount).ToList();
        if (createdTds.Any())
        {
            TempData["SuccessMessage"] = $"Successfully generated {createdTds.Count} new Tax Declarations from assessment partitions!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to partition property. Ensure reference property exists.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /PropertyAssessmentRequest/ForwardRequest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForwardRequest(Guid id, string? orNumber, DateTime? orDateIssued, PropertyAssessmentStatus targetStatus = PropertyAssessmentStatus.Replotting, string? remarks = null, string? appraiser = null, string? returnUrl = null)
    {
        var item = _rpaService.GetById(id);
        if (item == null) return NotFound();

        var effectiveOr = !string.IsNullOrWhiteSpace(orNumber) ? orNumber.Trim() : item.ORNumber;
        if (string.IsNullOrWhiteSpace(effectiveOr))
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Official Receipt (O.R.) Number is required before forwarding this request to GIS Replotting / Mapping." });
            }
            TempData["ErrorMessage"] = "Official Receipt (O.R.) Number is required before forwarding this request to GIS Replotting / Mapping.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _rpaService.ForwardRequest(id, effectiveOr, orDateIssued, targetStatus, remarks, appraiser);

        var msg = $"Assessment Request '{item.TrackingNumber}' with O.R. #{effectiveOr} successfully forwarded to {targetStatus}.";
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, message = msg, orNumber = effectiveOr, newStatus = targetStatus.ToString() });
        }

        TempData["SuccessMessage"] = msg;
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /PropertyAssessmentRequest/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(Guid id, PropertyAssessmentStatus newStatus, string? remarks, string? appraiser, string? orNumber = null, DateTime? orDateIssued = null)
    {
        var item = _rpaService.GetById(id);
        if (item == null) return NotFound();

        var effectiveOr = !string.IsNullOrWhiteSpace(orNumber) ? orNumber.Trim() : item.ORNumber;
        if (newStatus > PropertyAssessmentStatus.Receiving && string.IsNullOrWhiteSpace(effectiveOr))
        {
            TempData["ErrorMessage"] = $"Official Receipt (O.R.) Number is required before forwarding this request to '{newStatus}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!string.IsNullOrWhiteSpace(orNumber))
        {
            _rpaService.ForwardRequest(id, effectiveOr, orDateIssued, newStatus, remarks, appraiser);
        }
        else
        {
            _rpaService.UpdateStatus(id, newStatus, remarks, appraiser);
        }

        TempData["SuccessMessage"] = $"Assessment Request status updated to '{newStatus}'.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /PropertyAssessmentRequest/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        _rpaService.Delete(id);
        TempData["SuccessMessage"] = "Property Assessment Request deleted.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /PropertyAssessmentRequest/DownloadAttachment/5
    public IActionResult DownloadAttachment(Guid id)
    {
        var item = _rpaService.GetById(id);
        if (item == null || string.IsNullOrEmpty(item.PhysicalStoragePath) || !System.IO.File.Exists(item.PhysicalStoragePath))
        {
            TempData["ErrorMessage"] = "Attachment file not found on server.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var bytes = System.IO.File.ReadAllBytes(item.PhysicalStoragePath);
        var contentType = "application/octet-stream";
        return File(bytes, contentType, item.AttachmentFileName ?? "assessment_attachment");
    }

    // GET: /PropertyAssessmentRequest/GetBuildingClassifications
    [HttpGet]
    public IActionResult GetBuildingClassifications()
    {
        var list = _rpaService.GetBuildingClassifications();
        return Json(list);
    }

    // GET: /PropertyAssessmentRequest/GetBuildingAdditionalItems
    [HttpGet]
    public IActionResult GetBuildingAdditionalItems()
    {
        var list = _rpaService.GetBuildingAdditionalItems();
        return Json(list);
    }

    // GET: /PropertyAssessmentRequest/GetUnitValue
    [HttpGet]
    public IActionResult GetUnitValue(string structuralType, string classification, string mode = "Min")
    {
        var val = _rpaService.GetUnitValue(structuralType, classification, mode);
        return Json(new { success = true, unitValue = val });
    }

    // GET: /PropertyAssessmentRequest/GetAssessmentLevel
    [HttpGet]
    public IActionResult GetAssessmentLevel(string actualUse, decimal marketValue)
    {
        var level = _rpaService.GetAssessmentLevel(actualUse, marketValue);
        return Json(new { success = true, assessmentLevel = level });
    }

    // GET: /PropertyAssessmentRequest/GetDepreciationRate
    [HttpGet]
    public IActionResult GetDepreciationRate(string structuralType, string actualUse, int age, string maintenance = "AVERAGE")
    {
        var rate = _rpaService.GetDepreciationRate(structuralType, actualUse, age, maintenance);
        return Json(new { success = true, depreciationRate = rate });
    }

    // GET: /PropertyAssessmentRequest/GetBuildingStructuralTypes
    [HttpGet]
    public IActionResult GetBuildingStructuralTypes()
    {
        var list = _rpaService.GetBuildingStructuralTypes();
        return Json(list);
    }

    // GET: /PropertyAssessmentRequest/GetBuildingTypes
    [HttpGet]
    public IActionResult GetBuildingTypes()
    {
        var list = _rpaService.GetBuildingTypes();
        return Json(list);
    }

    // GET: /PropertyAssessmentRequest/GetOwnerDetails
    [HttpGet]
    public JsonResult GetOwnerDetails(Guid id)
    {
        var owner = _ownerService.GetById(id);
        if (owner == null)
        {
            return Json(null);
        }

        return Json(new
        {
            name = owner.DisplayName,
            tin = owner.TaxIdentNo,
            phone = owner.ContactNo ?? string.Empty,
            email = owner.Email ?? string.Empty,
            address = owner.FullAddress,
            street = owner.Address,
            barangay = owner.BarangayName,
            city = owner.CityMunicipalityName,
            province = owner.ProvinceName
        });
    }

    // GET: /PropertyAssessmentRequest/SearchApprovedProperties
    // High-performance remote search for thousands of approved records
    [HttpGet]
    public IActionResult SearchApprovedProperties(string? term, int limit = 30)
    {
        IQueryable<TaxDeclaration> query = _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking()
            .Where(t => t.Status == AssessmentStatus.Active); // ONLY APPROVED (Active roll)

        if (!string.IsNullOrWhiteSpace(term))
        {
            var clean = term.Trim();
            query = query.Where(t => t.TaxDeclarationNumber.Contains(clean)
                                  || t.PropertyIndexNumber.Contains(clean)
                                  || t.OwnerName.Contains(clean)
                                  || (t.BuildingDetails != null && t.BuildingDetails.DistrictOrBarangay != null && t.BuildingDetails.DistrictOrBarangay.Contains(clean))
                                  || (t.LandDetails != null && t.LandDetails.DistrictOrBarangay != null && t.LandDetails.DistrictOrBarangay.Contains(clean)));
        }

        var results = query
            .OrderByDescending(t => t.DateAssessed)
            .Take(limit)
            .ToList()
            .Select(t => new
            {
                id = t.Id.ToString(),
                tdn = t.TaxDeclarationNumber,
                pin = t.PropertyIndexNumber,
                owner = t.OwnerName,
                location = t.CompleteAddress,
                kind = t.Kind.ToString(),
                classification = t.Classification,
                area = t.AreaSqMeters,
                unitVal = t.UnitValue,
                marketVal = t.MarketValue,
                assessLevel = t.AssessmentLevelPercent,
                assessedVal = t.AssessedValue,
                barangay = t.DistrictOrBarangay,
                display = $"{t.TaxDeclarationNumber} - {t.OwnerName} ({t.PropertyIndexNumber}) [{t.Kind} • {t.Classification} • {t.AreaSqMeters:N0} sqm]"
            })
            .ToList();

        return Json(results);
    }

    // GET: /PropertyAssessmentRequest/SearchRegisteredOwners
    // High-performance remote search for thousands of active registered owners
    [HttpGet]
    public IActionResult SearchRegisteredOwners(string? term, int limit = 30)
    {
        IQueryable<Owner> query = _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive); // ONLY ACTIVE REGISTERED OWNERS

        if (!string.IsNullOrWhiteSpace(term))
        {
            var clean = term.Trim();
            query = query.Where(o => (o.FirstName != null && o.FirstName.Contains(clean))
                                  || (o.LastName != null && o.LastName.Contains(clean))
                                  || (o.CorporateName != null && o.CorporateName.Contains(clean))
                                  || (o.TaxIdentNo != null && o.TaxIdentNo.Contains(clean))
                                  || (o.RptinNumber != null && o.RptinNumber.Contains(clean))
                                  || (o.BarangayName != null && o.BarangayName.Contains(clean)));
        }

        var results = query
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.CorporateName)
            .Take(limit)
            .ToList()
            .Select(o => new
            {
                id = o.Id.ToString(),
                name = o.DisplayName,
                tin = o.TaxIdentNo,
                phone = o.ContactNo ?? string.Empty,
                email = o.Email ?? string.Empty,
                address = o.FullAddress,
                street = o.Address,
                barangay = o.BarangayName,
                city = o.CityMunicipalityName,
                province = o.ProvinceName,
                display = $"{o.DisplayName} (TIN: {o.TaxIdentNo} | {o.BarangayName})"
            })
            .ToList();

        return Json(results);
    }
}
