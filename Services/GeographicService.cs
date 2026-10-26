using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class GeographicService : IGeographicService
{
    private readonly List<Province> _provinces = new();
    private readonly List<CityMunicipality> _cities = new();
    private readonly List<Barangay> _barangays = new();

    public GeographicService()
    {
        SeedGeographicData();
    }

    private void SeedGeographicData()
    {
        var provSouthCotabato = new Province { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Code = "PROV-1263", Name = "South Cotabato", Region = "Region XII (SOCCSKSARGEN)" };
        var provCavite = new Province { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Code = "PROV-0421", Name = "Cavite", Region = "Region IV-A (CALABARZON)" };
        var provMetroManila = new Province { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Code = "PROV-1300", Name = "Metro Manila (NCR)", Region = "National Capital Region" };
        var provCebu = new Province { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Code = "PROV-0722", Name = "Cebu", Region = "Region VII (Central Visayas)" };

        _provinces.AddRange(new[] { provSouthCotabato, provCavite, provMetroManila, provCebu });

        // Cities
        var cityGenSan = new CityMunicipality { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), ProvinceId = provSouthCotabato.Id, Code = "CITY-9500", Name = "General Santos City", ZipCode = "9500", IsCity = true };
        var cityKoronadal = new CityMunicipality { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), ProvinceId = provSouthCotabato.Id, Code = "CITY-9506", Name = "Koronadal City", ZipCode = "9506", IsCity = true };
        var muniPolomolok = new CityMunicipality { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), ProvinceId = provSouthCotabato.Id, Code = "MUNI-9504", Name = "Polomolok", ZipCode = "9504", IsCity = false };
        var cityDasma = new CityMunicipality { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), ProvinceId = provCavite.Id, Code = "CITY-4114", Name = "Dasmariñas City", ZipCode = "4114", IsCity = true };
        var cityQuezon = new CityMunicipality { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), ProvinceId = provMetroManila.Id, Code = "CITY-1100", Name = "Quezon City", ZipCode = "1100", IsCity = true };

        _cities.AddRange(new[] { cityGenSan, cityKoronadal, muniPolomolok, cityDasma, cityQuezon });

        // Barangays for GenSan
        _barangays.AddRange(new[]
        {
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), CityMunicipalityId = cityGenSan.Id, Code = "BRGY-9500-01", Name = "Brgy. Dadiangas East", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), CityMunicipalityId = cityGenSan.Id, Code = "BRGY-9500-02", Name = "Brgy. Lagao", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000003"), CityMunicipalityId = cityGenSan.Id, Code = "BRGY-9500-03", Name = "Brgy. Calumpang", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000004"), CityMunicipalityId = cityGenSan.Id, Code = "BRGY-9500-04", Name = "Brgy. San Isidro", District = "District 3" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000005"), CityMunicipalityId = cityGenSan.Id, Code = "BRGY-9500-05", Name = "Brgy. City Heights", District = "District 2" },

            // Koronadal
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000006"), CityMunicipalityId = cityKoronadal.Id, Code = "BRGY-9506-01", Name = "Brgy. Zone 1 (Poblacion)", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000007"), CityMunicipalityId = cityKoronadal.Id, Code = "BRGY-9506-02", Name = "Brgy. Morales", District = "District 2" },

            // Dasmariñas
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000008"), CityMunicipalityId = cityDasma.Id, Code = "BRGY-4114-01", Name = "Brgy. Salitran I", District = "Zone 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000009"), CityMunicipalityId = cityDasma.Id, Code = "BRGY-4114-02", Name = "Brgy. Paliparan I", District = "Zone 2" }
        });
    }

    public IEnumerable<Province> GetProvinces() => _provinces.Where(p => p.IsActive).OrderBy(p => p.Name);

    public IEnumerable<CityMunicipality> GetCitiesByProvince(Guid provinceId) =>
        _cities.Where(c => c.ProvinceId == provinceId && c.IsActive).OrderBy(c => c.Name);

    public IEnumerable<Barangay> GetBarangaysByCity(Guid cityId) =>
        _barangays.Where(b => b.CityMunicipalityId == cityId && b.IsActive).OrderBy(b => b.Name);

    public Province? GetProvinceById(Guid id) => _provinces.FirstOrDefault(p => p.Id == id);
    public CityMunicipality? GetCityById(Guid id) => _cities.FirstOrDefault(c => c.Id == id);
    public Barangay? GetBarangayById(Guid id) => _barangays.FirstOrDefault(b => b.Id == id);
}
