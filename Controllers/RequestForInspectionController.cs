using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class RequestForInspectionController : Controller
{
    private readonly IRequestForInspectionService _rfiService;
    private readonly IAssessmentService _assessmentService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly IOwnerService _ownerService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ITaxDeclarationPdfService _tdPdfService;

    public RequestForInspectionController(
        IRequestForInspectionService rfiService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IOwnerService ownerService,
        ApplicationDbContext context,
        IWebHostEnvironment env,
        ITaxDeclarationPdfService tdPdfService)
    {
        _rfiService = rfiService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _ownerService = ownerService;
        _context = context;
        _env = env;
        _tdPdfService = tdPdfService;
    }

    // GET: /RequestForInspection/PrintPdf/5
    [HttpGet]
    public IActionResult PrintPdf(Guid id, bool download = false)
    {
        var rfi = _rfiService.GetById(id);
        if (rfi == null) return NotFound();

        var declarations = BuildTaxDeclarationsForPrint(rfi);
        var pdfBytes = _tdPdfService.GenerateRequestTdCertificatePdf(
            declarations,
            rfi.TrackingNumber,
            $"[{rfi.TransactionCode}] {rfi.TransactionTypeName}");

        var safeTrack = string.IsNullOrWhiteSpace(rfi.TrackingNumber)
            ? rfi.Id.ToString("N")[..8]
            : string.Concat(rfi.TrackingNumber.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"TaxDeclaration_{safeTrack}.pdf";

        if (download)
        {
            return File(pdfBytes, "application/pdf", fileName);
        }

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(pdfBytes, "application/pdf");
    }

    private List<TaxDeclaration> BuildTaxDeclarationsForPrint(RequestForInspection request)
    {
        var list = new List<TaxDeclaration>();
        var partitions = PartitionJsonHelper.ParseList(request.GeneratedPartitionsJson);
        var refTd = request.TaxDeclarationId.HasValue
            ? _assessmentService.GetById(request.TaxDeclarationId.Value)
            : (!string.IsNullOrWhiteSpace(request.TaxDeclarationNumber) ? _assessmentService.GetByTdNumber(request.TaxDeclarationNumber) : null);

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
                    TaxDeclarationNumber = !string.IsNullOrWhiteSpace(p.SystemGeneratedTDN) ? p.SystemGeneratedTDN : (request.TaxDeclarationNumber ?? request.TrackingNumber),
                    PropertyIndexNumber = !string.IsNullOrWhiteSpace(p.PIN) ? p.PIN : (request.PropertyIndexNumber ?? pRef?.PropertyIndexNumber ?? "N/A"),
                    TransactionCode = request.TransactionCode ?? "INSP",
                    OwnerName = !string.IsNullOrWhiteSpace(p.NewOwnerName) ? p.NewOwnerName : request.OwnerName,
                    OwnerAddress = !string.IsNullOrWhiteSpace(p.OwnerAddress) ? p.OwnerAddress : (request.PropertyLocation ?? pRef?.OwnerAddress ?? string.Empty),
                    OwnerTin = !string.IsNullOrWhiteSpace(p.OwnerTIN) ? p.OwnerTIN : (pRef?.OwnerTin ?? string.Empty),
                    OwnerPhone = !string.IsNullOrWhiteSpace(p.OwnerContactNo) ? p.OwnerContactNo : (request.ContactNumber ?? pRef?.OwnerPhone ?? string.Empty),
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
                    LotNo = !string.IsNullOrWhiteSpace(p.LotNumber) ? p.LotNumber : (request.LotNo ?? pRef?.LotNo),
                    BlockNo = !string.IsNullOrWhiteSpace(p.BlockNumber) ? p.BlockNumber : pRef?.BlockNo,
                    BoundaryNorth = !string.IsNullOrWhiteSpace(p.BoundaryNorth) ? p.BoundaryNorth : pRef?.BoundaryNorth,
                    BoundaryEast = !string.IsNullOrWhiteSpace(p.BoundaryEast) ? p.BoundaryEast : pRef?.BoundaryEast,
                    BoundarySouth = !string.IsNullOrWhiteSpace(p.BoundarySouth) ? p.BoundarySouth : pRef?.BoundarySouth,
                    BoundaryWest = !string.IsNullOrWhiteSpace(p.BoundaryWest) ? p.BoundaryWest : pRef?.BoundaryWest,
                    NoOfStoreys = p.NoOfStoreys > 0 ? p.NoOfStoreys : (pRef?.NoOfStoreys ?? 0),
                    TotalBuildingAreaSqm = p.TotalBuildingAreaSqm > 0 ? p.TotalBuildingAreaSqm : p.AreaSqMeters,
                    StructuralType = p.StructuralType ?? pRef?.StructuralType,
                    BuildingClassification = p.BuildingClassification ?? pRef?.BuildingClassification,
                    PreviousTdNo = p.BaseTDN ?? request.TaxDeclarationNumber ?? pRef?.TaxDeclarationNumber ?? string.Empty,
                    PreviousPin = request.PropertyIndexNumber ?? pRef?.PropertyIndexNumber ?? string.Empty,
                    PreviousOwner = pRef?.OwnerName ?? request.OwnerName ?? string.Empty,
                    PreviousAssessedValue = pRef?.AssessedValue ?? 0m,
                    Memoranda = (!string.IsNullOrWhiteSpace(p.Memoranda) ? p.Memoranda : request.Remarks) ?? string.Empty,
                    AppraisedByName = pRef?.AppraisedByName ?? request.Representative ?? string.Empty,
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
                    TaxDeclarationNumber = !string.IsNullOrWhiteSpace(request.TaxDeclarationNumber) ? request.TaxDeclarationNumber : request.TrackingNumber,
                    PropertyIndexNumber = !string.IsNullOrWhiteSpace(request.PropertyIndexNumber) ? request.PropertyIndexNumber : "N/A",
                    TransactionCode = request.TransactionCode ?? "INSP",
                    OwnerName = request.OwnerName,
                    OwnerAddress = request.PropertyLocation ?? string.Empty,
                    OwnerPhone = request.ContactNumber ?? string.Empty,
                    DistrictOrBarangay = "Brgy. Central",
                    StreetNo = request.PropertyLocation ?? string.Empty,
                    Municipality = "General Santos City",
                    ProvinceCity = "South Cotabato",
                    Kind = Enum.TryParse<PropertyKind>(request.Kind, true, out var rk) ? rk : PropertyKind.Land,
                    Classification = PropertyClassification.Residential,
                    ActualUse = "Residential",
                    LotNo = request.LotNo,
                    EffectivityYear = DateTime.Now.Year,
                    EffectivityQuarter = "1st Quarter",
                    PreviousTdNo = request.TaxDeclarationNumber ?? string.Empty,
                    PreviousPin = request.PropertyIndexNumber ?? string.Empty,
                    PreviousOwner = request.OwnerName,
                    Memoranda = (request.Remarks ?? request.OtherPurposeDetails) ?? string.Empty,
                    DateAppraised = request.RequestDate,
                    DateAssessed = request.UpdatedDate != default ? request.UpdatedDate : request.RequestDate
                });
            }
        }

        return list;
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

        var now = DateTime.Now;
        var trackingNo = $"RFI-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var model = new RequestForInspection
        {
            TrackingNumber = trackingNo,
            TransactionCode = "INSP",
            TransactionNo = $"TX-{now:yyyyMMdd}-{new Random().Next(100, 999)}",
            Kind = "Land",
            LotNo = string.Empty,
            PropertyLocation = string.Empty,
            Partitions = 1,
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
        ValidateInspectionRequest(request);

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

        List<Guid> selectedIds = new();
        if (!string.IsNullOrWhiteSpace(item.SelectedRefPropertyIdsJson))
        {
            try { selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(item.SelectedRefPropertyIdsJson) ?? new(); } catch { }
        }
        if (item.TaxDeclarationId.HasValue && !selectedIds.Contains(item.TaxDeclarationId.Value))
        {
            selectedIds.Add(item.TaxDeclarationId.Value);
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

    // POST: /RequestForInspection/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, RequestForInspection request, IFormFile? fileUpload)
    {
        if (id != request.Id) return BadRequest();

        ValidateInspectionRequest(request);

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

        List<Guid> selectedIds = new();
        if (!string.IsNullOrWhiteSpace(request.SelectedRefPropertyIdsJson))
        {
            try { selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(request.SelectedRefPropertyIdsJson) ?? new(); } catch { }
        }
        if (request.TaxDeclarationId.HasValue && !selectedIds.Contains(request.TaxDeclarationId.Value))
        {
            selectedIds.Add(request.TaxDeclarationId.Value);
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

    // -------------------------------------------------------------
    // Server-side validation for "File Request for Inspection"
    // (mirrors client-side rules in Views/RequestForInspection/Create.cshtml)
    // -------------------------------------------------------------
    private const int MaxTdnLength = 50;
    private const int MaxPinLength = 50;
    private const int MaxOwnerLength = 150;
    private const int MaxLotNoLength = 100;
    private const int MaxLocationLength = 250;
    private const int MaxPurposeDetailsLength = 500;
    private const int MaxContactPersonLength = 150;
    private const int MaxContactNumberLength = 20;
    private const int MaxEmailLength = 100;
    private const int MaxRepresentativeLength = 150;
    private const int MaxFeeQuantity = 999;

    private static readonly string[] AllowedKinds = { "Land", "Building", "Machinery" };
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

    private void ValidateInspectionRequest(RequestForInspection r)
    {
        void CheckText(string key, string? value, string label, int maxLen,
            bool required = true, System.Text.RegularExpressions.Regex? pattern = null, string? patternMessage = null,
            Func<string, string>? normalize = null)
        {
            var val = value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(val))
            {
                // Avoid duplicating the message produced by [Required] data annotations
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
            if (pattern != null && !pattern.IsMatch(normalize != null ? normalize(val) : val))
            {
                ModelState.AddModelError(key, patternMessage ?? $"{label} has an invalid format.");
            }
        }

        // 1. Reference Tax Declarations: at least one, no duplicate TD
        var selectedIds = new List<Guid>();
        if (!string.IsNullOrWhiteSpace(r.SelectedRefPropertyIdsJson))
        {
            try { selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(r.SelectedRefPropertyIdsJson) ?? new(); }
            catch { ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson), "Selected Reference Tax Declarations are invalid."); }
        }
        if (selectedIds.Count == 0 && r.TaxDeclarationId.HasValue) selectedIds.Add(r.TaxDeclarationId.Value);

        var isDiscovery = string.Equals(r.TransactionCode, "DC", StringComparison.OrdinalIgnoreCase);

        if (selectedIds.Count == 0)
        {
            if (!isDiscovery)
            {
                ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson), "Please search and select at least one Reference Tax Declaration (TD).");
            }
        }
        else
        {
            if (selectedIds.Count != selectedIds.Distinct().Count())
            {
                ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson), "Duplicate Reference TD selected. Each TD can only be selected once.");
            }

            var distinctIds = selectedIds.Distinct().ToList();
            var tdns = _context.TaxDeclarations.AsNoTracking()
                .Where(t => distinctIds.Contains(t.Id))
                .Select(t => new { t.Id, t.TaxDeclarationNumber, t.PropertyIndexNumber, t.OwnerName })
                .ToList();

            if (tdns.Count != distinctIds.Count)
            {
                ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson), "One or more selected Reference Tax Declarations no longer exist.");
            }

            for (int i = 0; i < tdns.Count; i++)
            {
                var t1 = tdns[i];
                for (int j = i + 1; j < tdns.Count; j++)
                {
                    var t2 = tdns[j];
                    if (IsSameReferenceTaxDeclaration(t1.TaxDeclarationNumber, t1.PropertyIndexNumber, t1.OwnerName,
                                                      t2.TaxDeclarationNumber, t2.PropertyIndexNumber, t2.OwnerName))
                    {
                        ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson),
                            $"Cannot select same Tax Declaration: '{t1.TaxDeclarationNumber}' and '{t2.TaxDeclarationNumber}' belong to the same TD / property series.");
                        break;
                    }
                }
            }

            // Consolidation (CS) · Lot Integration checks: at least 2 contiguous parcels under identical ownership
            var isConsolidation = string.Equals(r.TransactionCode, "CS", StringComparison.OrdinalIgnoreCase);
            if (isConsolidation)
            {
                if (distinctIds.Count < 2)
                {
                    ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson),
                        "Consolidation (CS) · Lot Integration requires selecting at least 2 contiguous reference parcels to integrate into a single parcel.");
                }
                else
                {
                    var distinctOwners = tdns.Select(t => t.OwnerName?.Trim()).Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();
                    if (distinctOwners.Count > 1)
                    {
                        ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson),
                            "Consolidation (CS) requires identical ownership. All selected contiguous parcels must belong to the same registered owner.");
                    }
                }
            }

            // Physical Change (PC) · Renovation / Expansion checks: exactly 1 reference parcel
            var isPhysicalChange = string.Equals(r.TransactionCode, "PC", StringComparison.OrdinalIgnoreCase);
            if (isPhysicalChange)
            {
                if (distinctIds.Count != 1)
                {
                    ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson),
                        "Reassessment Due to Physical Change (PC) · Renovation / Expansion requires selecting exactly 1 reference property undergoing structural renovation or expansion.");
                }
            }

            // Transfer (TR) · Administrative Title Conveyance checks: exactly 1 reference parcel
            var isTransfer = string.Equals(r.TransactionCode, "TR", StringComparison.OrdinalIgnoreCase);
            if (isTransfer)
            {
                if (distinctIds.Count != 1)
                {
                    ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson),
                        "Transfer (TR) · Administrative Title Conveyance requires selecting exactly 1 reference property.");
                }
            }

            // Reclassification (RC) · Actual Ground Use Conversion checks: exactly 1 reference parcel
            var isReclass = string.Equals(r.TransactionCode, "RC", StringComparison.OrdinalIgnoreCase);
            if (isReclass)
            {
                if (distinctIds.Count != 1)
                {
                    ModelState.AddModelError(nameof(r.SelectedRefPropertyIdsJson),
                        "Reclassification (RC) · Actual Ground Use Conversion requires selecting exactly 1 reference property.");
                }
            }
        }

        // Property identification
        CheckText(nameof(r.TaxDeclarationNumber), r.TaxDeclarationNumber, "TDN / ARP No.", MaxTdnLength,
            pattern: CodePattern, patternMessage: "TDN / ARP No. may only contain letters, numbers, dashes (-), slashes (/) and periods (.).");
        CheckText(nameof(r.PropertyIndexNumber), r.PropertyIndexNumber, "PIN / Property Index No.", MaxPinLength,
            pattern: CodePattern, patternMessage: "PIN may only contain letters, numbers, dashes (-), slashes (/) and periods (.).");
        CheckText(nameof(r.OwnerName), r.OwnerName, "Declared Owner", MaxOwnerLength);

        if (string.IsNullOrWhiteSpace(r.Kind) || !AllowedKinds.Contains(r.Kind.Trim()))
        {
            ModelState.AddModelError(nameof(r.Kind), "Property Kind is required (Land, Building, or Machinery).");
        }

        CheckText(nameof(r.LotNo), r.LotNo, "Lot No. / Survey No.", MaxLotNoLength);
        CheckText(nameof(r.PropertyLocation), r.PropertyLocation, "Location Address", MaxLocationLength);

        // 2. Purpose of Inspection
        var isPhysicalChangeTx = string.Equals(r.TransactionCode, "PC", StringComparison.OrdinalIgnoreCase);
        if (isPhysicalChangeTx)
        {
            r.IsReassessment = true;
            r.IsCancelled = true;
        }

        var isTransferTx = string.Equals(r.TransactionCode, "TR", StringComparison.OrdinalIgnoreCase);
        if (isTransferTx)
        {
            r.IsCancelled = true;
        }

        var isReclassTx = string.Equals(r.TransactionCode, "RC", StringComparison.OrdinalIgnoreCase);
        if (isReclassTx)
        {
            r.IsReassessment = true;
            r.IsCancelled = true;
        }

        if (!r.IsDepreciation && !r.IsReassessment && !r.IsCancelled && !r.IsOther)
        {
            if (isDiscovery)
            {
                r.IsOther = true;
                r.OtherPurposeDetails = "Discovery / New Declaration (DC) · Original Assessment under Sec. 222 RA 7160";
            }
            else
            {
                ModelState.AddModelError("PurposeOfInspection", "Please select at least one Purpose of Inspection.");
            }
        }
        if (r.IsOther && string.IsNullOrWhiteSpace(r.OtherPurposeDetails))
        {
            ModelState.AddModelError(nameof(r.OtherPurposeDetails), "Specific Purpose Details is required when \"Other Reasons\" is selected.");
        }
        else
        {
            CheckText(nameof(r.OtherPurposeDetails), r.OtherPurposeDetails, "Specific Purpose Details", MaxPurposeDetailsLength, required: false);
        }

        // 3. Contact & Representative Details
        CheckText(nameof(r.ContactPerson), r.ContactPerson, "Contact Person", MaxContactPersonLength,
            pattern: PersonNamePattern, patternMessage: "Contact Person may only contain letters, spaces, periods, commas, apostrophes and dashes.");
        CheckText(nameof(r.ContactNumber), r.ContactNumber, "Mobile / Phone No.", MaxContactNumberLength,
            pattern: PhonePattern, patternMessage: "Enter a valid mobile/phone no. (e.g. 0917-123-4567, +639171234567, or 083-552-1234).",
            normalize: v => System.Text.RegularExpressions.Regex.Replace(v, @"[\s\-\(\)]", ""));
        CheckText(nameof(r.EmailAddress), r.EmailAddress, "Email Address", MaxEmailLength,
            pattern: EmailPattern, patternMessage: "Enter a valid email address.");
        CheckText(nameof(r.Representative), r.Representative, "Representative Name", MaxRepresentativeLength,
            pattern: PersonNamePattern, patternMessage: "Representative Name may only contain letters, spaces, periods, commas, apostrophes and dashes.");

        // 5. Order of Payment & Fee Computation: all columns required, no duplicate particulars
        List<RequestedFeeItem> fees = new();
        if (!string.IsNullOrWhiteSpace(r.RequestedDocumentsJson))
        {
            try
            {
                fees = System.Text.Json.JsonSerializer.Deserialize<List<RequestedFeeItem>>(r.RequestedDocumentsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch
            {
                ModelState.AddModelError(nameof(r.RequestedDocumentsJson), "Order of Payment fee items are invalid.");
                return;
            }
        }

        if (fees.Count == 0)
        {
            ModelState.AddModelError(nameof(r.RequestedDocumentsJson), "At least one Order of Payment fee particular is required.");
            return;
        }

        var validCodes = _orderPaymentService.GetAll()
            .Where(op => !string.IsNullOrWhiteSpace(op.Code))
            .Select(op => op.Code!.Trim().ToUpperInvariant())
            .ToHashSet();
        var seenCodes = new HashSet<string>();

        for (int i = 0; i < fees.Count; i++)
        {
            var f = fees[i];
            var rowNo = i + 1;
            var code = f.ParticularCode?.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(code) || string.IsNullOrWhiteSpace(f.ParticularName))
            {
                ModelState.AddModelError(nameof(r.RequestedDocumentsJson), $"Fee row {rowNo}: Particular is required.");
                continue;
            }
            if (!validCodes.Contains(code))
            {
                ModelState.AddModelError(nameof(r.RequestedDocumentsJson), $"Fee row {rowNo}: Particular '{f.ParticularCode}' is not a valid Order of Payment item.");
            }
            if (!seenCodes.Add(code))
            {
                ModelState.AddModelError(nameof(r.RequestedDocumentsJson), $"Fee row {rowNo}: Duplicate particular '{f.ParticularCode} - {f.ParticularName}'.");
            }
            if (f.UnitCost <= 0)
            {
                ModelState.AddModelError(nameof(r.RequestedDocumentsJson), $"Fee row {rowNo}: Fee amount must be greater than 0.00.");
            }
            if (f.Quantity < 1 || f.Quantity > MaxFeeQuantity || f.Quantity != Math.Floor(f.Quantity))
            {
                ModelState.AddModelError(nameof(r.RequestedDocumentsJson), $"Fee row {rowNo}: Quantity must be a whole number from 1 to {MaxFeeQuantity}.");
            }
        }

        var computedTotal = fees.Sum(f => f.UnitCost * f.Quantity);
        if (computedTotal <= 0)
        {
            ModelState.AddModelError(nameof(r.TotalFeesAmount), "Total inspection fees must be greater than 0.00.");
        }
        else
        {
            // Trust the server-computed total over the posted hidden value
            r.TotalFeesAmount = Math.Round(computedTotal, 2);
        }
    }
}
