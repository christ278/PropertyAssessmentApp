using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Helpers;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

[Authorize]
public class RPTReportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly IRPTCertificatePdfService _certPdfService;

    public RPTReportController(
        ApplicationDbContext context,
        IPermissionService permissionService,
        IRPTCertificatePdfService certPdfService)
    {
        _context = context;
        _permissionService = permissionService;
        _certPdfService = certPdfService;
    }

    // GET: /RPTReport or /RPTReport/Index
    // Displays the Report Generator Form
    [HttpGet]
    public async Task<IActionResult> Index(string? tdn = null, int? reportId = null, Guid? landHoldingRequestId = null)
    {
        // Check if user has general access to Reports module
        if (!_permissionService.CanAccess(User, "RPTReport", "Index"))
        {
            TempData["ErrorMessage"] = "Access Restricted: You do not have permission to access the Real Property Tax Reports subsystem.";
            return RedirectToAction("Index", "Home");
        }

        var accessibleReports = await GetAccessibleReportsForUserAsync();

        var model = new GenerateRPTReportViewModel
        {
            TaxDeclarationNumber = tdn ?? string.Empty,
            RPTReportId = reportId == 5 ? 6 : (reportId == 2 ? 4 : reportId),
            AccessibleReports = accessibleReports,
            RequestedBy = User.Identity?.Name ?? string.Empty
        };

        if (landHoldingRequestId.HasValue)
        {
            var lh = await _context.LandHoldingRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == landHoldingRequestId.Value);

            if (lh != null)
            {
                int targetReportId = reportId ?? (
                    lh.CertNoLandholdings || lh.CertTotalLandholdings ? 6 :
                    lh.CertTypeTaxDeclaration ? 1 :
                    lh.CertAggregateAgriLandholdings ? 7 :
                    lh.CertNoImprovementFromDeath ? 4 :
                    6);

                if (targetReportId == 5) targetReportId = 6;
                if (targetReportId == 2) targetReportId = 4;
                model.RPTReportId = targetReportId;

                if (!string.IsNullOrWhiteSpace(lh.OwnerName))
                {
                    var allOwners = await _context.TaxpayerOwners.AsNoTracking().ToListAsync();
                    var matchedOwner = allOwners.FirstOrDefault(o =>
                        string.Equals(o.DisplayName?.Trim(), lh.OwnerName.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (matchedOwner != null)
                    {
                        model.OwnerId = matchedOwner.Id;
                        model.OwnerName = matchedOwner.DisplayName;
                    }
                    else
                    {
                        model.OwnerName = lh.OwnerName;
                    }
                }

                if (!string.IsNullOrWhiteSpace(lh.TaxDeclarationNumber))
                {
                    var tdnParts = lh.TaxDeclarationNumber
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();

                    if (targetReportId == 4 && tdnParts.Count > 0)
                    {
                        var candidateLands = await _context.TaxDeclarations
                            .AsNoTracking()
                            .Where(t => t.Kind == PropertyKind.Land
                                     && !t.IsCancelledAndSuperseded
                                     && t.Status == AssessmentStatus.Active
                                     && tdnParts.Contains(t.TaxDeclarationNumber))
                            .Select(t => t.TaxDeclarationNumber)
                            .ToListAsync();

                        var firstActiveLand = tdnParts.FirstOrDefault(p =>
                            !p.EndsWith("-00000") &&
                            candidateLands.Any(c => string.Equals(c, p, StringComparison.OrdinalIgnoreCase)));

                        model.TaxDeclarationNumber = firstActiveLand ?? tdnParts.FirstOrDefault() ?? string.Empty;
                    }
                    else
                    {
                        model.TaxDeclarationNumber = tdnParts.FirstOrDefault() ?? string.Empty;
                    }
                }

                if (!string.IsNullOrWhiteSpace(lh.Purposes))
                {
                    model.Purpose = lh.Purposes;
                }

                model.RequestedBy = !string.IsNullOrWhiteSpace(lh.RequesterName)
                    ? lh.RequesterName
                    : (!string.IsNullOrWhiteSpace(lh.OwnerName) ? lh.OwnerName : model.RequestedBy);

                model.ORNumber = lh.ORNumber;
                ViewBag.LandHoldingRequestId = lh.Id;
            }
        }

        ViewBag.TotalAccessibleReports = accessibleReports.Count;
        ViewBag.CanManagePermissions = _permissionService.CanAccess(User, "UserRole", "Permissions");

        return View(model);
    }

    // POST: /RPTReport/Generate
    // Validates Report Type and its required parameter (Owner for Land Holding, TDN for other types)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateRPTReportViewModel model)
    {
        model.AccessibleReports = await GetAccessibleReportsForUserAsync();

        // 1. Validate Report Type Selection
        if (!model.RPTReportId.HasValue || model.RPTReportId.Value <= 0)
        {
            ModelState.AddModelError(nameof(model.RPTReportId), "Please select a Report / Certification Type.");
        }
        else
        {
            bool hasAccess = CanAccessReportOrHolding(model.RPTReportId.Value);
            if (!hasAccess)
            {
                ModelState.AddModelError(nameof(model.RPTReportId), "Access Denied: Your account role is not authorized to generate this specific report type.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.TotalAccessibleReports = model.AccessibleReports.Count;
            ViewBag.CanManagePermissions = _permissionService.CanAccess(User, "UserRole", "Permissions");
            return View("Index", model);
        }

        var report = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == model.RPTReportId!.Value);
        if (report == null)
        {
            ModelState.AddModelError(nameof(model.RPTReportId), "The selected report configuration does not exist.");
            ViewBag.TotalAccessibleReports = model.AccessibleReports.Count;
            ViewBag.CanManagePermissions = _permissionService.CanAccess(User, "UserRole", "Permissions");
            return View("Index", model);
        }

        // 2. Branch by Certification Type: Land Holding (5 or 6) uses Registered Owner; others use TDN
        if (IsLandHoldingReport(report))
        {
            if (!model.OwnerId.HasValue && string.IsNullOrWhiteSpace(model.OwnerName))
            {
                ModelState.AddModelError(nameof(model.OwnerId), "Please select a Registered Owner for Land Holding certification.");
                ViewBag.TotalAccessibleReports = model.AccessibleReports.Count;
                ViewBag.CanManagePermissions = _permissionService.CanAccess(User, "UserRole", "Permissions");
                return View("Index", model);
            }

            var (effectiveReport, primaryTd, relatedProperties, resolvedOwnerId, resolvedOwnerName) =
                await ResolveOwnerHoldingAsync(model.OwnerId, model.OwnerName, report, model.SelectedPropertyIdsJson);

            var certificateModel = ReportPrintHelper.BuildCertificateModel(
                primaryTd,
                effectiveReport,
                model.RequestedBy,
                model.Purpose,
                model.ORNumber,
                1470.00m,
                relatedProperties);

            var propertyIdsParam = relatedProperties.Count > 0
                ? string.Join(",", relatedProperties.Select(p => p.Id))
                : "none";

            ViewBag.CustomPdfUrl = Url.Action(nameof(PrintPdf), "RPTReport", new
            {
                reportId = effectiveReport.RPTReportId,
                ownerId = resolvedOwnerId,
                ownerName = resolvedOwnerName,
                propertyIds = propertyIdsParam,
                requestor = model.RequestedBy,
                purpose = model.Purpose,
                orNumber = model.ORNumber
            });
            ViewBag.CustomDownloadPdfUrl = Url.Action(nameof(PrintPdf), "RPTReport", new
            {
                reportId = effectiveReport.RPTReportId,
                ownerId = resolvedOwnerId,
                ownerName = resolvedOwnerName,
                propertyIds = propertyIdsParam,
                requestor = model.RequestedBy,
                purpose = model.Purpose,
                orNumber = model.ORNumber,
                download = true
            });
            ViewBag.CustomPrintHtmlUrl = Url.Action(nameof(Print), "RPTReport", new
            {
                reportId = effectiveReport.RPTReportId,
                ownerId = resolvedOwnerId,
                ownerName = resolvedOwnerName,
                propertyIds = propertyIdsParam,
                requestor = model.RequestedBy,
                purpose = model.Purpose,
                orNumber = model.ORNumber
            });

            return View("ReportDetails", certificateModel);
        }

        // 3. Other Report Types: Validate TDN exists in Assessment Records
        TaxDeclaration? taxDeclaration = null;
        if (string.IsNullOrWhiteSpace(model.TaxDeclarationNumber))
        {
            ModelState.AddModelError(nameof(model.TaxDeclarationNumber), "Tax Declaration (TDN) No. is required.");
        }
        else
        {
            var cleanTdn = model.TaxDeclarationNumber.Trim();
            taxDeclaration = await _context.TaxDeclarations
                .Include(t => t.Owner)
                .Include(t => t.LandDetails)
                .Include(t => t.BuildingDetails)
                    .ThenInclude(b => b!.LandReferences)
                .Include(t => t.MachineryDetails)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == cleanTdn ||
                                          t.TaxDeclarationNumber.Replace(" ", "").ToLower() == cleanTdn.Replace(" ", "").ToLower());

            if (taxDeclaration == null)
            {
                ModelState.AddModelError(nameof(model.TaxDeclarationNumber), $"Tax Declaration (TDN) No. '{model.TaxDeclarationNumber}' does not exist in the list of Assessment Records.");
            }
            else if (IsImprovementReport(report) && !IsActiveApprovedLand(taxDeclaration))
            {
                ModelState.AddModelError(nameof(model.TaxDeclarationNumber), "Only active and approved Land Tax Declarations are allowed for Certification w/ No Improvement.");
            }
        }

        if (!ModelState.IsValid || taxDeclaration == null)
        {
            ViewBag.TotalAccessibleReports = model.AccessibleReports.Count;
            ViewBag.CanManagePermissions = _permissionService.CanAccess(User, "UserRole", "Permissions");
            return View("Index", model);
        }

        if (IsImprovementReport(report))
        {
            var (effectiveReport, relatedImprovements) =
                await ResolveLandImprovementAsync(taxDeclaration, report, model.SelectedPropertyIdsJson);

            var impCertModel = ReportPrintHelper.BuildCertificateModel(
                taxDeclaration,
                effectiveReport,
                model.RequestedBy,
                model.Purpose,
                model.ORNumber,
                1470.00m,
                relatedImprovements);

            var impIdsParam = relatedImprovements.Count > 0
                ? string.Join(",", relatedImprovements.Select(p => p.Id))
                : "none";

            ViewBag.CustomPdfUrl = Url.Action(nameof(PrintPdf), "RPTReport", new
            {
                tdn = taxDeclaration.TaxDeclarationNumber,
                reportId = effectiveReport.RPTReportId,
                propertyIds = impIdsParam,
                requestor = model.RequestedBy,
                purpose = model.Purpose,
                orNumber = model.ORNumber
            });
            ViewBag.CustomDownloadPdfUrl = Url.Action(nameof(PrintPdf), "RPTReport", new
            {
                tdn = taxDeclaration.TaxDeclarationNumber,
                reportId = effectiveReport.RPTReportId,
                propertyIds = impIdsParam,
                requestor = model.RequestedBy,
                purpose = model.Purpose,
                orNumber = model.ORNumber,
                download = true
            });
            ViewBag.CustomPrintHtmlUrl = Url.Action(nameof(Print), "RPTReport", new
            {
                tdn = taxDeclaration.TaxDeclarationNumber,
                reportId = effectiveReport.RPTReportId,
                propertyIds = impIdsParam,
                requestor = model.RequestedBy,
                purpose = model.Purpose,
                orNumber = model.ORNumber
            });

            return View("ReportDetails", impCertModel);
        }

        // 4. Related properties for DAR or other multi-property reports
        var related = await LoadRelatedPropertiesAsync(report, taxDeclaration);

        var certModel = ReportPrintHelper.BuildCertificateModel(
            taxDeclaration,
            report,
            model.RequestedBy,
            model.Purpose,
            model.ORNumber,
            1470.00m,
            related);

        return View("ReportDetails", certModel);
    }

    // GET: /RPTReport/Print?tdn=...&reportId=...
    // Clean standalone printout page without navigation chrome for pure paper/PDF output
    [HttpGet]
    public async Task<IActionResult> Print(string? tdn = null, int reportId = 0, Guid? ownerId = null, string? ownerName = null, string? propertyIds = null, string? requestor = null, string? purpose = null, string? orNumber = null)
    {
        if (reportId <= 0)
        {
            return BadRequest("Report ID is required.");
        }

        if (!CanAccessReportOrHolding(reportId))
        {
            return Forbid();
        }

        var report = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == reportId);
        if (report == null)
        {
            return NotFound($"Report ID '{reportId}' not found.");
        }

        if (IsLandHoldingReport(report) && (ownerId.HasValue || !string.IsNullOrWhiteSpace(ownerName)))
        {
            var (effectiveReport, primaryTd, relatedProperties, _, _) =
                await ResolveOwnerHoldingAsync(ownerId, ownerName, report, propertyIds);

            var holdingModel = ReportPrintHelper.BuildCertificateModel(
                primaryTd,
                effectiveReport,
                requestor,
                purpose,
                orNumber,
                1470.00m,
                relatedProperties);

            return View("Print", holdingModel);
        }

        if (string.IsNullOrWhiteSpace(tdn))
        {
            return BadRequest("TDN and Report ID are required.");
        }

        var cleanTdn = tdn.Trim();
        var taxDeclaration = await _context.TaxDeclarations
            .Include(t => t.Owner)
            .Include(t => t.LandDetails)
            .Include(t => t.BuildingDetails)
            .Include(t => t.MachineryDetails)
            .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == cleanTdn ||
                                      t.TaxDeclarationNumber.Replace(" ", "").ToLower() == cleanTdn.Replace(" ", "").ToLower());

        if (taxDeclaration == null)
        {
            return NotFound($"TDN '{cleanTdn}' not found in assessment records.");
        }

        if (IsImprovementReport(report))
        {
            if (!IsActiveApprovedLand(taxDeclaration))
            {
                return BadRequest("Only active and approved Land Tax Declarations are allowed for Certification w/ No Improvement.");
            }
            var (effectiveReport, relatedImprovements) = await ResolveLandImprovementAsync(taxDeclaration, report, propertyIds);
            report = effectiveReport;
            var impModel = ReportPrintHelper.BuildCertificateModel(
                taxDeclaration,
                report,
                requestor,
                purpose,
                orNumber,
                1470.00m,
                relatedImprovements);
            return View("Print", impModel);
        }

        var relatedList = await LoadRelatedPropertiesAsync(report, taxDeclaration);

        var certificateModel = ReportPrintHelper.BuildCertificateModel(
            taxDeclaration,
            report,
            requestor,
            purpose,
            orNumber,
            1470.00m,
            relatedList);

        return View("Print", certificateModel);
    }

    // GET: /RPTReport/PrintPdf?tdn=...&reportId=...&download=false
    // Renders the official QuestPDF certificate as a high-resolution PDF stream matching the physical sample
    [HttpGet]
    public async Task<IActionResult> PrintPdf(string? tdn = null, int reportId = 0, Guid? ownerId = null, string? ownerName = null, string? propertyIds = null, string? requestor = null, string? purpose = null, string? orNumber = null, bool download = false)
    {
        if (reportId <= 0)
        {
            return BadRequest("Report ID is required.");
        }

        if (!CanAccessReportOrHolding(reportId))
        {
            return Forbid();
        }

        var report = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == reportId);
        if (report == null)
        {
            return NotFound($"Report ID '{reportId}' not found.");
        }

        OfficialCertificateViewModel certificateModel;
        string safeKey;

        if (IsLandHoldingReport(report) && (ownerId.HasValue || !string.IsNullOrWhiteSpace(ownerName)))
        {
            var (effectiveReport, primaryTd, relatedProperties, _, resolvedOwnerName) =
                await ResolveOwnerHoldingAsync(ownerId, ownerName, report, propertyIds);

            certificateModel = ReportPrintHelper.BuildCertificateModel(
                primaryTd,
                effectiveReport,
                requestor,
                purpose,
                orNumber,
                1470.00m,
                relatedProperties);

            report = effectiveReport;
            safeKey = string.Concat((resolvedOwnerName ?? "Owner").Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(tdn))
            {
                return BadRequest("Tax Declaration Number (TDN) and Report ID are required.");
            }

            var cleanTdn = tdn.Trim();
            var taxDeclaration = await _context.TaxDeclarations
                .Include(t => t.Owner)
                .Include(t => t.LandDetails)
                .Include(t => t.BuildingDetails)
                .Include(t => t.MachineryDetails)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == cleanTdn ||
                                          t.TaxDeclarationNumber.Replace(" ", "").ToLower() == cleanTdn.Replace(" ", "").ToLower());

            if (taxDeclaration == null)
            {
                return NotFound($"TDN '{cleanTdn}' not found in assessment records.");
            }

            List<TaxDeclaration> relatedProperties;
            if (IsImprovementReport(report))
            {
                if (!IsActiveApprovedLand(taxDeclaration))
                {
                    return BadRequest("Only active and approved Land Tax Declarations are allowed for Certification w/ No Improvement.");
                }
                (report, relatedProperties) = await ResolveLandImprovementAsync(taxDeclaration, report, propertyIds);
            }
            else
            {
                relatedProperties = await LoadRelatedPropertiesAsync(report, taxDeclaration);
            }

            certificateModel = ReportPrintHelper.BuildCertificateModel(
                taxDeclaration,
                report,
                requestor,
                purpose,
                orNumber,
                1470.00m,
                relatedProperties);

            safeKey = string.Concat(cleanTdn.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        }

        var pdfBytes = _certPdfService.GenerateCertificatePdf(certificateModel);
        var safeReportTitle = string.Concat(report.Title.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        var fileName = $"{safeReportTitle}_{safeKey}.pdf";

        if (download)
        {
            return File(pdfBytes, "application/pdf", fileName);
        }

        return File(pdfBytes, "application/pdf");
    }

    // GET: /RPTReport/SearchOwners?q=...
    [HttpGet]
    public async Task<IActionResult> SearchOwners(string? q = null)
    {
        var query = _context.TaxpayerOwners.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var tokens = q.Trim()
                .Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var tok in tokens)
            {
                var t = tok;
                query = query.Where(o =>
                    (o.FirstName != null && o.FirstName.Contains(t)) ||
                    (o.MiddleName != null && o.MiddleName.Contains(t)) ||
                    (o.LastName != null && o.LastName.Contains(t)) ||
                    (o.CorporateName != null && o.CorporateName.Contains(t)) ||
                    o.TaxIdentNo.Contains(t) ||
                    o.RptinNumber.Contains(t));
            }
        }

        var owners = await query
            .OrderBy(o => o.LastName ?? o.CorporateName ?? o.FirstName)
            .Take(25)
            .ToListAsync();

        var activeTds = await _context.TaxDeclarations
            .AsNoTracking()
            .Where(t => !t.IsCancelledAndSuperseded && t.Status != AssessmentStatus.Cancelled)
            .Select(t => new { t.OwnerId, OwnerName = t.OwnerName ?? string.Empty })
            .ToListAsync();

        var results = owners.Select(o =>
        {
            int propCount = activeTds.Count(t => DoesNameMatchOwner(t.OwnerId, t.OwnerName, o, null));

            return new
            {
                id = o.Id,
                ownerName = o.DisplayName,
                tin = o.TaxIdentNo,
                address = o.FullAddress,
                propertyCount = propCount
            };
        }).ToList();

        return Json(results);
    }

    // GET: /RPTReport/GetOwnerProperties?ownerId=...&ownerName=...&landHoldingRequestId=...
    [HttpGet]
    public async Task<IActionResult> GetOwnerProperties(Guid? ownerId = null, string? ownerName = null, Guid? landHoldingRequestId = null)
    {
        Owner? owner = null;
        if (ownerId.HasValue && ownerId.Value != Guid.Empty)
        {
            owner = await _context.TaxpayerOwners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == ownerId.Value);
        }

        var targetName = (owner?.DisplayName ?? ownerName ?? string.Empty).Trim().ToLower();

        var requestPropIds = new List<Guid>();
        var requestTdns = new List<string>();
        if (landHoldingRequestId.HasValue && landHoldingRequestId.Value != Guid.Empty)
        {
            var lh = await _context.LandHoldingRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == landHoldingRequestId.Value);
            if (lh != null)
            {
                if (lh.TaxDeclarationId.HasValue) requestPropIds.Add(lh.TaxDeclarationId.Value);
                if (!string.IsNullOrWhiteSpace(lh.TaxDeclarationNumber))
                {
                    foreach (var part in lh.TaxDeclarationNumber.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (!requestTdns.Contains(part, StringComparer.OrdinalIgnoreCase))
                            requestTdns.Add(part);
                    }
                }
                if (!string.IsNullOrWhiteSpace(lh.SelectedPropertiesJson))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(lh.SelectedPropertiesJson);
                        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var el in doc.RootElement.EnumerateArray())
                            {
                                if ((el.TryGetProperty("id", out var idEl) || el.TryGetProperty("Id", out idEl)) &&
                                    Guid.TryParse(idEl.GetString(), out var gid) && !requestPropIds.Contains(gid))
                                {
                                    requestPropIds.Add(gid);
                                }
                                if (el.TryGetProperty("taxDeclarationNumber", out var tdnEl) || el.TryGetProperty("TaxDeclarationNumber", out tdnEl))
                                {
                                    var tv = tdnEl.GetString()?.Trim();
                                    if (!string.IsNullOrWhiteSpace(tv) && !requestTdns.Contains(tv, StringComparer.OrdinalIgnoreCase))
                                        requestTdns.Add(tv);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        if (owner == null && string.IsNullOrEmpty(targetName) && requestPropIds.Count == 0 && requestTdns.Count == 0)
        {
            return Json(Array.Empty<object>());
        }

        var allActiveTds = await _context.TaxDeclarations
            .AsNoTracking()
            .Include(t => t.LandDetails)
            .Include(t => t.BuildingDetails)
            .Include(t => t.MachineryDetails)
            .Where(t => !t.IsCancelledAndSuperseded && t.Status != AssessmentStatus.Cancelled)
            .OrderBy(t => t.TaxDeclarationNumber)
            .ToListAsync();

        var hasOwnerCriteria = owner != null || !string.IsNullOrEmpty(targetName);

        var props = allActiveTds
            .Where(t =>
                DoesNameMatchOwner(t.OwnerId, t.OwnerName, owner, ownerName) ||
                (requestPropIds.Count > 0 && requestPropIds.Contains(t.Id)) ||
                (requestPropIds.Count == 0 && !hasOwnerCriteria && requestTdns.Count > 0 && requestTdns.Contains(t.TaxDeclarationNumber, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        var result = props
            .GroupBy(p => !string.IsNullOrWhiteSpace(p.TaxDeclarationNumber)
                ? p.TaxDeclarationNumber.Trim().ToUpperInvariant()
                : p.Id.ToString())
            .Select(g => g
                .OrderByDescending(p => DoesNameMatchOwner(p.OwnerId, p.OwnerName, owner, ownerName))
                .ThenByDescending(p => requestPropIds.Contains(p.Id))
                .ThenByDescending(p => p.Status == AssessmentStatus.Active)
                .First())
            .Select(p => new
            {
                id = p.Id,
                taxDeclarationNumber = p.TaxDeclarationNumber,
                propertyIndexNumber = p.PropertyIndexNumber,
                kind = p.Kind.ToString(),
                classification = p.Classification.ToString(),
                location = p.CompleteAddress,
                marketValue = p.MarketValue,
                assessedValue = p.AssessedValue
            }).ToList();

        return Json(result);
    }

    private static bool DoesNameMatchOwner(Guid? tdOwnerId, string? tdOwnerName, Owner? owner, string? fallbackName)
    {
        var rawTdName = (tdOwnerName ?? string.Empty).Trim();

        if (owner != null)
        {
            if (tdOwnerId.HasValue && tdOwnerId.Value == owner.Id)
                return true;

            if (!string.IsNullOrEmpty(rawTdName))
            {
                if (rawTdName.Equals(owner.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;

                if (owner.OwnerType == OwnerType.Corporate || owner.OwnerType == OwnerType.Government)
                {
                    if (!string.IsNullOrWhiteSpace(owner.CorporateName) &&
                        rawTdName.Contains(owner.CorporateName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else if (!string.IsNullOrWhiteSpace(owner.FirstName) && !string.IsNullOrWhiteSpace(owner.LastName))
                {
                    if (rawTdName.Contains(owner.FirstName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        rawTdName.Contains(owner.LastName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }

        var target = (owner?.DisplayName ?? fallbackName ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(rawTdName))
        {
            if (rawTdName.Equals(target, StringComparison.OrdinalIgnoreCase))
                return true;

            var tokens = target
                .Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2)
                .ToList();

            if (tokens.Count >= 2 && tokens.All(tok => rawTdName.Contains(tok, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    private bool CanAccessReportOrHolding(int reportId)
    {
        if (_permissionService.CanAccessReport(User, reportId)) return true;
        if (reportId == 5 || reportId == 6)
        {
            return _permissionService.CanAccessReport(User, 5) || _permissionService.CanAccessReport(User, 6);
        }
        if (reportId == 2 || reportId == 4)
        {
            return _permissionService.CanAccessReport(User, 2) || _permissionService.CanAccessReport(User, 4);
        }
        return false;
    }

    private static bool IsLandHoldingReport(RPTReport report)
    {
        return report.RPTReportId == 5
            || report.RPTReportId == 6
            || report.Title.Contains("Holding", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImprovementReport(RPTReport report)
    {
        return report.RPTReportId == 2
            || report.RPTReportId == 4
            || report.Title.Contains("Improvement", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsActiveApprovedLand(TaxDeclaration t)
    {
        return t.Kind == PropertyKind.Land
            && !t.IsCancelledAndSuperseded
            && t.Status == AssessmentStatus.Active
            && !string.IsNullOrWhiteSpace(t.TaxDeclarationNumber)
            && !t.TaxDeclarationNumber.Trim().EndsWith("-00000");
    }

    private async Task<List<TaxDeclaration>> GetImprovementsForLandAsync(TaxDeclaration landTd)
    {
        var landTdn = (landTd.TaxDeclarationNumber ?? string.Empty).Trim();
        var landPin = (landTd.PropertyIndexNumber ?? string.Empty).Trim();
        var landLot = (landTd.LotNo ?? string.Empty).Trim();

        var candidates = await _context.TaxDeclarations
            .AsNoTracking()
            .Include(t => t.Owner)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.LandReferences)
            .Include(t => t.MachineryDetails)
            .Where(t => (t.Kind == PropertyKind.Building || t.Kind == PropertyKind.Machinery)
                     && !t.IsCancelledAndSuperseded
                     && t.Status == AssessmentStatus.Active)
            .OrderBy(t => t.TaxDeclarationNumber)
            .ToListAsync();

        static bool SameVal(string? a, string b) =>
            !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) &&
            string.Equals(a.Trim(), b, StringComparison.OrdinalIgnoreCase);

        return candidates
            .Where(t => !string.IsNullOrWhiteSpace(t.TaxDeclarationNumber) && !t.TaxDeclarationNumber.Trim().EndsWith("-00000"))
            .Where(t =>
            {
                if (t.Kind == PropertyKind.Building && t.BuildingDetails != null)
                {
                    var b = t.BuildingDetails;
                    if (SameVal(b.LandTdNo, landTdn) || SameVal(b.LandPin, landPin) || SameVal(b.LandLotNo, landLot))
                        return true;
                    if (b.LandReferences != null && b.LandReferences.Any(lr =>
                        SameVal(lr.LandTdNo, landTdn) || SameVal(lr.LandPin, landPin)))
                        return true;
                }
                if (t.Kind == PropertyKind.Machinery && t.MachineryDetails != null)
                {
                    var m = t.MachineryDetails;
                    if (SameVal(m.LandTdNo, landTdn) || SameVal(m.LandPin, landPin))
                        return true;
                }
                return false;
            })
            .GroupBy(t => !string.IsNullOrWhiteSpace(t.TaxDeclarationNumber)
                ? t.TaxDeclarationNumber.Trim().ToUpperInvariant()
                : t.Id.ToString())
            .Select(g => g.First())
            .OrderBy(t => t.TaxDeclarationNumber)
            .ToList();
    }

    private async Task<(RPTReport EffectiveReport, List<TaxDeclaration> RelatedProperties)>
        ResolveLandImprovementAsync(TaxDeclaration landTd, RPTReport requestedReport, string? selectedPropertyIdsOrJson = null)
    {
        List<TaxDeclaration> improvements;
        if (selectedPropertyIdsOrJson != null)
        {
            var selectedIds = ParsePropertyIds(selectedPropertyIdsOrJson);
            improvements = selectedIds.Count > 0
                ? await _context.TaxDeclarations
                    .AsNoTracking()
                    .Include(t => t.Owner)
                    .Include(t => t.BuildingDetails)
                    .Include(t => t.MachineryDetails)
                    .Where(t => selectedIds.Contains(t.Id))
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync()
                : new List<TaxDeclaration>();
        }
        else
        {
            improvements = await GetImprovementsForLandAsync(landTd);
        }

        var effectiveReport = requestedReport;
        if (improvements.Count > 0)
        {
            var withImp = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == 4);
            if (withImp != null) effectiveReport = withImp;
        }
        else
        {
            var noImp = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == 2);
            if (noImp != null) effectiveReport = noImp;
        }

        return (effectiveReport, improvements);
    }

    private static List<Guid> ParsePropertyIds(string? selectedPropertyIdsOrJson)
    {
        var selectedIds = new List<Guid>();
        if (string.IsNullOrWhiteSpace(selectedPropertyIdsOrJson)) return selectedIds;
        var trimmed = selectedPropertyIdsOrJson.Trim();
        if (trimmed.Equals("none", StringComparison.OrdinalIgnoreCase)) return selectedIds;
        if (trimmed.StartsWith("["))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(trimmed);
                if (parsed != null) selectedIds = parsed;
            }
            catch { }
        }
        else
        {
            foreach (var part in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var gid)) selectedIds.Add(gid);
            }
        }
        return selectedIds;
    }

    // GET: /RPTReport/SearchLandTdns?q=...&ownerId=...&ownerName=...
    [HttpGet]
    public async Task<IActionResult> SearchLandTdns(string? q = null, Guid? ownerId = null, string? ownerName = null)
    {
        var rawLands = await _context.TaxDeclarations
            .AsNoTracking()
            .Where(t => t.Kind == PropertyKind.Land
                     && !t.IsCancelledAndSuperseded
                     && t.Status == AssessmentStatus.Active)
            .OrderBy(t => t.TaxDeclarationNumber)
            .ToListAsync();

        var lands = rawLands
            .Where(t => !string.IsNullOrWhiteSpace(t.TaxDeclarationNumber) && !t.TaxDeclarationNumber.Trim().EndsWith("-00000"))
            .ToList();

        if (ownerId.HasValue && ownerId.Value != Guid.Empty)
        {
            lands = lands
                .Where(t => t.OwnerId.HasValue && t.OwnerId.Value == ownerId.Value)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(ownerName))
        {
            lands = lands
                .Where(t => DoesNameMatchOwner(t.OwnerId, t.OwnerName, null, ownerName))
                .ToList();
        }

        lands = lands
            .GroupBy(t => t.TaxDeclarationNumber.Trim().ToUpperInvariant())
            .Select(g => g.First())
            .ToList();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var tokens = q.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            lands = lands.Where(t =>
            {
                var hay = $"{t.TaxDeclarationNumber} {t.OwnerName} {t.LotNo} {t.PropertyIndexNumber} {t.DistrictOrBarangay} {t.Municipality}".ToLower();
                return tokens.All(tok => hay.Contains(tok));
            }).ToList();
        }

        var results = lands.Take(30).Select(t => new
        {
            id = t.TaxDeclarationNumber,
            tdn = t.TaxDeclarationNumber,
            ownerName = t.OwnerName,
            lotNo = t.LotNo,
            pin = t.PropertyIndexNumber,
            location = t.CompleteAddress
        }).ToList();

        return Json(results);
    }

    // GET: /RPTReport/GetLandImprovements?tdn=...
    [HttpGet]
    public async Task<IActionResult> GetLandImprovements(string tdn)
    {
        if (string.IsNullOrWhiteSpace(tdn))
        {
            return Json(new { valid = false, message = "Land Tax Declaration (TDN) No. is required.", properties = Array.Empty<object>() });
        }

        var cleanTdn = tdn.Trim();
        var landTd = await _context.TaxDeclarations
            .AsNoTracking()
            .Include(t => t.LandDetails)
            .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == cleanTdn ||
                                      t.TaxDeclarationNumber.Replace(" ", "").ToLower() == cleanTdn.Replace(" ", "").ToLower());

        if (landTd == null)
        {
            return Json(new { valid = false, message = $"Land TDN '{cleanTdn}' was not found in Assessment Records.", properties = Array.Empty<object>() });
        }

        if (!IsActiveApprovedLand(landTd))
        {
            return Json(new { valid = false, message = $"TDN '{landTd.TaxDeclarationNumber}' must be an Active & Approved Land Tax Declaration (found {landTd.Kind}, Status: {landTd.Status}).", properties = Array.Empty<object>() });
        }

        var improvements = await GetImprovementsForLandAsync(landTd);
        var propDtos = improvements.Select(p => new
        {
            id = p.Id,
            taxDeclarationNumber = p.TaxDeclarationNumber,
            kind = p.Kind.ToString(),
            classification = p.Classification.ToString(),
            location = p.CompleteAddress,
            marketValue = p.MarketValue,
            assessedValue = p.AssessedValue
        }).ToList();

        return Json(new
        {
            valid = true,
            landTdn = landTd.TaxDeclarationNumber,
            ownerName = landTd.OwnerName,
            lotNo = landTd.LotNo,
            properties = propDtos
        });
    }

    private async Task<(RPTReport EffectiveReport, TaxDeclaration PrimaryTd, List<TaxDeclaration> RelatedProperties, Guid? OwnerId, string OwnerName)>
        ResolveOwnerHoldingAsync(Guid? ownerId, string? ownerName, RPTReport requestedReport, string? selectedPropertyIdsOrJson = null)
    {
        Owner? owner = null;
        if (ownerId.HasValue)
        {
            owner = await _context.TaxpayerOwners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == ownerId.Value);
        }

        var resolvedName = (owner?.DisplayName ?? ownerName ?? string.Empty).Trim();
        var resolvedAddress = owner?.FullAddress ?? string.Empty;
        var resolvedNameLower = resolvedName.ToLower();

        List<TaxDeclaration> ownerProperties;
        if (selectedPropertyIdsOrJson != null)
        {
            var selectedIds = new List<Guid>();
            var trimmed = selectedPropertyIdsOrJson.Trim();
            if (trimmed.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                // Explicitly empty
            }
            else if (trimmed.StartsWith("["))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(trimmed);
                    if (parsed != null) selectedIds = parsed;
                }
                catch { }
            }
            else
            {
                foreach (var part in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (Guid.TryParse(part, out var gid)) selectedIds.Add(gid);
                }
            }

            ownerProperties = selectedIds.Count > 0
                ? await _context.TaxDeclarations
                    .AsNoTracking()
                    .Include(t => t.Owner)
                    .Include(t => t.LandDetails)
                    .Include(t => t.BuildingDetails)
                    .Include(t => t.MachineryDetails)
                    .Where(t => selectedIds.Contains(t.Id))
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync()
                : new List<TaxDeclaration>();
        }
        else
        {
            ownerProperties = await _context.TaxDeclarations
                .AsNoTracking()
                .Include(t => t.Owner)
                .Include(t => t.LandDetails)
                .Include(t => t.BuildingDetails)
                .Include(t => t.MachineryDetails)
                .Where(t => !t.IsCancelledAndSuperseded && t.Status != AssessmentStatus.Cancelled)
                .Where(t => (owner != null && t.OwnerId == owner.Id) ||
                            (!string.IsNullOrEmpty(resolvedNameLower) && t.OwnerName != null && t.OwnerName.Trim().ToLower() == resolvedNameLower))
                .OrderBy(t => t.TaxDeclarationNumber)
                .ToListAsync();
        }

        RPTReport effectiveReport = requestedReport;
        if (ownerProperties.Count > 0)
        {
            // Owner has registered properties -> All Property Holding Certificate (#6)
            var allHolding = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == 6);
            if (allHolding != null) effectiveReport = allHolding;

            return (effectiveReport, ownerProperties[0], ownerProperties, owner?.Id ?? ownerId, resolvedName);
        }
        else
        {
            // Owner has no registered properties -> Certificate of No Land Holding (#5)
            var noHolding = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == 5);
            if (noHolding != null) effectiveReport = noHolding;

            var placeholderTd = new TaxDeclaration
            {
                OwnerId = owner?.Id ?? ownerId,
                OwnerName = resolvedName,
                OwnerAddress = resolvedAddress,
                OwnerTin = owner?.TaxIdentNo ?? string.Empty,
                StreetNo = resolvedAddress
            };

            return (effectiveReport, placeholderTd, new List<TaxDeclaration>(), owner?.Id ?? ownerId, resolvedName);
        }
    }

    private async Task<List<TaxDeclaration>> LoadRelatedPropertiesAsync(RPTReport report, TaxDeclaration taxDeclaration)
    {
        if (report.Title.Contains("BIR", StringComparison.OrdinalIgnoreCase) ||
            report.Title.Contains("DAR", StringComparison.OrdinalIgnoreCase) ||
            report.Title.Contains("Holding", StringComparison.OrdinalIgnoreCase))
        {
            if (taxDeclaration.OwnerId.HasValue)
            {
                return await _context.TaxDeclarations
                    .Where(t => t.OwnerId == taxDeclaration.OwnerId.Value)
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync();
            }
            if (!string.IsNullOrWhiteSpace(taxDeclaration.OwnerName))
            {
                return await _context.TaxDeclarations
                    .Where(t => t.OwnerName == taxDeclaration.OwnerName)
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync();
            }
        }
        else if (report.RPTReportId == 4 || (report.Title.Contains("Certificate of Improvement", StringComparison.OrdinalIgnoreCase) && !report.Title.Contains("NO Improvement", StringComparison.OrdinalIgnoreCase)))
        {
            var cleanTdnVal = taxDeclaration.TaxDeclarationNumber;
            return await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Where(t => t.Kind == PropertyKind.Building &&
                            ((t.BuildingDetails != null && t.BuildingDetails.LandTdNo == cleanTdnVal) ||
                             (taxDeclaration.LotNo != null && t.BuildingDetails != null && t.BuildingDetails.LandLotNo == taxDeclaration.LotNo)))
                .OrderBy(t => t.TaxDeclarationNumber)
                .ToListAsync();
        }

        return new List<TaxDeclaration>();
    }

    // GET: /RPTReport/ValidateTdn?tdn=...
    // Live validation API checking whether a TDN exists in the Assessment records
    [HttpGet]
    public async Task<IActionResult> ValidateTdn(string tdn)
    {
        if (string.IsNullOrWhiteSpace(tdn))
        {
            return Json(new { exists = false, message = "Tax Declaration (TDN) No. is required." });
        }

        var cleanTdn = tdn.Trim();
        var record = await _context.TaxDeclarations
            .Where(t => t.TaxDeclarationNumber == cleanTdn ||
                        t.TaxDeclarationNumber.Replace(" ", "").ToLower() == cleanTdn.Replace(" ", "").ToLower())
            .Select(t => new
            {
                t.TaxDeclarationNumber,
                t.OwnerName,
                t.PropertyIndexNumber,
                Kind = t.Kind.ToString()
            })
            .FirstOrDefaultAsync();

        if (record == null)
        {
            return Json(new
            {
                exists = false,
                message = $"Tax Declaration (TDN) No. '{cleanTdn}' does not exist in the list of Assessment Records."
            });
        }

        return Json(new
        {
            exists = true,
            message = $"Verified in Assessment Records: {record.OwnerName} ({record.Kind} - PIN: {record.PropertyIndexNumber})",
            data = record
        });
    }

    // Helper: Retrieves list of SelectListItem containing only reports current user has permission to access
    private async Task<List<SelectListItem>> GetAccessibleReportsForUserAsync()
    {
        var allReports = await _context.RPTReports.OrderBy(r => r.RPTReportId).ToListAsync();
        var selectList = new List<SelectListItem>();
        bool addedCombinedHolding = false;
        bool addedCombinedImprovement = false;

        foreach (var report in allReports)
        {
            if (report.RPTReportId == 5 || report.RPTReportId == 6)
            {
                if (!addedCombinedHolding && (_permissionService.CanAccessReport(User, 5) || _permissionService.CanAccessReport(User, 6)))
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = "6",
                        Text = "Certificate of Property Holdings"
                    });
                    addedCombinedHolding = true;
                }
                continue;
            }

            if (report.RPTReportId == 2 || report.RPTReportId == 4)
            {
                if (!addedCombinedImprovement && (_permissionService.CanAccessReport(User, 2) || _permissionService.CanAccessReport(User, 4)))
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = "4",
                        Text = "CERTIFICATION W/ NO IMPROVEMENT"
                    });
                    addedCombinedImprovement = true;
                }
                continue;
            }

            if (_permissionService.CanAccessReport(User, report.RPTReportId))
            {
                selectList.Add(new SelectListItem
                {
                    Value = report.RPTReportId.ToString(),
                    Text = $"{report.Title} (#{report.RPTReportId})"
                });
            }
        }

        return selectList;
    }
}
