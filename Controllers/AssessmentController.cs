using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class AssessmentController : Controller
{
    private readonly IAssessmentService _assessmentService;
    private readonly IOwnerService _ownerService;
    private readonly ApplicationDbContext _context;
    private readonly ITaxDeclarationPdfService _tdPdfService;

    public AssessmentController(
        IAssessmentService assessmentService,
        IOwnerService ownerService,
        ApplicationDbContext context,
        ITaxDeclarationPdfService tdPdfService)
    {
        _assessmentService = assessmentService;
        _ownerService = ownerService;
        _context = context;
        _tdPdfService = tdPdfService;
    }

    // GET: /Assessment/PrintPdf/5
    [HttpGet]
    public IActionResult PrintPdf(Guid id, bool download = false)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }

        var pdfBytes = _tdPdfService.GenerateCompleteRecordPdf(record);
        var safeTdNo = string.IsNullOrWhiteSpace(record.TaxDeclarationNumber)
            ? record.Id.ToString("N")[..8]
            : string.Concat(record.TaxDeclarationNumber.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"FAAS_Record_{safeTdNo}.pdf";

        if (download)
        {
            return File(pdfBytes, "application/pdf", fileName);
        }

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(pdfBytes, "application/pdf");
    }

    // GET: /Assessment
    public IActionResult Index(string? search, PropertyClassification? classification, AssessmentStatus? status)
    {
        ViewBag.Search = search;
        ViewBag.Classification = classification;
        ViewBag.Status = status;

        ViewBag.TotalMarketValue = _assessmentService.GetTotalMarketValue();
        ViewBag.TotalAssessedValue = _assessmentService.GetTotalAssessedValue();
        ViewBag.TotalRevenue = _assessmentService.GetTotalEstimatedTaxRevenue();
        ViewBag.TotalProperties = _assessmentService.GetTotalActivePropertiesCount();

        var records = _assessmentService.GetAll(search, classification, status);
        return View(records);
    }

    // GET: /Assessment/Details/5
    public IActionResult Details(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }
        return View(record);
    }

    // GET: /Assessment/ViewRecord/5
    public IActionResult ViewRecord(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }
        return View(record);
    }

    // GET: /Assessment/Lineage/5  (Mother & Child Tax Declaration Lineage)
    public IActionResult Lineage(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }

        var allTds = _assessmentService.GetAll().ToList();
        var currentTdNo = (record.TaxDeclarationNumber ?? string.Empty).Trim();
        var currentPin = (record.PropertyIndexNumber ?? string.Empty).Trim();

        static List<string> SplitTokens(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? new List<string>()
                : raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim())
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .ToList();

        var prevTdTokens = SplitTokens(record.PreviousTdNo);
        var prevPinTokens = SplitTokens(record.PreviousPin);
        var landTdToken = record.LandTdNo?.Trim();
        var bldgTdToken = record.BuildingTdNo?.Trim();

        var mothers = allTds
            .Where(t => t.Id != record.Id && (
                prevTdTokens.Any(tok => string.Equals(t.TaxDeclarationNumber?.Trim(), tok, StringComparison.OrdinalIgnoreCase)) ||
                prevPinTokens.Any(tok => string.Equals(t.PropertyIndexNumber?.Trim(), tok, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentTdNo) && SplitTokens(t.SupersededByTdns).Any(tok => string.Equals(tok, currentTdNo, StringComparison.OrdinalIgnoreCase))) ||
                (prevTdTokens.Count == 0 && !string.IsNullOrEmpty(landTdToken) && string.Equals(t.TaxDeclarationNumber?.Trim(), landTdToken, StringComparison.OrdinalIgnoreCase)) ||
                (prevTdTokens.Count == 0 && !string.IsNullOrEmpty(bldgTdToken) && string.Equals(t.TaxDeclarationNumber?.Trim(), bldgTdToken, StringComparison.OrdinalIgnoreCase))
            ))
            .OrderByDescending(t => t.DateAssessed)
            .ToList();

        var supersededTokens = SplitTokens(record.SupersededByTdns);

        var children = allTds
            .Where(t => t.Id != record.Id && (
                supersededTokens.Any(tok => string.Equals(t.TaxDeclarationNumber?.Trim(), tok, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentTdNo) && SplitTokens(t.PreviousTdNo).Any(tok => string.Equals(tok, currentTdNo, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(currentPin) && SplitTokens(t.PreviousPin).Any(tok => string.Equals(tok, currentPin, StringComparison.OrdinalIgnoreCase)))
            ))
            .OrderBy(t => t.TaxDeclarationNumber)
            .ToList();

        var relatedImprovements = allTds
            .Where(t => t.Id != record.Id && !mothers.Any(m => m.Id == t.Id) && !children.Any(c => c.Id == t.Id) && (
                (!string.IsNullOrEmpty(currentTdNo) && string.Equals(t.LandTdNo?.Trim(), currentTdNo, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentPin) && string.Equals(t.LandPin?.Trim(), currentPin, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentTdNo) && string.Equals(t.BuildingTdNo?.Trim(), currentTdNo, StringComparison.OrdinalIgnoreCase))
            ))
            .OrderBy(t => t.Kind)
            .ThenBy(t => t.TaxDeclarationNumber)
            .ToList();

        PropertyAssessmentRequest? cancellingReq = null;
        if (record.CancelledByRequestId.HasValue)
        {
            cancellingReq = _context.PropertyAssessmentRequests
                .FirstOrDefault(r => r.Id == record.CancelledByRequestId.Value);
        }

        var vm = new TdLineageViewModel
        {
            Selected = record,
            MotherRecords = mothers,
            ChildRecords = children,
            RelatedImprovements = relatedImprovements,
            CancellingRequest = cancellingReq
        };

        return View(vm);
    }
    // GET: /Assessment/Create
    public IActionResult Create()
    {
        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();
        var model = new TaxDeclaration
        {
            TaxDeclarationNumber = _assessmentService.GenerateCastTdNumber(DateTime.Now.Year, "0001"),
            ARPNo = CastTdGenerator.DefaultArpNumber,
            TransactionCode = "DC",
            PropertyIndexNumber = $"187-00-0001-001-{new Random().Next(1, 999):D3}",
            EffectivityYear = DateTime.Now.Year,
            AssessmentLevelPercent = 20m
        };
        return View(model);
    }

    // POST: /Assessment/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TaxDeclaration model)
    {
        if (ModelState.IsValid)
        {
            if (model.MarketValue <= 0 && model.AreaSqMeters > 0 && model.UnitValue > 0)
            {
                model.MarketValue = model.AreaSqMeters * model.UnitValue;
            }

            if (model.AssessmentLevelPercent <= 0)
            {
                model.AssessmentLevelPercent = _assessmentService.GetDefaultAssessmentLevel(model.Classification);
            }

            _assessmentService.Add(model);
            TempData["SuccessMessage"] = $"Tax Declaration {model.TaxDeclarationNumber} registered successfully!";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();
        return View(model);
    }

    // GET: /Assessment/Edit/5
    public IActionResult Edit(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }
        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();
        return View(record);
    }

    // POST: /Assessment/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, TaxDeclaration model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            _assessmentService.Update(model);
            TempData["SuccessMessage"] = $"Tax Declaration {model.TaxDeclarationNumber} updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();
        return View(model);
    }

    [HttpGet]
    public IActionResult GetLandFormData()
    {
        return Json(new
        {
            classifications = _context.LandClassifications.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToList(),
            subClassifications = _context.LandSubClassifications.Where(s => s.IsActive).ToList(),
            actualUses = _context.LandActualUses.Where(a => a.IsActive).ToList(),
            assessmentLevels = _context.LandAssessmentLevels.Where(l => l.IsActive).ToList(),
            adjustments = _context.LandAdjustmentFactors.Where(j => j.IsActive).ToList(),
            improvementTypes = _context.LandImprovementTypes.Where(i => i.IsActive).OrderBy(i => i.TypeDescription).ToList()
        });
    }

    private void PopulateLandPrerequisites()
    {
        ViewBag.LandClassifications = _context.LandClassifications.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToList();
        ViewBag.LandSubClassifications = _context.LandSubClassifications.Where(s => s.IsActive).ToList();
        ViewBag.LandActualUses = _context.LandActualUses.Where(a => a.IsActive).ToList();
        ViewBag.LandAssessmentLevels = _context.LandAssessmentLevels.Where(l => l.IsActive).ToList();
        ViewBag.LandAdjustments = _context.LandAdjustmentFactors.Where(j => j.IsActive).ToList();
        ViewBag.LandImprovementTypes = _context.LandImprovementTypes.Where(i => i.IsActive).OrderBy(i => i.TypeDescription).ToList();
        ViewBag.Barangays = _context.Barangays.OrderBy(b => b.Name).ToList();
    }

    // GET: /Assessment/Calculator
    public IActionResult Calculator()
    {
        var model = new AssessmentCalculatorViewModel();
        return View(model);
    }

    // POST: /Assessment/Calculator
    [HttpPost]
    public IActionResult Calculator(AssessmentCalculatorViewModel model)
    {
        return View(model);
    }

    // GET: /Assessment/GetOwnerDetails
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
            id = owner.Id,
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

    // GET: /Assessment/GetDefaultAssessmentLevel
    [HttpGet]
    public JsonResult GetDefaultAssessmentLevel(PropertyClassification classification)
    {
        var level = _assessmentService.GetDefaultAssessmentLevel(classification);
        return Json(new { assessmentLevel = level });
    }

    // POST: /Assessment/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record != null)
        {
            _assessmentService.Delete(id);
            TempData["SuccessMessage"] = $"Tax Declaration {record.TaxDeclarationNumber} was deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
