using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

[Authorize]
public class BuildingReferenceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;

    public BuildingReferenceController(ApplicationDbContext context, IPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    private bool HasSettingsAccess() =>
        _permissionService.CanAccess(User, "BuildingReference", "Index")
        || User.IsInRole("SystemAdmin")
        || string.Equals(User.Identity?.Name, "admin", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "classification", string? search = null)
    {
        if (!HasSettingsAccess())
        {
            TempData["ErrorMessage"] = "Access Denied: You do not have permission to manage Building Reference tables.";
            return RedirectToAction("Index", "Home");
        }

        var normalizedTab = (tab ?? "classification").Trim().ToLowerInvariant();
        var validTabs = new HashSet<string> { "classification", "bucc", "assessmentlevel", "depreciation", "additionalitem" };
        if (!validTabs.Contains(normalizedTab)) normalizedTab = "classification";

        var term = (search ?? string.Empty).Trim();

        var classifications = await _context.BuildingClassifications
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.ClassificationCode)
            .ToListAsync();

        var structureRates = await _context.BuildingStructureRates
            .AsNoTracking()
            .OrderBy(r => r.BuildingStructuralTypeId)
            .ThenBy(r => r.BuildingTypeId)
            .ThenByDescending(r => r.RevisionYear)
            .ToListAsync();

        var structuralTypes = await _context.BuildingStructuralTypes
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync();

        var buildingTypes = await _context.BuildingTypes
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync();

        var assessmentLevels = await _context.BuildingAssessmentLevels
            .AsNoTracking()
            .OrderBy(l => l.ActualUseId)
            .ThenBy(l => l.LowerBound)
            .ToListAsync();

        var depreciations = await _context.BuildingDepreciations
            .AsNoTracking()
            .OrderBy(d => d.TypeCode)
            .ThenBy(d => d.ActualUseId)
            .ThenBy(d => d.AgeLowerBound)
            .ToListAsync();

        var additionalItems = await _context.BuildingAdditionalItems
            .AsNoTracking()
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.AdditionalItemDescription)
            .ToListAsync();

        if (!string.IsNullOrEmpty(term))
        {
            var stMap = structuralTypes.ToDictionary(s => s.Id, s => $"{s.StructuralCode} {s.StructuralDescription}");
            var btMap = buildingTypes.ToDictionary(b => b.Id, b => $"{b.BuildingTypeCode} {b.BuildingTypeDescription}");

            classifications = classifications
                .Where(c => c.ClassificationCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || c.ClassificationName.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || (c.StructuralTypeDefault ?? "").Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            structureRates = structureRates
                .Where(r => (stMap.TryGetValue(r.BuildingStructuralTypeId, out var st) && st.Contains(term, StringComparison.OrdinalIgnoreCase))
                         || (btMap.TryGetValue(r.BuildingTypeId, out var bt) && bt.Contains(term, StringComparison.OrdinalIgnoreCase))
                         || r.RevisionYear.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            assessmentLevels = assessmentLevels
                .Where(l => ActualUseLabel(l.ActualUseId).Contains(term, StringComparison.OrdinalIgnoreCase)
                         || l.RevisionYear.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            depreciations = depreciations
                .Where(d => d.TypeCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || d.DegreeOfMaintenance.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || ActualUseLabel(d.ActualUseId).Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            additionalItems = additionalItems
                .Where(i => i.AdditionalItemDescription.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewBag.ActiveTab = normalizedTab;
        ViewBag.Search = search ?? string.Empty;
        ViewBag.Classifications = classifications;
        ViewBag.StructureRates = structureRates;
        ViewBag.StructuralTypes = structuralTypes;
        ViewBag.BuildingTypes = buildingTypes;
        ViewBag.AssessmentLevels = assessmentLevels;
        ViewBag.Depreciations = depreciations;
        ViewBag.AdditionalItems = additionalItems;

        return View();
    }

    public static string ActualUseLabel(int actualUseId) => actualUseId switch
    {
        1 => "RESIDENTIAL",
        4 => "COMMERCIAL",
        6 => "INDUSTRIAL",
        10 => "SPECIAL / CULTURAL",
        17 => "AGRICULTURAL",
        _ => $"USE #{actualUseId}"
    };

    private IActionResult RedirectTab(string tab) => RedirectToAction(nameof(Index), new { tab });

    // =============================================================
    // 1. Building Classifications CRUD
    // =============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveClassification(int id, string classificationCode, string classificationName, string? description, string? structuralTypeDefault, decimal defaultUnitConstructionCost, int sortOrder, bool isActive = true)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (string.IsNullOrWhiteSpace(classificationCode) || string.IsNullOrWhiteSpace(classificationName))
        {
            TempData["ErrorMessage"] = "Classification Code and Name are required.";
            return RedirectTab("classification");
        }

        if (id > 0)
        {
            var existing = await _context.BuildingClassifications.FindAsync(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Building classification not found.";
                return RedirectTab("classification");
            }
            existing.ClassificationCode = classificationCode.Trim();
            existing.ClassificationName = classificationName.Trim();
            existing.Description = description?.Trim();
            existing.StructuralTypeDefault = structuralTypeDefault?.Trim();
            existing.DefaultUnitConstructionCost = defaultUnitConstructionCost;
            existing.SortOrder = sortOrder;
            existing.IsActive = isActive;
        }
        else
        {
            _context.BuildingClassifications.Add(new BuildingClassification
            {
                ClassificationCode = classificationCode.Trim(),
                ClassificationName = classificationName.Trim(),
                Description = description?.Trim(),
                StructuralTypeDefault = structuralTypeDefault?.Trim(),
                DefaultUnitConstructionCost = defaultUnitConstructionCost,
                SortOrder = sortOrder,
                IsActive = isActive
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Building classification saved.";
        return RedirectTab("classification");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClassification(int id)
    {
        if (!HasSettingsAccess()) return Forbid();
        var item = await _context.BuildingClassifications.FindAsync(id);
        if (item != null)
        {
            _context.BuildingClassifications.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Building classification deleted.";
        }
        return RedirectTab("classification");
    }

    // =============================================================
    // 2. BUCC Structure Rates CRUD
    // =============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveStructureRate(int id, int buildingStructuralTypeId, int buildingTypeId, decimal minRate, decimal maxRate, int revisionYear = 2011)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (buildingStructuralTypeId <= 0 || buildingTypeId <= 0)
        {
            TempData["ErrorMessage"] = "Structural Type and Building Type are required.";
            return RedirectTab("bucc");
        }

        if (id > 0)
        {
            var existing = await _context.BuildingStructureRates.FindAsync(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "BUCC Structure Rate not found.";
                return RedirectTab("bucc");
            }
            existing.BuildingStructuralTypeId = buildingStructuralTypeId;
            existing.BuildingTypeId = buildingTypeId;
            existing.MinRate = minRate;
            existing.MaxRate = maxRate;
            existing.RevisionYear = revisionYear;
        }
        else
        {
            _context.BuildingStructureRates.Add(new BuildingStructureRate
            {
                BuildingStructuralTypeId = buildingStructuralTypeId,
                BuildingTypeId = buildingTypeId,
                MinRate = minRate,
                MaxRate = maxRate,
                RevisionYear = revisionYear
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "BUCC Structure Rate saved.";
        return RedirectTab("bucc");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStructureRate(int id)
    {
        if (!HasSettingsAccess()) return Forbid();
        var item = await _context.BuildingStructureRates.FindAsync(id);
        if (item != null)
        {
            _context.BuildingStructureRates.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "BUCC Structure Rate deleted.";
        }
        return RedirectTab("bucc");
    }

    // =============================================================
    // 3. Building Assessment Levels CRUD
    // =============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssessmentLevel(int id, int actualUseId, decimal lowerBound, decimal upperBound, decimal assessmentLevel, int revisionYear = 2011)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (id > 0)
        {
            var existing = await _context.BuildingAssessmentLevels.FindAsync(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Building Assessment Level not found.";
                return RedirectTab("assessmentlevel");
            }
            existing.ActualUseId = actualUseId;
            existing.LowerBound = lowerBound;
            existing.UpperBound = upperBound;
            existing.AssessmentLevel = assessmentLevel;
            existing.RevisionYear = revisionYear;
        }
        else
        {
            _context.BuildingAssessmentLevels.Add(new BuildingAssessmentLevel
            {
                ActualUseId = actualUseId,
                LowerBound = lowerBound,
                UpperBound = upperBound,
                AssessmentLevel = assessmentLevel,
                RevisionYear = revisionYear
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Building Assessment Level saved.";
        return RedirectTab("assessmentlevel");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssessmentLevel(int id)
    {
        if (!HasSettingsAccess()) return Forbid();
        var item = await _context.BuildingAssessmentLevels.FindAsync(id);
        if (item != null)
        {
            _context.BuildingAssessmentLevels.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Building Assessment Level deleted.";
        }
        return RedirectTab("assessmentlevel");
    }

    // =============================================================
    // 4. Building Depreciation Schedule CRUD
    // =============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDepreciation(int id, string typeCode, int actualUseId, string degreeOfMaintenance, int ageLowerBound, int ageUpperBound, decimal percentDepreciated)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (string.IsNullOrWhiteSpace(typeCode))
        {
            TempData["ErrorMessage"] = "Type Code is required.";
            return RedirectTab("depreciation");
        }

        if (id > 0)
        {
            var existing = await _context.BuildingDepreciations.FindAsync(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Depreciation rule not found.";
                return RedirectTab("depreciation");
            }
            existing.TypeCode = typeCode.Trim();
            existing.ActualUseId = actualUseId;
            existing.DegreeOfMaintenance = (degreeOfMaintenance ?? "AVERAGE").Trim().ToUpperInvariant();
            existing.AgeLowerBound = ageLowerBound;
            existing.AgeUpperBound = ageUpperBound;
            existing.PercentDepreciated = percentDepreciated;
        }
        else
        {
            _context.BuildingDepreciations.Add(new BuildingDepreciation
            {
                TypeCode = typeCode.Trim(),
                ActualUseId = actualUseId,
                DegreeOfMaintenance = (degreeOfMaintenance ?? "AVERAGE").Trim().ToUpperInvariant(),
                AgeLowerBound = ageLowerBound,
                AgeUpperBound = ageUpperBound,
                PercentDepreciated = percentDepreciated
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Depreciation rule saved.";
        return RedirectTab("depreciation");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDepreciation(int id)
    {
        if (!HasSettingsAccess()) return Forbid();
        var item = await _context.BuildingDepreciations.FindAsync(id);
        if (item != null)
        {
            _context.BuildingDepreciations.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Depreciation rule deleted.";
        }
        return RedirectTab("depreciation");
    }

    // =============================================================
    // 5. Building Additional Items CRUD
    // =============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAdditionalItem(int id, string additionalItemDescription, decimal percentUnitValue, decimal costPerSQM, decimal excessSQM, decimal excessPercentage, int revisionYear = 2011, int sortOrder = 0, bool isActive = true)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (string.IsNullOrWhiteSpace(additionalItemDescription))
        {
            TempData["ErrorMessage"] = "Description is required.";
            return RedirectTab("additionalitem");
        }

        if (id > 0)
        {
            var existing = await _context.BuildingAdditionalItems.FindAsync(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Additional item not found.";
                return RedirectTab("additionalitem");
            }
            existing.AdditionalItemDescription = additionalItemDescription.Trim();
            existing.PercentUnitValue = percentUnitValue;
            existing.CostPerSQM = costPerSQM;
            existing.ExcessSQM = excessSQM;
            existing.ExcessPercentage = excessPercentage;
            existing.RevisionYear = revisionYear;
            existing.SortOrder = sortOrder;
            existing.IsActive = isActive;
        }
        else
        {
            _context.BuildingAdditionalItems.Add(new BuildingAdditionalItem
            {
                AdditionalItemDescription = additionalItemDescription.Trim(),
                PercentUnitValue = percentUnitValue,
                CostPerSQM = costPerSQM,
                ExcessSQM = excessSQM,
                ExcessPercentage = excessPercentage,
                RevisionYear = revisionYear,
                SortOrder = sortOrder,
                IsActive = isActive
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Additional item saved.";
        return RedirectTab("additionalitem");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAdditionalItem(int id)
    {
        if (!HasSettingsAccess()) return Forbid();
        var item = await _context.BuildingAdditionalItems.FindAsync(id);
        if (item != null)
        {
            _context.BuildingAdditionalItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Additional item deleted.";
        }
        return RedirectTab("additionalitem");
    }
}
