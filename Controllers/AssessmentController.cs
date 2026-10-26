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

    public AssessmentController(IAssessmentService assessmentService, IOwnerService ownerService, ApplicationDbContext context)
    {
        _assessmentService = assessmentService;
        _ownerService = ownerService;
        _context = context;
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

    // GET: /Assessment/Create
    public IActionResult Create()
    {
        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();
        var model = new TaxDeclaration
        {
            TaxDeclarationNumber = $"TD-{DateTime.Now.Year}-00{new Random().Next(1000, 9999)}",
            TransactionCode = "DC",
            PropertyIndexNumber = $"101-01-{new Random().Next(100, 999)}-01-{new Random().Next(10, 99)}",
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
