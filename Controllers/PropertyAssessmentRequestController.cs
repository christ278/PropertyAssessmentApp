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

    private static List<AssessmentPartitionItem> ParsePartitionList(string? json) => PartitionJsonHelper.ParseList(json);
    private static AssessmentPartitionItem? ParsePartitionItem(string? json) => PartitionJsonHelper.ParseItem(json);
    private readonly IPropertyAssessmentRequestService _rpaService;
    private readonly IAssessmentService _assessmentService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly IOwnerService _ownerService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ITaxDeclarationPdfService _tdPdfService;

    public PropertyAssessmentRequestController(
        IPropertyAssessmentRequestService rpaService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IOwnerService ownerService,
        ApplicationDbContext context,
        IWebHostEnvironment env,
        ITaxDeclarationPdfService tdPdfService)
    {
        _rpaService = rpaService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _ownerService = ownerService;
        _context = context;
        _env = env;
        _tdPdfService = tdPdfService;
    }

    // GET: /PropertyAssessmentRequest/PrintPdf/5
    [HttpGet]
    public IActionResult PrintPdf(Guid id, bool download = false)
    {
        var req = _rpaService.GetById(id);
        if (req == null) return NotFound();

        var declarations = BuildTaxDeclarationsForPrint(req);
        var pdfBytes = _tdPdfService.GenerateRequestTdCertificatePdf(
            declarations,
            req.TrackingNumber,
            $"[{req.TransactionTypeCode}] {req.TransactionTypeName}");

        var safeTrack = string.IsNullOrWhiteSpace(req.TrackingNumber)
            ? req.Id.ToString("N")[..8]
            : string.Concat(req.TrackingNumber.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"TaxDeclaration_{safeTrack}.pdf";

        if (download)
        {
            return File(pdfBytes, "application/pdf", fileName);
        }

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(pdfBytes, "application/pdf");
    }

    private List<TaxDeclaration> BuildTaxDeclarationsForPrint(PropertyAssessmentRequest request)
    {
        var list = new List<TaxDeclaration>();
        var partitions = ParsePartitionList(request.GeneratedPartitionsJson);
        var refTd = request.RefTaxDeclarationId.HasValue
            ? _assessmentService.GetById(request.RefTaxDeclarationId.Value)
            : (!string.IsNullOrWhiteSpace(request.PrevTDN) ? _assessmentService.GetByTdNumber(request.PrevTDN) : null);

        if (partitions != null && partitions.Count > 0)
        {
            foreach (var p in partitions)
            {
                if (!string.IsNullOrWhiteSpace(p.SystemGeneratedTDN))
                {
                    var existing = _assessmentService.GetByTdNumber(p.SystemGeneratedTDN);
                    if (existing != null)
                    {
                        list.Add(existing);
                        continue;
                    }
                }

                var pRef = p.BasePropertyId.HasValue ? _assessmentService.GetById(p.BasePropertyId.Value) : refTd;
                var effBarangay = !string.IsNullOrWhiteSpace(p.DistrictOrBarangay) ? p.DistrictOrBarangay : (pRef?.DistrictOrBarangay ?? "Brgy. Central");
                list.Add(new TaxDeclaration
                {
                    Id = Guid.NewGuid(),
                    TaxDeclarationNumber = !string.IsNullOrWhiteSpace(p.SystemGeneratedTDN) ? p.SystemGeneratedTDN : (request.PrevTDN ?? request.TrackingNumber),
                    PropertyIndexNumber = !string.IsNullOrWhiteSpace(p.PIN) ? p.PIN : (request.PrevPIN ?? pRef?.PropertyIndexNumber ?? "N/A"),
                    TransactionCode = request.TransactionTypeCode,
                    OwnerName = !string.IsNullOrWhiteSpace(p.NewOwnerName) ? p.NewOwnerName : request.NewOwnerName,
                    OwnerAddress = !string.IsNullOrWhiteSpace(p.OwnerAddress) ? p.OwnerAddress : (request.Address ?? pRef?.OwnerAddress ?? string.Empty),
                    OwnerTin = !string.IsNullOrWhiteSpace(p.OwnerTIN) ? p.OwnerTIN : (request.OwnerTIN ?? pRef?.OwnerTin ?? string.Empty),
                    OwnerPhone = !string.IsNullOrWhiteSpace(p.OwnerContactNo) ? p.OwnerContactNo : (request.ContactNo ?? pRef?.OwnerPhone ?? string.Empty),
                    AdministratorName = p.AdministratorName ?? pRef?.AdministratorName,
                    AdministratorAddress = p.AdministratorAddress ?? pRef?.AdministratorAddress,
                    AdministratorTin = p.AdministratorTin ?? pRef?.AdministratorTin,
                    AdministratorContactNo = p.AdministratorContactNo ?? pRef?.AdministratorContactNo,
                    DistrictOrBarangay = effBarangay,
                    StreetNo = !string.IsNullOrWhiteSpace(p.StreetLocation) ? p.StreetLocation : (request.PropertyLocation ?? pRef?.StreetNo ?? string.Empty),
                    Municipality = !string.IsNullOrWhiteSpace(p.CityMunicipality) ? p.CityMunicipality : (pRef?.Municipality ?? "General Santos City"),
                    ProvinceCity = !string.IsNullOrWhiteSpace(p.Province) ? p.Province : (pRef?.ProvinceCity ?? "South Cotabato"),
                    Kind = Enum.TryParse<PropertyKind>(p.Kind, true, out var pk) ? pk : (pRef?.Kind ?? PropertyKind.Land),
                    Classification = Enum.TryParse<PropertyClassification>(p.Classification, true, out var pc) ? pc : (pRef?.Classification ?? PropertyClassification.Residential),
                    ActualUse = pRef?.ActualUse ?? p.Classification,
                    AreaSqMeters = p.AreaSqMeters > 0 ? p.AreaSqMeters : (pRef?.AreaSqMeters ?? 0m),
                    UnitValue = p.UnitValue > 0 ? p.UnitValue : (pRef?.UnitValue ?? 0m),
                    MarketValue = p.MarketValue > 0 ? p.MarketValue : (pRef?.MarketValue ?? 0m),
                    AdjustedMarketValue = p.MarketValue > 0 ? p.MarketValue : (pRef?.AdjustedMarketValue ?? 0m),
                    AssessmentLevelPercent = p.AssessmentLevelPercent > 0 ? p.AssessmentLevelPercent : (pRef?.AssessmentLevelPercent ?? 20m),
                    Taxability = !string.IsNullOrWhiteSpace(p.Taxability) ? p.Taxability : "Taxable",
                    EffectivityYear = p.EffectivityYear ?? DateTime.Now.Year,
                    EffectivityQuarter = !string.IsNullOrWhiteSpace(p.EffectivityQuarter) ? p.EffectivityQuarter : "1st Quarter",
                    TitleNo = !string.IsNullOrWhiteSpace(p.TitleNo) ? p.TitleNo : pRef?.TitleNo,
                    SurveyNo = !string.IsNullOrWhiteSpace(p.SurveyNumber) ? p.SurveyNumber : (p.CadastralSurveyNo ?? pRef?.SurveyNo),
                    LotNo = !string.IsNullOrWhiteSpace(p.LotNumber) ? p.LotNumber : pRef?.LotNo,
                    BlockNo = !string.IsNullOrWhiteSpace(p.BlockNumber) ? p.BlockNumber : pRef?.BlockNo,
                    BoundaryNorth = !string.IsNullOrWhiteSpace(p.BoundaryNorth) ? p.BoundaryNorth : pRef?.BoundaryNorth,
                    BoundaryEast = !string.IsNullOrWhiteSpace(p.BoundaryEast) ? p.BoundaryEast : pRef?.BoundaryEast,
                    BoundarySouth = !string.IsNullOrWhiteSpace(p.BoundarySouth) ? p.BoundarySouth : pRef?.BoundarySouth,
                    BoundaryWest = !string.IsNullOrWhiteSpace(p.BoundaryWest) ? p.BoundaryWest : pRef?.BoundaryWest,
                    NoOfStoreys = p.NoOfStoreys > 0 ? p.NoOfStoreys : (pRef?.NoOfStoreys ?? 0),
                    TotalBuildingAreaSqm = p.TotalBuildingAreaSqm > 0 ? p.TotalBuildingAreaSqm : p.AreaSqMeters,
                    StructuralType = p.StructuralType ?? pRef?.StructuralType,
                    BuildingClassification = p.BuildingClassification ?? pRef?.BuildingClassification,
                    PreviousTdNo = p.BaseTDN ?? request.PrevTDN ?? pRef?.TaxDeclarationNumber ?? string.Empty,
                    PreviousPin = request.PrevPIN ?? pRef?.PropertyIndexNumber ?? string.Empty,
                    PreviousOwner = request.PrevOwner ?? pRef?.OwnerName ?? string.Empty,
                    PreviousAssessedValue = pRef?.AssessedValue ?? 0m,
                    Memoranda = (!string.IsNullOrWhiteSpace(p.Memoranda) ? p.Memoranda : request.Remarks) ?? string.Empty,
                    AppraisedByName = request.AssignedAppraiser ?? pRef?.AppraisedByName ?? string.Empty,
                    DateAppraised = request.UpdatedDate != default ? request.UpdatedDate : request.RequestDate,
                    DateAssessed = request.UpdatedDate != default ? request.UpdatedDate : request.RequestDate,
                    ApprovedByName = pRef?.ApprovedByName ?? "ATTY.JESSIE P. DELA CRUZ, REA"
                });
            }
        }

        if (list.Count == 0)
        {
            if (refTd != null)
            {
                list.Add(refTd);
            }
            else
            {
                list.Add(new TaxDeclaration
                {
                    Id = request.Id,
                    TaxDeclarationNumber = !string.IsNullOrWhiteSpace(request.PrevTDN) ? request.PrevTDN : request.TrackingNumber,
                    PropertyIndexNumber = !string.IsNullOrWhiteSpace(request.PrevPIN) ? request.PrevPIN : "N/A",
                    TransactionCode = request.TransactionTypeCode,
                    OwnerName = request.NewOwnerName,
                    OwnerAddress = request.Address ?? string.Empty,
                    OwnerTin = request.OwnerTIN ?? string.Empty,
                    OwnerPhone = request.ContactNo ?? string.Empty,
                    DistrictOrBarangay = "Brgy. Central",
                    StreetNo = request.PropertyLocation ?? string.Empty,
                    Municipality = "General Santos City",
                    ProvinceCity = "South Cotabato",
                    Kind = Enum.TryParse<PropertyKind>(request.Kind, true, out var rk) ? rk : PropertyKind.Land,
                    Classification = PropertyClassification.Residential,
                    ActualUse = "Residential",
                    EffectivityYear = DateTime.Now.Year,
                    EffectivityQuarter = "1st Quarter",
                    PreviousTdNo = request.PrevTDN ?? string.Empty,
                    PreviousPin = request.PrevPIN ?? string.Empty,
                    PreviousOwner = request.PrevOwner ?? string.Empty,
                    Memoranda = request.Remarks ?? string.Empty,
                    AppraisedByName = request.AssignedAppraiser ?? string.Empty,
                    DateAppraised = request.RequestDate,
                    DateAssessed = request.UpdatedDate != default ? request.UpdatedDate : request.RequestDate
                });
            }
        }

        return list;
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

        // Validate that O.R. Number is provided if the request is being forwarded beyond Receiving
        if (request.Status > PropertyAssessmentStatus.Receiving && string.IsNullOrWhiteSpace(request.ORNumber))
        {
            ModelState.AddModelError(nameof(request.ORNumber), "Official Receipt (O.R.) Number is required before forwarding this assessment request beyond Receiving.");
        }

        ValidateAssessmentRequest(request);

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

        ValidateAssessmentRequest(request);

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
    public IActionResult EditPartition(Guid? requestId, int index, string? returnUrl, string? kind = null)
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

        // Machinery Appraisal Reference Prerequisites (CAST Standard)
        ViewBag.MachineryActualUses = _context.MachineryActualUses.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.Rank).ToList();
        ViewBag.MachineryAssessmentLevels = _context.MachineryAssessmentLevels.AsNoTracking().Where(l => l.IsActive).OrderByDescending(l => l.RevisionYear).ToList();

        ViewBag.Barangays = _context.Barangays.AsNoTracking().OrderBy(b => b.Name).ToList();
        ViewBag.RegisteredOwners = _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.CorporateName)
            .Take(50)
            .ToList();

        AssessmentPartitionItem partition = new AssessmentPartitionItem();
        if (!string.IsNullOrWhiteSpace(kind))
        {
            partition.Kind = kind.Trim();
        }

        if (requestId.HasValue)
        {
            var req = _rpaService.GetById(requestId.Value);
            var partitionsJson = req?.GeneratedPartitionsJson;
            if (string.IsNullOrWhiteSpace(partitionsJson))
            {
                var rfi = _context.RequestForInspections.AsNoTracking().FirstOrDefault(r => r.Id == requestId.Value);
                partitionsJson = rfi?.GeneratedPartitionsJson;
            }

            if (!string.IsNullOrWhiteSpace(partitionsJson))
            {
                try
                {
                    var list = ParsePartitionList(partitionsJson);
                    if (index >= 0 && index < list.Count)
                    {
                        partition = list[index];
                        if (!string.IsNullOrWhiteSpace(kind))
                        {
                            partition.Kind = kind.Trim();
                        }
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
    public async Task<IActionResult> SavePartition(
        AssessmentPartitionItem item,
        int index,
        Guid? requestId,
        string? returnUrl,
        IFormFile? sketchFile,
        [FromForm] string? partitionJsonPayload = null)
    {
        // 0. Prefer the complete client-side partition payload (includes schedules/boundaries not bound as form fields)
        if (!string.IsNullOrWhiteSpace(partitionJsonPayload))
        {
            try
            {
                var payloadItem = ParsePartitionItem(partitionJsonPayload);
                if (payloadItem != null)
                {
                    // Keep form-bound sketch data if the payload did not carry it
                    if (string.IsNullOrWhiteSpace(payloadItem.BuildingFloorPlanSketchPath))
                        payloadItem.BuildingFloorPlanSketchPath = item.BuildingFloorPlanSketchPath;
                    if (string.IsNullOrWhiteSpace(payloadItem.BuildingFloorPlanSketchBase64))
                        payloadItem.BuildingFloorPlanSketchBase64 = item.BuildingFloorPlanSketchBase64;
                    if (payloadItem.BasePropertyId == null) payloadItem.BasePropertyId = item.BasePropertyId;
                    if (string.IsNullOrWhiteSpace(payloadItem.BaseTDN)) payloadItem.BaseTDN = item.BaseTDN;
                    item = payloadItem;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Partition payload could not be read, saved form fields only: {ex.Message}";
            }
        }

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

        // Ensure CAST Combination #1 (ARPNo) and Combination #2 (SystemGeneratedTDN) are formatted
        var lotOrParcel = !string.IsNullOrWhiteSpace(item.ParcelNo) ? item.ParcelNo : item.LotNumber;
        if (string.IsNullOrWhiteSpace(item.ARPNo) || item.ARPNo == CastTdGenerator.DefaultArpNumber)
        {
            item.ARPNo = CastTdGenerator.FormatArpNumber(item.DistrictOrBarangay, item.SectionNo, lotOrParcel);
        }
        var effYear = item.EffectivityYear ?? DateTime.Now.Year;
        if (string.IsNullOrWhiteSpace(item.SystemGeneratedTDN)
            || item.SystemGeneratedTDN.StartsWith("TD-", StringComparison.OrdinalIgnoreCase)
            || item.SystemGeneratedTDN.StartsWith("TDN-", StringComparison.OrdinalIgnoreCase))
        {
            item.SystemGeneratedTDN = _assessmentService.GenerateCastTdNumber(effYear, item.DistrictOrBarangay);
        }
        else if (CastTdGenerator.IsOfficialCastTdFormat(item.SystemGeneratedTDN))
        {
            var parts = item.SystemGeneratedTDN.Split('-');
            if (int.TryParse(parts[2], out int seq))
            {
                item.SystemGeneratedTDN = CastTdGenerator.FormatOfficialTdNumber(effYear, item.DistrictOrBarangay, seq);
            }
        }

        // 3. Update in Database if requestId exists (supports both PropertyAssessmentRequest and RequestForInspection)
        if (requestId.HasValue)
        {
            var req = _rpaService.GetById(requestId.Value);
            if (req != null)
            {
                try
                {
                    var list = ParsePartitionList(req.GeneratedPartitionsJson);

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
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Failed to save partition: {ex.Message}";
                }
            }
            else
            {
                var rfi = _context.RequestForInspections.FirstOrDefault(r => r.Id == requestId.Value);
                if (rfi != null)
                {
                    try
                    {
                        var list = ParsePartitionList(rfi.GeneratedPartitionsJson);

                        if (index >= 0 && index < list.Count)
                        {
                            list[index] = item;
                        }
                        else
                        {
                            list.Add(item);
                        }
                        rfi.GeneratedPartitionsJson = System.Text.Json.JsonSerializer.Serialize(list, _jsonOptions);
                        rfi.Partitions = list.Count > 0 ? list.Count : 1;
                        rfi.UpdatedDate = DateTime.Now;
                        _context.SaveChanges();
                        TempData["SuccessMessage"] = $"Partition {item.SystemGeneratedTDN} ({item.PartitionIndex}) appraisal updated successfully!";
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Failed to save partition: {ex.Message}";
                    }
                }
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
            id = owner.Id.ToString(),
            name = owner.DisplayName,
            ownerType = owner.OwnerType.ToString(),
            tin = owner.TaxIdentNo,
            rptin = owner.RptinNumber,
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
                ownerType = o.OwnerType.ToString(),
                tin = o.TaxIdentNo,
                rptin = o.RptinNumber,
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

    private const int MaxLotNoLength = 100;
    private const int MaxLocationLength = 250;
    private const int MaxOwnerNameLength = 150;
    private const int MaxAddressLength = 250;
    private const int MaxOwnerTinLength = 30;
    private const int MaxContactNumberLength = 20;
    private const int MaxEmailLength = 100;
    private const int MaxRequestorLength = 150;
    private const int MaxFeeQuantity = 999;

    private static readonly System.Text.RegularExpressions.Regex CodePattern = new(@"^[A-Za-z0-9\-/\.\s]+$");
    private static readonly System.Text.RegularExpressions.Regex PersonNamePattern = new(@"^[\p{L}\s\.,'\-]+$");
    private static readonly System.Text.RegularExpressions.Regex PhonePattern = new(@"^(\+63|0)?\d{7,10}$");
    private static readonly System.Text.RegularExpressions.Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$");

    private static string NormalizeTdnForComparison(string? tdn)
    {
        if (string.IsNullOrWhiteSpace(tdn)) return string.Empty;
        var s = System.Text.RegularExpressions.Regex.Replace(tdn.Trim().ToUpperInvariant(), @"[\s\-_]+", "-");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"-(LAND|BLDG|BUILDING|MACH|MACHINERY|IMP|IMPROVEMENT)-", "-", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"-(LAND|BLDG|BUILDING|MACH|MACHINERY|IMP|IMPROVEMENT)$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"^(LAND|BLDG|BUILDING|MACH|MACHINERY|IMP|IMPROVEMENT)-", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return s;
    }

    private static string ExtractTdnSequence(string? tdn)
    {
        if (string.IsNullOrWhiteSpace(tdn)) return string.Empty;
        var matches = System.Text.RegularExpressions.Regex.Matches(tdn, @"(\d{3,})");
        return matches.Count > 0 ? matches[^1].Value : string.Empty;
    }

    private static string NormalizePinForComparison(string? pin)
    {
        if (string.IsNullOrWhiteSpace(pin)) return string.Empty;
        var clean = System.Text.RegularExpressions.Regex.Replace(pin.Trim().ToUpperInvariant(), @"[^A-Z0-9]", "");
        if (clean == "NA" || clean == "NONE" || System.Text.RegularExpressions.Regex.IsMatch(clean, @"^0+$")) return string.Empty;
        return clean;
    }

    private static string NormalizeOwnerForComparison(string? owner)
    {
        if (string.IsNullOrWhiteSpace(owner)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(owner.Trim().ToUpperInvariant(), @"[\s.,'-]+", "");
    }

    private static bool IsSameReferenceTaxDeclaration(string? tdn1, string? pin1, string? owner1,
                                                      string? tdn2, string? pin2, string? owner2)
    {
        var rawTdn1 = tdn1?.Trim().ToUpperInvariant() ?? string.Empty;
        var rawTdn2 = tdn2?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawTdn1) && rawTdn1 == rawTdn2) return true;

        var norm1 = NormalizeTdnForComparison(tdn1);
        var norm2 = NormalizeTdnForComparison(tdn2);
        if (!string.IsNullOrEmpty(norm1) && norm1 == norm2) return true;

        var p1 = NormalizePinForComparison(pin1);
        var p2 = NormalizePinForComparison(pin2);
        if (!string.IsNullOrEmpty(p1) && p1.Length >= 6 && p1 == p2) return true;

        var seq1 = ExtractTdnSequence(tdn1);
        var seq2 = ExtractTdnSequence(tdn2);
        var o1 = NormalizeOwnerForComparison(owner1);
        var o2 = NormalizeOwnerForComparison(owner2);
        if (!string.IsNullOrEmpty(seq1) && seq1 == seq2 && !string.IsNullOrEmpty(o1) && o1 == o2) return true;

        return false;
    }

    private sealed class RequestedFeeItem
    {
        public string? ParticularCode { get; set; }
        public string? ParticularName { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalCost { get; set; }
    }

    private void ValidateAssessmentRequest(PropertyAssessmentRequest request)
    {
        void CheckText(string key, string? value, string label, int maxLen,
            bool required = true, System.Text.RegularExpressions.Regex? pattern = null, string? patternMessage = null)
        {
            var val = value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(val))
            {
                if (required && (ModelState[key]?.Errors.Count ?? 0) == 0)
                    ModelState.AddModelError(key, $"{label} is required.");
                return;
            }
            if (required && string.Equals(val.Replace("/", ""), "NA", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(key, $"{label} is required (N/A is not accepted).");
                return;
            }
            if (val.Length > maxLen)
            {
                ModelState.AddModelError(key, $"{label} must not exceed {maxLen} characters.");
                return;
            }
            if (pattern != null && !pattern.IsMatch(val))
            {
                ModelState.AddModelError(key, patternMessage ?? $"{label} contains invalid characters.");
            }
        }

        // 1. Reference Tax Declaration Validation
        var selectedIds = new List<Guid>();
        if (!string.IsNullOrWhiteSpace(request.SelectedRefPropertyIdsJson))
        {
            try
            {
                selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(request.SelectedRefPropertyIdsJson) ?? new();
            }
            catch
            {
                ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "Selected Reference Tax Declarations are invalid.");
            }
        }
        if (request.RefTaxDeclarationId.HasValue && !selectedIds.Contains(request.RefTaxDeclarationId.Value))
        {
            selectedIds.Add(request.RefTaxDeclarationId.Value);
        }

        var isDiscovery = string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase);

        if (selectedIds.Count == 0)
        {
            if (!isDiscovery)
            {
                ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "Please search and select at least one Reference Tax Declaration (TD).");
                ModelState.AddModelError(nameof(request.RefTaxDeclarationId), "At least one Reference Property must be selected.");
            }
        }
        else
        {
            if (selectedIds.Distinct().Count() != selectedIds.Count)
            {
                ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "Duplicate Reference TD selected. Each TD can only be selected once.");
            }
            else
            {
                var refProperties = _context.TaxDeclarations
                    .AsNoTracking()
                    .Where(t => selectedIds.Contains(t.Id))
                    .ToList();

                if (refProperties.Count != selectedIds.Count)
                {
                    ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "One or more selected Reference Tax Declarations no longer exist.");
                }
                else
                {
                    for (int i = 0; i < refProperties.Count; i++)
                    {
                        for (int j = i + 1; j < refProperties.Count; j++)
                        {
                            var a = refProperties[i];
                            var b = refProperties[j];
                            if (IsSameReferenceTaxDeclaration(a.TaxDeclarationNumber, a.PropertyIndexNumber, a.OwnerName,
                                                              b.TaxDeclarationNumber, b.PropertyIndexNumber, b.OwnerName))
                            {
                                ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson),
                                    $"Reference TD '{a.TaxDeclarationNumber}' and '{b.TaxDeclarationNumber}' refer to the same property. Please select distinct Tax Declarations.");
                                break;
                            }
                        }
                    }
                }
            }
        }

        // 2. Subdivision (SD), Consolidation (CS), Physical Change (PC), Transfer (TR), and Reclassification (RC) Validation
        var isSubdivision = string.Equals(request.TransactionTypeCode, "SD", StringComparison.OrdinalIgnoreCase);
        var isConsolidation = string.Equals(request.TransactionTypeCode, "CS", StringComparison.OrdinalIgnoreCase);
        var isPhysicalChange = string.Equals(request.TransactionTypeCode, "PC", StringComparison.OrdinalIgnoreCase);
        var isTransfer = string.Equals(request.TransactionTypeCode, "TR", StringComparison.OrdinalIgnoreCase);
        var isReclassification = string.Equals(request.TransactionTypeCode, "RC", StringComparison.OrdinalIgnoreCase);
        if (isSubdivision)
        {
            var partitionCount = request.Partitions;
            if (!string.IsNullOrWhiteSpace(request.GeneratedPartitionsJson))
            {
                try
                {
                    var parts = ParsePartitionList(request.GeneratedPartitionsJson);
                    if (parts != null && parts.Count > 0) partitionCount = parts.Count;
                }
                catch { }
            }
            if (partitionCount < 2)
            {
                ModelState.AddModelError(nameof(request.Partitions), "Subdivision (SD) · Segregation of Land requires at least 2 segregated lot partitions.");
            }
        }
        if (isConsolidation)
        {
            // Consolidation combines two or more contiguous parcels under identical ownership into a single parcel.
            if (selectedIds.Count < 2)
            {
                ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "Consolidation (CS) · Lot Integration requires selecting at least 2 contiguous parent parcels to combine into a single parcel.");
            }
            else
            {
                var distinctIds = selectedIds.Distinct().ToList();
                var parentTds = _context.TaxDeclarations.AsNoTracking()
                    .Where(t => distinctIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.OwnerName, t.OwnerTin, t.OwnerId })
                    .ToList();

                if (parentTds.Count >= 2)
                {
                    var firstOwner = parentTds[0].OwnerName?.Trim();
                    var firstTin = parentTds[0].OwnerTin?.Trim();
                    var hasDifferentOwner = parentTds.Any(p =>
                        !string.Equals(p.OwnerName?.Trim(), firstOwner, StringComparison.OrdinalIgnoreCase) &&
                        (string.IsNullOrEmpty(firstTin) || !string.Equals(p.OwnerTin?.Trim(), firstTin, StringComparison.OrdinalIgnoreCase)));

                    if (hasDifferentOwner)
                    {
                        ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "Consolidation (CS) requires identical ownership. The selected contiguous parcels must belong to the same registered owner.");
                    }
                }
            }
        }
        if (isPhysicalChange)
        {
            // Physical change reassessment applies to exactly 1 property undergoing renovation/expansion
            if (selectedIds.Count != 1)
            {
                ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "Reassessment Due to Physical Change (PC) · Renovation / Expansion requires exactly 1 reference property undergoing structural renovation, floor addition, or improvement.");
            }
        }
        if (isTransfer)
        {
            // Transfer applies to exactly 1 reference property being conveyed
            if (selectedIds.Count != 1)
            {
                ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "Transfer (TR) · Administrative Title Conveyance requires selecting exactly 1 reference property to transfer to the new owner.");
            }
        }
        if (isReclassification)
        {
            // Reclassification applies to exactly 1 reference property converting physical ground use
            if (selectedIds.Count != 1)
            {
                ModelState.AddModelError(nameof(request.SelectedRefPropertyIdsJson), "Reclassification (RC) · Actual Ground Use Conversion requires selecting exactly 1 reference property converting physical ground use.");
            }
        }

        // Back Taxes Validation
        if (!string.IsNullOrWhiteSpace(request.GeneratedPartitionsJson))
        {
            try
            {
                var parts = ParsePartitionList(request.GeneratedPartitionsJson);
                if (parts != null)
                {
                    foreach (var p in parts)
                    {
                        if (p.BackTaxesYearFrom.HasValue)
                        {
                            if (p.BackTaxesYearTo.HasValue && p.BackTaxesYearTo.Value < p.BackTaxesYearFrom.Value)
                            {
                                ModelState.AddModelError(nameof(request.GeneratedPartitionsJson),
                                    "Back Taxes 'To Year' cannot be earlier than 'From Year'.");
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // 3. New Owner Demographics
        CheckText(nameof(request.NewOwnerName), request.NewOwnerName, "New Owner / Buyer Name", MaxOwnerNameLength, required: true, pattern: PersonNamePattern, patternMessage: "New Owner / Buyer Name contains invalid characters.");
        CheckText(nameof(request.Address), request.Address, "Owner Address", MaxAddressLength, required: true);
        CheckText(nameof(request.PropertyLocation), request.PropertyLocation, "Property Location", MaxLocationLength, required: true);

        if (!string.IsNullOrWhiteSpace(request.OwnerTIN))
            CheckText(nameof(request.OwnerTIN), request.OwnerTIN, "Owner TIN", MaxOwnerTinLength, required: false);

        if (!string.IsNullOrWhiteSpace(request.ContactNo))
            CheckText(nameof(request.ContactNo), request.ContactNo, "Contact / Mobile No.", MaxContactNumberLength, required: false, pattern: PhonePattern, patternMessage: "Contact number must be 7-11 digits (e.g., 09171234567 or +639171234567).");

        if (!string.IsNullOrWhiteSpace(request.EmailAddress))
            CheckText(nameof(request.EmailAddress), request.EmailAddress, "Email Address", MaxEmailLength, required: false, pattern: EmailPattern, patternMessage: "Please enter a valid email address.");

        if (!string.IsNullOrWhiteSpace(request.LotNo))
            CheckText(nameof(request.LotNo), request.LotNo, "Lot / Survey No.", MaxLotNoLength, required: false);

        if (!string.IsNullOrWhiteSpace(request.Requestor))
            CheckText(nameof(request.Requestor), request.Requestor, "Requestor / Representative", MaxRequestorLength, required: false, pattern: PersonNamePattern, patternMessage: "Requestor name contains invalid characters.");

        // 4. Order of Payment & Fee Computation Validation
        if (string.IsNullOrWhiteSpace(request.RequestedDocumentsJson) || request.RequestedDocumentsJson.Trim() == "[]")
        {
            ModelState.AddModelError(nameof(request.RequestedDocumentsJson), "At least one Order of Payment fee particular is required.");
        }
        else
        {
            List<RequestedFeeItem>? feeItems = null;
            try
            {
                feeItems = System.Text.Json.JsonSerializer.Deserialize<List<RequestedFeeItem>>(request.RequestedDocumentsJson, _jsonOptions);
            }
            catch
            {
                ModelState.AddModelError(nameof(request.RequestedDocumentsJson), "Fee computation data is invalid.");
            }

            if (feeItems == null || feeItems.Count == 0)
            {
                ModelState.AddModelError(nameof(request.RequestedDocumentsJson), "At least one Order of Payment fee particular is required.");
            }
            else
            {
                var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < feeItems.Count; i++)
                {
                    var item = feeItems[i];
                    var particularKey = !string.IsNullOrWhiteSpace(item.ParticularCode) ? item.ParticularCode.Trim() : item.ParticularName?.Trim();
                    if (string.IsNullOrWhiteSpace(particularKey))
                    {
                        ModelState.AddModelError(nameof(request.RequestedDocumentsJson), $"Fee row {i + 1}: Particular is required.");
                        continue;
                    }
                    if (!seenCodes.Add(particularKey))
                    {
                        ModelState.AddModelError(nameof(request.RequestedDocumentsJson), $"Duplicate fee particular '{particularKey}' found. Each fee particular may only be added once.");
                    }
                    if (item.Quantity <= 0 || item.Quantity > MaxFeeQuantity)
                    {
                        ModelState.AddModelError(nameof(request.RequestedDocumentsJson), $"Fee row {i + 1} ({particularKey}): Quantity must be between 1 and {MaxFeeQuantity}.");
                    }
                    if (item.UnitCost < 0)
                    {
                        ModelState.AddModelError(nameof(request.RequestedDocumentsJson), $"Fee row {i + 1} ({particularKey}): Rate/Unit Cost cannot be negative.");
                    }
                }

                if (request.TotalFeesAmount <= 0)
                {
                    ModelState.AddModelError(nameof(request.TotalFeesAmount), "Total fees amount must be greater than zero.");
                }
            }
        }
    }
}
