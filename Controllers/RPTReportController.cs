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
    public async Task<IActionResult> Index(string? tdn = null, int? reportId = null)
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
            RPTReportId = reportId,
            AccessibleReports = accessibleReports,
            RequestedBy = User.Identity?.Name ?? string.Empty
        };

        ViewBag.TotalAccessibleReports = accessibleReports.Count;
        ViewBag.CanManagePermissions = _permissionService.CanAccess(User, "UserRole", "Permissions");

        return View(model);
    }

    // POST: /RPTReport/Generate
    // Validates TDN No. and Report Type (verifying user permission) and renders the official certificate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateRPTReportViewModel model)
    {
        model.AccessibleReports = await GetAccessibleReportsForUserAsync();

        // 1. Validate TDN Input
        if (string.IsNullOrWhiteSpace(model.TaxDeclarationNumber))
        {
            ModelState.AddModelError(nameof(model.TaxDeclarationNumber), "Tax Declaration (TDN) No. is required.");
        }

        // 2. Validate Report Type Selection
        if (!model.RPTReportId.HasValue || model.RPTReportId.Value <= 0)
        {
            ModelState.AddModelError(nameof(model.RPTReportId), "Please select a Report / Certification Type.");
        }
        else
        {
            // Verify that current user's role has permission to access this specific report
            bool hasAccess = _permissionService.CanAccessReport(User, model.RPTReportId.Value);
            if (!hasAccess)
            {
                ModelState.AddModelError(nameof(model.RPTReportId), "Access Denied: Your account role is not authorized to generate this specific report type.");
            }
        }

        // 3. Specifically validate if TDN exists in the list of Assessment records
        TaxDeclaration? taxDeclaration = null;
        if (!string.IsNullOrWhiteSpace(model.TaxDeclarationNumber))
        {
            var cleanTdn = model.TaxDeclarationNumber.Trim();
            taxDeclaration = await _context.TaxDeclarations
                .Include(t => t.Owner)
                .Include(t => t.LandDetails)
                .Include(t => t.BuildingDetails)
                .Include(t => t.MachineryDetails)
                .FirstOrDefaultAsync(t => t.TaxDeclarationNumber == cleanTdn ||
                                          t.TaxDeclarationNumber.Replace(" ", "").ToLower() == cleanTdn.Replace(" ", "").ToLower());

            if (taxDeclaration == null)
            {
                ModelState.AddModelError(nameof(model.TaxDeclarationNumber), $"Tax Declaration (TDN) No. '{model.TaxDeclarationNumber}' does not exist in the list of Assessment Records.");
            }
        }

        if (!ModelState.IsValid || taxDeclaration == null)
        {
            ViewBag.TotalAccessibleReports = model.AccessibleReports.Count;
            ViewBag.CanManagePermissions = _permissionService.CanAccess(User, "UserRole", "Permissions");
            return View("Index", model);
        }

        // 4. Retrieve Report Metadata
        var report = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == model.RPTReportId!.Value);
        if (report == null)
        {
            ModelState.AddModelError(nameof(model.RPTReportId), "The selected report configuration does not exist.");
            return View("Index", model);
        }

        // 5. If multi-holding or agricultural report, look up all properties for this owner
        var relatedProperties = new List<TaxDeclaration>();
        if (report.Title.Contains("BIR", StringComparison.OrdinalIgnoreCase) ||
            report.Title.Contains("DAR", StringComparison.OrdinalIgnoreCase) ||
            report.Title.Contains("Holding", StringComparison.OrdinalIgnoreCase))
        {
            if (taxDeclaration.OwnerId.HasValue)
            {
                relatedProperties = await _context.TaxDeclarations
                    .Where(t => t.OwnerId == taxDeclaration.OwnerId.Value)
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync();
            }
            else if (!string.IsNullOrWhiteSpace(taxDeclaration.OwnerName))
            {
                relatedProperties = await _context.TaxDeclarations
                    .Where(t => t.OwnerName == taxDeclaration.OwnerName)
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync();
            }
        }
        else if (report.RPTReportId == 4 || (report.Title.Contains("Certificate of Improvement", StringComparison.OrdinalIgnoreCase) && !report.Title.Contains("NO Improvement", StringComparison.OrdinalIgnoreCase)))
        {
            var cleanTdnVal = taxDeclaration.TaxDeclarationNumber;
            relatedProperties = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Where(t => t.Kind == PropertyKind.Building &&
                            ((t.BuildingDetails != null && t.BuildingDetails.LandTdNo == cleanTdnVal) ||
                             (taxDeclaration.LotNo != null && t.BuildingDetails != null && t.BuildingDetails.LandLotNo == taxDeclaration.LotNo)))
                .OrderBy(t => t.TaxDeclarationNumber)
                .ToListAsync();
        }

        // 6. Build the standardized certificate model using the common helper
        var certificateModel = ReportPrintHelper.BuildCertificateModel(
            taxDeclaration,
            report,
            model.RequestedBy,
            model.Purpose,
            model.ORNumber,
            1470.00m,
            relatedProperties);

        return View("ReportDetails", certificateModel);
    }

    // GET: /RPTReport/Print?tdn=...&reportId=...
    // Clean standalone printout page without navigation chrome for pure paper/PDF output
    [HttpGet]
    public async Task<IActionResult> Print(string tdn, int reportId, string? requestor = null, string? purpose = null, string? orNumber = null)
    {
        if (string.IsNullOrWhiteSpace(tdn) || reportId <= 0)
        {
            return BadRequest("TDN and Report ID are required.");
        }

        bool hasAccess = _permissionService.CanAccessReport(User, reportId);
        if (!hasAccess)
        {
            return Forbid();
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

        var report = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == reportId);
        if (report == null)
        {
            return NotFound($"Report ID '{reportId}' not found.");
        }

        var relatedProperties = new List<TaxDeclaration>();
        if (report.Title.Contains("BIR", StringComparison.OrdinalIgnoreCase) ||
            report.Title.Contains("DAR", StringComparison.OrdinalIgnoreCase) ||
            report.Title.Contains("Holding", StringComparison.OrdinalIgnoreCase))
        {
            if (taxDeclaration.OwnerId.HasValue)
            {
                relatedProperties = await _context.TaxDeclarations
                    .Where(t => t.OwnerId == taxDeclaration.OwnerId.Value)
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync();
            }
            else if (!string.IsNullOrWhiteSpace(taxDeclaration.OwnerName))
            {
                relatedProperties = await _context.TaxDeclarations
                    .Where(t => t.OwnerName == taxDeclaration.OwnerName)
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync();
            }
        }
        else if (report.RPTReportId == 4 || (report.Title.Contains("Certificate of Improvement", StringComparison.OrdinalIgnoreCase) && !report.Title.Contains("NO Improvement", StringComparison.OrdinalIgnoreCase)))
        {
            var cleanTdnVal = taxDeclaration.TaxDeclarationNumber;
            relatedProperties = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Where(t => t.Kind == PropertyKind.Building &&
                            ((t.BuildingDetails != null && t.BuildingDetails.LandTdNo == cleanTdnVal) ||
                             (taxDeclaration.LotNo != null && t.BuildingDetails != null && t.BuildingDetails.LandLotNo == taxDeclaration.LotNo)))
                .OrderBy(t => t.TaxDeclarationNumber)
                .ToListAsync();
        }

        var certificateModel = ReportPrintHelper.BuildCertificateModel(
            taxDeclaration,
            report,
            requestor,
            purpose,
            orNumber,
            1470.00m,
            relatedProperties);

        return View("Print", certificateModel);
    }

    // GET: /RPTReport/PrintPdf?tdn=...&reportId=...&download=false
    // Renders the official QuestPDF certificate as a high-resolution PDF stream matching the physical sample
    [HttpGet]
    public async Task<IActionResult> PrintPdf(string tdn, int reportId, string? requestor = null, string? purpose = null, string? orNumber = null, bool download = false)
    {
        if (string.IsNullOrWhiteSpace(tdn) || reportId <= 0)
        {
            return BadRequest("Tax Declaration Number (TDN) and Report ID are required.");
        }

        bool hasAccess = _permissionService.CanAccessReport(User, reportId);
        if (!hasAccess)
        {
            return Forbid();
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

        var report = await _context.RPTReports.FirstOrDefaultAsync(r => r.RPTReportId == reportId);
        if (report == null)
        {
            return NotFound($"Report ID '{reportId}' not found.");
        }

        var relatedProperties = new List<TaxDeclaration>();
        if (report.Title.Contains("BIR", StringComparison.OrdinalIgnoreCase) ||
            report.Title.Contains("DAR", StringComparison.OrdinalIgnoreCase) ||
            report.Title.Contains("Holding", StringComparison.OrdinalIgnoreCase))
        {
            if (taxDeclaration.OwnerId.HasValue)
            {
                relatedProperties = await _context.TaxDeclarations
                    .Where(t => t.OwnerId == taxDeclaration.OwnerId.Value)
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync();
            }
            else if (!string.IsNullOrWhiteSpace(taxDeclaration.OwnerName))
            {
                relatedProperties = await _context.TaxDeclarations
                    .Where(t => t.OwnerName == taxDeclaration.OwnerName)
                    .OrderBy(t => t.TaxDeclarationNumber)
                    .ToListAsync();
            }
        }
        else if (report.RPTReportId == 4 || (report.Title.Contains("Certificate of Improvement", StringComparison.OrdinalIgnoreCase) && !report.Title.Contains("NO Improvement", StringComparison.OrdinalIgnoreCase)))
        {
            var cleanTdnVal = taxDeclaration.TaxDeclarationNumber;
            relatedProperties = await _context.TaxDeclarations
                .Include(t => t.BuildingDetails)
                .Where(t => t.Kind == PropertyKind.Building &&
                            ((t.BuildingDetails != null && t.BuildingDetails.LandTdNo == cleanTdnVal) ||
                             (taxDeclaration.LotNo != null && t.BuildingDetails != null && t.BuildingDetails.LandLotNo == taxDeclaration.LotNo)))
                .OrderBy(t => t.TaxDeclarationNumber)
                .ToListAsync();
        }

        var certificateModel = ReportPrintHelper.BuildCertificateModel(
            taxDeclaration,
            report,
            requestor,
            purpose,
            orNumber,
            1470.00m,
            relatedProperties);

        var pdfBytes = _certPdfService.GenerateCertificatePdf(certificateModel);
        var safeReportTitle = string.Concat(report.Title.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        var safeTdn = string.Concat(cleanTdn.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        var fileName = $"{safeReportTitle}_{safeTdn}.pdf";

        if (download)
        {
            return File(pdfBytes, "application/pdf", fileName);
        }

        return File(pdfBytes, "application/pdf");
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

        foreach (var report in allReports)
        {
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
