using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class LocationController : Controller
{
    private readonly IGeographicService _geoService;

    public LocationController(IGeographicService geoService)
    {
        _geoService = geoService;
    }

    [HttpGet]
    public JsonResult GetProvinces()
    {
        var list = _geoService.GetProvinces().Select(p => new { id = p.Id, name = p.Name, region = p.Region });
        return Json(list);
    }

    [HttpGet]
    public JsonResult GetCitiesByProvince(Guid provinceId)
    {
        var list = _geoService.GetCitiesByProvince(provinceId).Select(c => new { id = c.Id, name = c.Name, zip = c.ZipCode });
        return Json(list);
    }

    [HttpGet]
    public JsonResult GetBarangaysByCity(Guid cityId)
    {
        var list = _geoService.GetBarangaysByCity(cityId).Select(b => new { id = b.Id, name = b.Name, district = b.District });
        return Json(list);
    }
}
