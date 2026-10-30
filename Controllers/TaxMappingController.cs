using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class TaxMappingController : Controller
{
    private readonly ITaxMappingService _taxMapService;
    private readonly IGeographicService _geoService;
    private readonly IOwnerService _ownerService;
    private readonly IAssessmentService _assessmentService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly IWebHostEnvironment _env;
    private readonly IWorkflowNotifier _workflowNotifier;

    public TaxMappingController(
        ITaxMappingService taxMapService,
        IGeographicService geoService,
        IOwnerService ownerService,
        IAssessmentService assessmentService,
        IOrderPaymentService orderPaymentService,
        IWebHostEnvironment env,
        IWorkflowNotifier workflowNotifier)
    {
        _taxMapService = taxMapService;
        _geoService = geoService;
        _ownerService = ownerService;
        _assessmentService = assessmentService;
        _orderPaymentService = orderPaymentService;
        _env = env;
        _workflowNotifier = workflowNotifier;
    }

    private void PopulateGeographicEntities(TaxMappingRequest model)
    {
        if (model == null) return;

        if (model.ProvinceId.HasValue)
        {
            model.Province = _geoService.GetProvinceById(model.ProvinceId.Value);
            if (model.Province != null) model.ProvinceName = model.Province.Name;
        }

        if (model.CityMunicipalityId.HasValue)
        {
            model.CityMunicipality = _geoService.GetCityById(model.CityMunicipalityId.Value);
            if (model.CityMunicipality != null) model.CityMunicipalityName = model.CityMunicipality.Name;
        }

        if (model.BarangayId.HasValue)
        {
            model.Barangay = _geoService.GetBarangayById(model.BarangayId.Value);
            if (model.Barangay != null) model.BarangayName = model.Barangay.Name;
        }
    }

    // AJAX Endpoint: Fetch property details for auto-filling Tax Mapping fields
    [HttpGet]
    public IActionResult GetPropertyDetails(Guid id)
    {
        var prop = _assessmentService.GetById(id);
        if (prop == null) return NotFound();

        return Json(new
        {
            id = prop.Id,
            taxDeclarationNumber = prop.TaxDeclarationNumber,
            propertyIndexNumber = prop.PropertyIndexNumber,
            ownerName = prop.OwnerName,
            ownerPhone = prop.OwnerPhone,
            ownerEmail = prop.OwnerEmail,
            ownerAddress = prop.OwnerAddress,
            streetAddress = prop.StreetNo,
            districtOrBarangay = prop.DistrictOrBarangay,
            municipality = prop.Municipality,
            provinceCity = prop.ProvinceCity,
            areaSqMeters = prop.AreaSqMeters
        });
    }

    // GET: /TaxMapping
    public IActionResult Index(string? search, RequestStatus? status)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;

        ViewBag.TotalCount = _taxMapService.GetTotalCount();
        ViewBag.PendingCount = _taxMapService.GetPendingCount();
        ViewBag.ApprovedCount = _taxMapService.GetApprovedCount();

        var records = _taxMapService.GetAll(search, status).ToList();
        foreach (var req in records)
        {
            PopulateGeographicEntities(req);
        }
        return View(records);
    }

    // GET: /TaxMapping/Details/{guid}
    public IActionResult Details(Guid id)
    {
        var item = _taxMapService.GetById(id);
        if (item == null)
        {
            return NotFound();
        }
        PopulateGeographicEntities(item);
        return View(item);
    }

    // GET: /TaxMapping/Create
    public IActionResult Create()
    {
        var provinces = _geoService.GetProvinces().ToList();
        var defaultProv = provinces.FirstOrDefault();
        var cities = defaultProv != null ? _geoService.GetCitiesByProvince(defaultProv.Id).ToList() : new List<CityMunicipality>();
        var defaultCity = cities.FirstOrDefault();
        var barangays = defaultCity != null ? _geoService.GetBarangaysByCity(defaultCity.Id).ToList() : new List<Barangay>();

        ViewBag.Provinces = provinces;
        ViewBag.Cities = cities;
        ViewBag.Barangays = barangays;
        ViewBag.Owners = _ownerService.GetAll().ToList();
        ViewBag.Properties = _assessmentService.GetAll().ToList();
        ViewBag.OrderPayments = _orderPaymentService.GetAll(isActive: true).ToList();

        var model = new TaxMappingRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = $"TMR-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
            ProvinceId = defaultProv?.Id,
            CityMunicipalityId = defaultCity?.Id,
            BarangayId = barangays.FirstOrDefault()?.Id,
            Status = RequestStatus.Pending
        };
        PopulateGeographicEntities(model);
        return View(model);
    }

    // POST: /TaxMapping/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaxMappingRequest model, IFormFile? fileUpload)
    {
        ModelState.Remove(nameof(TaxMappingRequest.Province));
        ModelState.Remove(nameof(TaxMappingRequest.CityMunicipality));
        ModelState.Remove(nameof(TaxMappingRequest.Barangay));

        PopulateGeographicEntities(model);

        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _taxMapService.Add(model, fileUpload, webRoot);
                await _workflowNotifier.NotifyTransactionCreatedAsync(model.Id, "TaxMappingRequest", model.RequestNumber, 2, model.ApplicantName);
                TempData["SuccessMessage"] = $"Tax Mapping Request '{model.RequestNumber}' for '{model.ApplicantName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error saving tax mapping request: {ex.Message}");
            }
        }

        TempData["ErrorMessage"] = "Failed to submit Tax Mapping Request. Please check validation errors.";
        ViewBag.Provinces = _geoService.GetProvinces();
        ViewBag.Cities = model.ProvinceId.HasValue ? _geoService.GetCitiesByProvince(model.ProvinceId.Value) : new List<CityMunicipality>();
        ViewBag.Barangays = model.CityMunicipalityId.HasValue ? _geoService.GetBarangaysByCity(model.CityMunicipalityId.Value) : new List<Barangay>();
        ViewBag.Owners = _ownerService.GetAll().ToList();
        ViewBag.Properties = _assessmentService.GetAll().ToList();
        ViewBag.OrderPayments = _orderPaymentService.GetAll(isActive: true).ToList();

        return View(model);
    }

    // GET: /TaxMapping/Edit/{guid}
    public IActionResult Edit(Guid id)
    {
        var item = _taxMapService.GetById(id);
        if (item == null)
        {
            return NotFound();
        }
        PopulateGeographicEntities(item);

        var provinces = _geoService.GetProvinces().ToList();
        var selectedProvId = item.ProvinceId ?? provinces.FirstOrDefault()?.Id ?? Guid.Empty;
        var cities = _geoService.GetCitiesByProvince(selectedProvId).ToList();
        var selectedCityId = item.CityMunicipalityId ?? cities.FirstOrDefault()?.Id ?? Guid.Empty;
        var barangays = _geoService.GetBarangaysByCity(selectedCityId).ToList();

        ViewBag.Provinces = provinces;
        ViewBag.Cities = cities;
        ViewBag.Barangays = barangays;
        ViewBag.Owners = _ownerService.GetAll().ToList();
        ViewBag.Properties = _assessmentService.GetAll().ToList();
        ViewBag.OrderPayments = _orderPaymentService.GetAll(isActive: true).ToList();

        return View(item);
    }

    // POST: /TaxMapping/Edit/{guid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, TaxMappingRequest model, IFormFile? fileUpload)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        ModelState.Remove(nameof(TaxMappingRequest.Province));
        ModelState.Remove(nameof(TaxMappingRequest.CityMunicipality));
        ModelState.Remove(nameof(TaxMappingRequest.Barangay));

        PopulateGeographicEntities(model);

        if (ModelState.IsValid)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
                _taxMapService.Update(model, fileUpload, webRoot);
                TempData["SuccessMessage"] = $"Tax Mapping Request '{model.RequestNumber}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating tax mapping request: {ex.Message}");
            }
        }

        TempData["ErrorMessage"] = "Failed to update Tax Mapping Request. Please check validation errors.";
        ViewBag.Provinces = _geoService.GetProvinces();
        ViewBag.Cities = model.ProvinceId.HasValue ? _geoService.GetCitiesByProvince(model.ProvinceId.Value) : new List<CityMunicipality>();
        ViewBag.Barangays = model.CityMunicipalityId.HasValue ? _geoService.GetBarangaysByCity(model.CityMunicipalityId.Value) : new List<Barangay>();
        ViewBag.Owners = _ownerService.GetAll().ToList();
        ViewBag.Properties = _assessmentService.GetAll().ToList();
        ViewBag.OrderPayments = _orderPaymentService.GetAll(isActive: true).ToList();

        return View(model);
    }

    // POST: /TaxMapping/Delete/{guid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        var item = _taxMapService.GetById(id);
        if (item != null)
        {
            _taxMapService.Delete(id);
            TempData["SuccessMessage"] = $"Tax Mapping Request '{item.RequestNumber}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /TaxMapping/DownloadAttachment/{guid}
    public IActionResult DownloadAttachment(Guid id)
    {
        var item = _taxMapService.GetById(id);
        if (item == null || string.IsNullOrWhiteSpace(item.PhysicalStoragePath) || !System.IO.File.Exists(item.PhysicalStoragePath))
        {
            TempData["ErrorMessage"] = "Attachment file not found at the configured physical storage location.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var memory = new MemoryStream();
        using (var stream = new FileStream(item.PhysicalStoragePath, FileMode.Open, FileAccess.Read))
        {
            stream.CopyTo(memory);
        }
        memory.Position = 0;

        var fileName = item.AttachmentFileName ?? "taxmap_attachment.pdf";
        return File(memory, "application/octet-stream", fileName);
    }
}
