using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.DTOs;
using PropertyAssessmentApp.Mappers;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

[Authorize]
public class DataTransferUtilityController : Controller
{
    private readonly IDataTransferService _dataTransferService;
    private readonly IOwnerService _ownerService;

    public DataTransferUtilityController(
        IDataTransferService dataTransferService,
        IOwnerService ownerService)
    {
        _dataTransferService = dataTransferService;
        _ownerService = ownerService;
    }

    // GET: /DataTransferUtility
    public IActionResult Index()
    {
        return View();
    }

    // GET: /DataTransferUtility/LoadTable
    public async Task<IActionResult> LoadTable(string filter = "TDN", string keyword = "", int pageNumber = 1, int pageSize = 10)
    {
        var (items, totalRecords) = await _dataTransferService.LoadTableAsync(filter, keyword, pageNumber, pageSize);

        ViewBag.CurrentPage = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.Filter = filter;
        ViewBag.Keyword = keyword;

        return PartialView("_TableDataPartial", items);
    }

    // POST: /DataTransferUtility/GetOwnerModal
    [HttpPost]
    public IActionResult GetOwnerModal([FromBody] TDNDTO tdnDto)
    {
        if (tdnDto == null)
        {
            return BadRequest("Invalid TDN data provided.");
        }

        var (firstName, middleName, lastName) = DataTransferMapper.ParseDisplayName(tdnDto.DisplayName);

        var prefilledOwner = new Owner
        {
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            CorporateName = tdnDto.DisplayName,
            TaxIdentNo = tdnDto.RPTIN.HasValue ? $"TIN-{tdnDto.RPTIN}" : "000-000-000-000",
            RptinNumber = tdnDto.RPTIN.HasValue ? $"RPTIN-{tdnDto.RPTIN}" : $"RPTIN-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
            Address = !string.IsNullOrWhiteSpace(tdnDto.OwnerAddress) ? tdnDto.OwnerAddress : (!string.IsNullOrWhiteSpace(tdnDto.Location) ? tdnDto.Location : "General Santos City"),
            ContactNo = tdnDto.PropAdminContactNo ?? "0917-000-0000",
            Email = "taxpayer@gensan.gov.ph"
        };

        ViewBag.ExistingOwners = _ownerService.GetAll();
        ViewBag.TdnJson = JsonSerializer.Serialize(tdnDto);

        return PartialView("_OwnerMapModalPartial", prefilledOwner);
    }

    public class SaveTransferRequest
    {
        public string TdnJson { get; set; } = string.Empty;
        public Guid? SelectedOwnerId { get; set; }
        public Owner? NewOwner { get; set; }
    }

    // POST: /DataTransferUtility/SaveTransfer
    [HttpPost]
    public async Task<IActionResult> SaveTransfer([FromBody] SaveTransferRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TdnJson))
        {
            return Json(new { success = false, message = "No TDN payload received." });
        }

        try
        {
            var tdnDto = JsonSerializer.Deserialize<TDNDTO>(request.TdnJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (tdnDto == null)
            {
                return Json(new { success = false, message = "Could not parse TDN record payload." });
            }

            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value 
                        ?? User.Identity?.Name 
                        ?? "Assessor Staff";
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                        ?? "Assessment Staff";

            var (success, message, tdId) = await _dataTransferService.TransferAndSaveAsync(
                tdnDto, 
                request.SelectedOwnerId, 
                request.NewOwner, 
                userName, 
                userRole);

            return Json(new
            {
                success = success,
                message = message,
                tdId = tdId
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Transfer failed: " + ex.Message });
        }
    }

    // GET: /DataTransferUtility/MigrationLogs
    [HttpGet]
    public async Task<IActionResult> MigrationLogs()
    {
        var logs = await _dataTransferService.GetMigrationLogsAsync(100);
        return PartialView("_MigrationLogsModalPartial", logs);
    }
}
