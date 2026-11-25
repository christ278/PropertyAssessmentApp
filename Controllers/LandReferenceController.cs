using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

[Authorize]
public class LandReferenceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;

    public LandReferenceController(ApplicationDbContext context, IPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    private bool HasSettingsAccess() =>
        _permissionService.CanAccess(User, "LandReference", "Index")
        || User.IsInRole("SystemAdmin")
        || string.Equals(User.Identity?.Name, "admin", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "classification", string status = "all", string? search = null)
    {
        if (!HasSettingsAccess())
        {
            TempData["ErrorMessage"] = "Access Denied: You do not have permission to manage Land Reference tables.";
            return RedirectToAction("Index", "Home");
        }

        var normalizedTab = (tab ?? "classification").Trim().ToLowerInvariant();
        var validTabs = new HashSet<string> { "classification", "subclass", "actualuse", "assessmentlevel", "adjustment", "improvement" };
        if (!validTabs.Contains(normalizedTab)) normalizedTab = "classification";

        var normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
        var term = (search ?? string.Empty).Trim();

        var allClassifications = await _context.LandClassifications
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.ClassificationName)
            .ToListAsync();

        var allActualUses = await _context.LandActualUses
            .AsNoTracking()
            .OrderBy(a => a.Rank ?? int.MaxValue)
            .ThenBy(a => a.ActualUseName)
            .ToListAsync();

        var allSubClasses = await _context.LandSubClassifications
            .AsNoTracking()
            .OrderBy(s => s.ClassificationId)
            .ThenBy(s => s.SubClass)
            .ToListAsync();

        var allAssessmentLevels = await _context.LandAssessmentLevels
            .AsNoTracking()
            .OrderBy(l => l.ActualUseName)
            .ThenByDescending(l => l.RevisionYear)
            .ToListAsync();

        var allAdjustments = await _context.LandAdjustmentFactors
            .AsNoTracking()
            .OrderBy(a => a.Description)
            .ToListAsync();

        var allImprovements = await _context.LandImprovementTypes
            .AsNoTracking()
            .OrderBy(i => i.Kind)
            .ThenBy(i => i.TypeDescription)
            .ToListAsync();

        var classMap = allClassifications.ToDictionary(c => c.Id, c => c.ClassificationName);
        var actualUseMap = allActualUses.ToDictionary(a => a.Id, a => a.ActualUseName);

        bool MatchesStatus(bool isActive) => normalizedStatus switch
        {
            "active" => isActive,
            "inactive" => !isActive,
            _ => true
        };

        bool MatchesSearch(params string?[] fields) =>
            string.IsNullOrEmpty(term) ||
            fields.Any(f => !string.IsNullOrWhiteSpace(f) && f.Contains(term, StringComparison.OrdinalIgnoreCase));

        var vm = new LandReferenceSettingsViewModel
        {
            ActiveTab = normalizedTab,
            StatusFilter = normalizedStatus,
            Search = term,
            ClassificationNames = classMap,
            ActualUseNames = actualUseMap,
            Classifications = allClassifications
                .Where(c => MatchesStatus(c.IsActive) && MatchesSearch(c.ClassificationCode, c.ClassificationName, c.Prefix))
                .ToList(),
            SubClassifications = allSubClasses
                .Where(s => MatchesStatus(s.IsActive) && MatchesSearch(s.SubClass, classMap.GetValueOrDefault(s.ClassificationId)))
                .ToList(),
            ActualUses = allActualUses
                .Where(a => MatchesStatus(a.IsActive) && MatchesSearch(a.ActualUseCode, a.ActualUseName))
                .ToList(),
            AssessmentLevels = allAssessmentLevels
                .Where(l => MatchesStatus(l.IsActive) && MatchesSearch(l.ActualUseName, l.ActualUseId.HasValue ? actualUseMap.GetValueOrDefault(l.ActualUseId.Value) : null))
                .ToList(),
            AdjustmentFactors = allAdjustments
                .Where(a => MatchesStatus(a.IsActive) && MatchesSearch(a.Description, a.Distance))
                .ToList(),
            ImprovementTypes = allImprovements
                .Where(i => MatchesStatus(i.IsActive) && MatchesSearch(i.TypeCode, i.TypeDescription, i.Kind))
                .ToList()
        };

        return View(vm);
    }

    private IActionResult RedirectToTab(string tab, string status = "all", string? search = null) =>
        RedirectToAction(nameof(Index), new { tab, status, search });

    // 1. CLASSIFICATION
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveClassification(int id, string classificationCode, string classificationName, int? ranking, string? prefix, int sortOrder = 1, bool isActive = true)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (string.IsNullOrWhiteSpace(classificationCode) || string.IsNullOrWhiteSpace(classificationName))
        {
            TempData["ErrorMessage"] = "Classification Code and Name are required.";
            return RedirectToTab("classification");
        }

        if (id == 0)
        {
            _context.LandClassifications.Add(new LandClassification
            {
                ClassificationCode = classificationCode.Trim().ToUpperInvariant(),
                ClassificationName = classificationName.Trim(),
                Ranking = ranking,
                Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim().ToUpperInvariant(),
                SortOrder = sortOrder,
                IsActive = isActive
            });
            TempData["SuccessMessage"] = $"Classification '{classificationName.Trim()}' added.";
        }
        else
        {
            var entity = await _context.LandClassifications.FindAsync(id);
            if (entity == null) { TempData["ErrorMessage"] = "Classification not found."; return RedirectToTab("classification"); }
            entity.ClassificationCode = classificationCode.Trim().ToUpperInvariant();
            entity.ClassificationName = classificationName.Trim();
            entity.Ranking = ranking;
            entity.Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim().ToUpperInvariant();
            entity.SortOrder = sortOrder;
            entity.IsActive = isActive;
            TempData["SuccessMessage"] = $"Classification '{entity.ClassificationName}' updated.";
        }

        await _context.SaveChangesAsync();
        return RedirectToTab("classification");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleClassificationStatus(int id, string status = "all", string? search = null)
    {
        if (!HasSettingsAccess()) return Forbid();
        var entity = await _context.LandClassifications.FindAsync(id);
        if (entity == null) { TempData["ErrorMessage"] = "Classification not found."; return RedirectToTab("classification", status, search); }
        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Classification '{entity.ClassificationName}' marked as {(entity.IsActive ? "Active" : "Inactive")}.";
        return RedirectToTab("classification", status, search);
    }

    // 2. SUB-CLASSIFICATION
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSubClassification(int id, int classificationId, string subClass, decimal unitValue, int revisionYear = 2011, bool isActive = true)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (classificationId <= 0 || string.IsNullOrWhiteSpace(subClass))
        {
            TempData["ErrorMessage"] = "Classification and Sub-Class name are required.";
            return RedirectToTab("subclass");
        }

        if (id == 0)
        {
            _context.LandSubClassifications.Add(new LandSubClassification
            {
                ClassificationId = classificationId,
                SubClass = subClass.Trim(),
                UnitValue = unitValue,
                RevisionYear = revisionYear,
                IsActive = isActive
            });
            TempData["SuccessMessage"] = $"Sub-Class '{subClass.Trim()}' added.";
        }
        else
        {
            var entity = await _context.LandSubClassifications.FindAsync(id);
            if (entity == null) { TempData["ErrorMessage"] = "Sub-Class not found."; return RedirectToTab("subclass"); }
            entity.ClassificationId = classificationId;
            entity.SubClass = subClass.Trim();
            entity.UnitValue = unitValue;
            entity.RevisionYear = revisionYear;
            entity.IsActive = isActive;
            TempData["SuccessMessage"] = $"Sub-Class '{entity.SubClass}' updated.";
        }

        await _context.SaveChangesAsync();
        return RedirectToTab("subclass");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSubClassificationStatus(int id, string status = "all", string? search = null)
    {
        if (!HasSettingsAccess()) return Forbid();
        var entity = await _context.LandSubClassifications.FindAsync(id);
        if (entity == null) { TempData["ErrorMessage"] = "Sub-Class not found."; return RedirectToTab("subclass", status, search); }
        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Sub-Class '{entity.SubClass}' marked as {(entity.IsActive ? "Active" : "Inactive")}.";
        return RedirectToTab("subclass", status, search);
    }

    // 3. ACTUAL USE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveActualUse(int id, string actualUseCode, string actualUseName, bool isTaxExempt = false, int? rank = null, bool isActive = true)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (string.IsNullOrWhiteSpace(actualUseCode) || string.IsNullOrWhiteSpace(actualUseName))
        {
            TempData["ErrorMessage"] = "Actual Use Code and Name are required.";
            return RedirectToTab("actualuse");
        }

        if (id == 0)
        {
            _context.LandActualUses.Add(new LandActualUse
            {
                ActualUseCode = actualUseCode.Trim().ToUpperInvariant(),
                ActualUseName = actualUseName.Trim(),
                IsTaxExempt = isTaxExempt,
                Rank = rank,
                IsActive = isActive
            });
            TempData["SuccessMessage"] = $"Actual Use '{actualUseName.Trim()}' added.";
        }
        else
        {
            var entity = await _context.LandActualUses.FindAsync(id);
            if (entity == null) { TempData["ErrorMessage"] = "Actual Use not found."; return RedirectToTab("actualuse"); }
            entity.ActualUseCode = actualUseCode.Trim().ToUpperInvariant();
            entity.ActualUseName = actualUseName.Trim();
            entity.IsTaxExempt = isTaxExempt;
            entity.Rank = rank;
            entity.IsActive = isActive;
            TempData["SuccessMessage"] = $"Actual Use '{entity.ActualUseName}' updated.";
        }

        await _context.SaveChangesAsync();
        return RedirectToTab("actualuse");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActualUseStatus(int id, string status = "all", string? search = null)
    {
        if (!HasSettingsAccess()) return Forbid();
        var entity = await _context.LandActualUses.FindAsync(id);
        if (entity == null) { TempData["ErrorMessage"] = "Actual Use not found."; return RedirectToTab("actualuse", status, search); }
        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Actual Use '{entity.ActualUseName}' marked as {(entity.IsActive ? "Active" : "Inactive")}.";
        return RedirectToTab("actualuse", status, search);
    }

    // 4. ASSESSMENT LEVEL
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssessmentLevel(int id, int? actualUseId, string? actualUseName, decimal assessmentLevel, int revisionYear = 2011, bool isActive = true)
    {
        if (!HasSettingsAccess()) return Forbid();

        var resolvedName = (actualUseName ?? string.Empty).Trim();
        if (actualUseId.HasValue && actualUseId.Value > 0 && string.IsNullOrWhiteSpace(resolvedName))
        {
            var au = await _context.LandActualUses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == actualUseId.Value);
            resolvedName = au?.ActualUseName ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            TempData["ErrorMessage"] = "Actual Use Name is required.";
            return RedirectToTab("assessmentlevel");
        }

        if (id == 0)
        {
            _context.LandAssessmentLevels.Add(new LandAssessmentLevel
            {
                ActualUseId = actualUseId > 0 ? actualUseId : null,
                ActualUseName = resolvedName,
                AssessmentLevel = assessmentLevel,
                RevisionYear = revisionYear,
                IsActive = isActive
            });
            TempData["SuccessMessage"] = $"Assessment Level for '{resolvedName}' added.";
        }
        else
        {
            var entity = await _context.LandAssessmentLevels.FindAsync(id);
            if (entity == null) { TempData["ErrorMessage"] = "Assessment Level not found."; return RedirectToTab("assessmentlevel"); }
            entity.ActualUseId = actualUseId > 0 ? actualUseId : null;
            entity.ActualUseName = resolvedName;
            entity.AssessmentLevel = assessmentLevel;
            entity.RevisionYear = revisionYear;
            entity.IsActive = isActive;
            TempData["SuccessMessage"] = $"Assessment Level for '{entity.ActualUseName}' updated.";
        }

        await _context.SaveChangesAsync();
        return RedirectToTab("assessmentlevel");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAssessmentLevelStatus(int id, string status = "all", string? search = null)
    {
        if (!HasSettingsAccess()) return Forbid();
        var entity = await _context.LandAssessmentLevels.FindAsync(id);
        if (entity == null) { TempData["ErrorMessage"] = "Assessment Level not found."; return RedirectToTab("assessmentlevel", status, search); }
        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Assessment Level '{entity.ActualUseName}' marked as {(entity.IsActive ? "Active" : "Inactive")}.";
        return RedirectToTab("assessmentlevel", status, search);
    }

    // 5. ADJUSTMENT FACTOR
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAdjustmentFactor(int id, string description, string? distance, decimal adjustmentPercent, int revisionYear = 2011, bool isActive = true)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (string.IsNullOrWhiteSpace(description))
        {
            TempData["ErrorMessage"] = "Adjustment Description is required.";
            return RedirectToTab("adjustment");
        }

        if (id == 0)
        {
            _context.LandAdjustmentFactors.Add(new LandAdjustmentFactor
            {
                Description = description.Trim(),
                Distance = string.IsNullOrWhiteSpace(distance) ? null : distance.Trim(),
                AdjustmentPercent = adjustmentPercent,
                RevisionYear = revisionYear,
                IsActive = isActive
            });
            TempData["SuccessMessage"] = $"Adjustment Factor '{description.Trim()}' added.";
        }
        else
        {
            var entity = await _context.LandAdjustmentFactors.FindAsync(id);
            if (entity == null) { TempData["ErrorMessage"] = "Adjustment Factor not found."; return RedirectToTab("adjustment"); }
            entity.Description = description.Trim();
            entity.Distance = string.IsNullOrWhiteSpace(distance) ? null : distance.Trim();
            entity.AdjustmentPercent = adjustmentPercent;
            entity.RevisionYear = revisionYear;
            entity.IsActive = isActive;
            TempData["SuccessMessage"] = $"Adjustment Factor '{entity.Description}' updated.";
        }

        await _context.SaveChangesAsync();
        return RedirectToTab("adjustment");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdjustmentFactorStatus(int id, string status = "all", string? search = null)
    {
        if (!HasSettingsAccess()) return Forbid();
        var entity = await _context.LandAdjustmentFactors.FindAsync(id);
        if (entity == null) { TempData["ErrorMessage"] = "Adjustment Factor not found."; return RedirectToTab("adjustment", status, search); }
        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Adjustment Factor '{entity.Description}' marked as {(entity.IsActive ? "Active" : "Inactive")}.";
        return RedirectToTab("adjustment", status, search);
    }

    // 6. IMPROVEMENT TYPE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveImprovementType(int id, string typeCode, string typeDescription, string kind = "FRUIT BEARING", decimal baseValue = 50.00m, decimal assessmentLevel = 40.00m, int revisionYear = 2011, bool isActive = true)
    {
        if (!HasSettingsAccess()) return Forbid();
        if (string.IsNullOrWhiteSpace(typeCode) || string.IsNullOrWhiteSpace(typeDescription))
        {
            TempData["ErrorMessage"] = "Type Code and Description are required.";
            return RedirectToTab("improvement");
        }

        if (id == 0)
        {
            _context.LandImprovementTypes.Add(new LandImprovementType
            {
                TypeCode = typeCode.Trim().ToUpperInvariant(),
                TypeDescription = typeDescription.Trim(),
                Kind = string.IsNullOrWhiteSpace(kind) ? "FRUIT BEARING" : kind.Trim().ToUpperInvariant(),
                BaseValue = baseValue,
                AssessmentLevel = assessmentLevel,
                RevisionYear = revisionYear,
                IsActive = isActive
            });
            TempData["SuccessMessage"] = $"Improvement Type '{typeDescription.Trim()}' added.";
        }
        else
        {
            var entity = await _context.LandImprovementTypes.FindAsync(id);
            if (entity == null) { TempData["ErrorMessage"] = "Improvement Type not found."; return RedirectToTab("improvement"); }
            entity.TypeCode = typeCode.Trim().ToUpperInvariant();
            entity.TypeDescription = typeDescription.Trim();
            entity.Kind = string.IsNullOrWhiteSpace(kind) ? "FRUIT BEARING" : kind.Trim().ToUpperInvariant();
            entity.BaseValue = baseValue;
            entity.AssessmentLevel = assessmentLevel;
            entity.RevisionYear = revisionYear;
            entity.IsActive = isActive;
            TempData["SuccessMessage"] = $"Improvement Type '{entity.TypeDescription}' updated.";
        }

        await _context.SaveChangesAsync();
        return RedirectToTab("improvement");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleImprovementTypeStatus(int id, string status = "all", string? search = null)
    {
        if (!HasSettingsAccess()) return Forbid();
        var entity = await _context.LandImprovementTypes.FindAsync(id);
        if (entity == null) { TempData["ErrorMessage"] = "Improvement Type not found."; return RedirectToTab("improvement", status, search); }
        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Improvement Type '{entity.TypeDescription}' marked as {(entity.IsActive ? "Active" : "Inactive")}.";
        return RedirectToTab("improvement", status, search);
    }
}
