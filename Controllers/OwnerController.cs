using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class OwnerController : Controller
{
    private readonly IOwnerService _ownerService;
    private readonly IGeographicService _geoService;

    public OwnerController(IOwnerService ownerService, IGeographicService geoService)
    {
        _ownerService = ownerService;
        _geoService = geoService;
    }

    private void PopulateGeographicEntities(Owner owner)
    {
        if (owner == null) return;

        if (owner.ProvinceId.HasValue)
        {
            owner.Province = _geoService.GetProvinceById(owner.ProvinceId.Value);
            if (owner.Province != null) owner.ProvinceName = owner.Province.Name;
        }

        if (owner.CityMunicipalityId.HasValue)
        {
            owner.CityMunicipality = _geoService.GetCityById(owner.CityMunicipalityId.Value);
            if (owner.CityMunicipality != null) owner.CityMunicipalityName = owner.CityMunicipality.Name;
        }

        if (owner.BarangayId.HasValue)
        {
            owner.Barangay = _geoService.GetBarangayById(owner.BarangayId.Value);
            if (owner.Barangay != null) owner.BarangayName = owner.Barangay.Name;
        }
    }

    private void ValidateOwnerTypeFields(Owner model)
    {
        ModelState.Remove(nameof(Owner.Province));
        ModelState.Remove(nameof(Owner.CityMunicipality));
        ModelState.Remove(nameof(Owner.Barangay));

        if (model.OwnerType == OwnerType.Individual || model.OwnerType == OwnerType.Estate)
        {
            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError(nameof(Owner.LastName), "Last Name is required for individual taxpayers.");
            }
            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                ModelState.AddModelError(nameof(Owner.FirstName), "First Name is required for individual taxpayers.");
            }
        }
        else if (model.OwnerType == OwnerType.Corporate || model.OwnerType == OwnerType.Government)
        {
            if (string.IsNullOrWhiteSpace(model.CorporateName))
            {
                ModelState.AddModelError(nameof(Owner.CorporateName), "Company / Corporate Name is required.");
            }
        }
    }

    // GET: /Owner
    public IActionResult Index(string? search, OwnerType? type, bool? isActive)
    {
        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.IsActive = isActive;

        ViewBag.TotalCount = _ownerService.GetTotalCount();
        ViewBag.IndividualCount = _ownerService.GetIndividualCount();
        ViewBag.CorporateCount = _ownerService.GetCorporateCount();

        var records = _ownerService.GetAll(search, type, isActive).ToList();
        foreach (var owner in records)
        {
            PopulateGeographicEntities(owner);
        }
        return View(records);
    }

    // GET: /Owner/Details/{guid}
    public IActionResult Details(Guid id)
    {
        var owner = _ownerService.GetById(id);
        if (owner == null)
        {
            return NotFound();
        }
        PopulateGeographicEntities(owner);
        return View(owner);
    }

    // GET: /Owner/Create
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

        var model = new Owner
        {
            Id = Guid.NewGuid(),
            RptinNumber = $"RPTIN-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
            ProvinceId = defaultProv?.Id,
            CityMunicipalityId = defaultCity?.Id,
            BarangayId = barangays.FirstOrDefault()?.Id,
            IsActive = true
        };
        PopulateGeographicEntities(model);
        return View(model);
    }

    // POST: /Owner/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Owner model)
    {
        ValidateOwnerTypeFields(model);
        PopulateGeographicEntities(model);

        if (ModelState.IsValid)
        {
            _ownerService.Add(model);
            TempData["SuccessMessage"] = $"Owner '{model.DisplayName}' (RPTIN: {model.RptinNumber}) registered successfully!";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Failed to register owner. Please fix validation errors below.";
        ViewBag.Provinces = _geoService.GetProvinces();
        ViewBag.Cities = model.ProvinceId.HasValue ? _geoService.GetCitiesByProvince(model.ProvinceId.Value) : new List<CityMunicipality>();
        ViewBag.Barangays = model.CityMunicipalityId.HasValue ? _geoService.GetBarangaysByCity(model.CityMunicipalityId.Value) : new List<Barangay>();

        return View(model);
    }

    // GET: /Owner/Edit/{guid}
    public IActionResult Edit(Guid id)
    {
        var owner = _ownerService.GetById(id);
        if (owner == null)
        {
            return NotFound();
        }
        PopulateGeographicEntities(owner);

        var provinces = _geoService.GetProvinces().ToList();
        var selectedProvId = owner.ProvinceId ?? provinces.FirstOrDefault()?.Id ?? Guid.Empty;
        var cities = _geoService.GetCitiesByProvince(selectedProvId).ToList();
        var selectedCityId = owner.CityMunicipalityId ?? cities.FirstOrDefault()?.Id ?? Guid.Empty;
        var barangays = _geoService.GetBarangaysByCity(selectedCityId).ToList();

        ViewBag.Provinces = provinces;
        ViewBag.Cities = cities;
        ViewBag.Barangays = barangays;

        return View(owner);
    }

    // POST: /Owner/Edit/{guid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, Owner model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        ValidateOwnerTypeFields(model);
        PopulateGeographicEntities(model);

        if (ModelState.IsValid)
        {
            _ownerService.Update(model);
            TempData["SuccessMessage"] = $"Owner '{model.DisplayName}' profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Failed to update owner profile. Please check validation errors.";
        ViewBag.Provinces = _geoService.GetProvinces();
        ViewBag.Cities = model.ProvinceId.HasValue ? _geoService.GetCitiesByProvince(model.ProvinceId.Value) : new List<CityMunicipality>();
        ViewBag.Barangays = model.CityMunicipalityId.HasValue ? _geoService.GetBarangaysByCity(model.CityMunicipalityId.Value) : new List<Barangay>();

        return View(model);
    }

    // POST: /Owner/Delete/{guid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        var owner = _ownerService.GetById(id);
        if (owner != null)
        {
            _ownerService.Delete(id);
            TempData["SuccessMessage"] = $"Owner '{owner.DisplayName}' was deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }
}
