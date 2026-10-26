using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IGeographicService
{
    IEnumerable<Province> GetProvinces();
    IEnumerable<CityMunicipality> GetCitiesByProvince(Guid provinceId);
    IEnumerable<Barangay> GetBarangaysByCity(Guid cityId);

    Province? GetProvinceById(Guid id);
    CityMunicipality? GetCityById(Guid id);
    Barangay? GetBarangayById(Guid id);
}
