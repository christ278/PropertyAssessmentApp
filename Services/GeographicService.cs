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

            // Koronadal City (27 Official Barangays)
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000006"), CityMunicipalityId = cityKoronadal.Id, Code = "0001", Name = "Brgy. Zone I (Poblacion)", District = "Poblacion" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000007"), CityMunicipalityId = cityKoronadal.Id, Code = "0002", Name = "Brgy. Zone II (Poblacion)", District = "Poblacion" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000010"), CityMunicipalityId = cityKoronadal.Id, Code = "0003", Name = "Brgy. Zone III (Poblacion)", District = "Poblacion" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000011"), CityMunicipalityId = cityKoronadal.Id, Code = "0004", Name = "Brgy. Zone IV (Poblacion)", District = "Poblacion" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000012"), CityMunicipalityId = cityKoronadal.Id, Code = "0005", Name = "Brgy. Assumption", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000013"), CityMunicipalityId = cityKoronadal.Id, Code = "0006", Name = "Brgy. Avanceña", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000014"), CityMunicipalityId = cityKoronadal.Id, Code = "0007", Name = "Brgy. Cacub", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000015"), CityMunicipalityId = cityKoronadal.Id, Code = "0008", Name = "Brgy. Caloocan", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000016"), CityMunicipalityId = cityKoronadal.Id, Code = "0009", Name = "Brgy. Carpenter Hill", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000017"), CityMunicipalityId = cityKoronadal.Id, Code = "0010", Name = "Brgy. Concepcion", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000018"), CityMunicipalityId = cityKoronadal.Id, Code = "0011", Name = "Brgy. Esperanza", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000019"), CityMunicipalityId = cityKoronadal.Id, Code = "0012", Name = "Brgy. Gen. P. Santos", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000020"), CityMunicipalityId = cityKoronadal.Id, Code = "0013", Name = "Brgy. Mabini", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000021"), CityMunicipalityId = cityKoronadal.Id, Code = "0014", Name = "Brgy. Magsaysay", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000022"), CityMunicipalityId = cityKoronadal.Id, Code = "0015", Name = "Brgy. Mambucal", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000023"), CityMunicipalityId = cityKoronadal.Id, Code = "0016", Name = "Brgy. Morales", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000024"), CityMunicipalityId = cityKoronadal.Id, Code = "0017", Name = "Brgy. Namnama", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000025"), CityMunicipalityId = cityKoronadal.Id, Code = "0018", Name = "Brgy. New Pangasinan", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000026"), CityMunicipalityId = cityKoronadal.Id, Code = "0019", Name = "Brgy. Paraiso", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000027"), CityMunicipalityId = cityKoronadal.Id, Code = "0020", Name = "Brgy. Rotonda", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000028"), CityMunicipalityId = cityKoronadal.Id, Code = "0021", Name = "Brgy. San Isidro", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000029"), CityMunicipalityId = cityKoronadal.Id, Code = "0022", Name = "Brgy. San Jose", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000030"), CityMunicipalityId = cityKoronadal.Id, Code = "0023", Name = "Brgy. San Roque", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000031"), CityMunicipalityId = cityKoronadal.Id, Code = "0024", Name = "Brgy. Saravia", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000032"), CityMunicipalityId = cityKoronadal.Id, Code = "0025", Name = "Brgy. Sta. Cruz", District = "District 1" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000033"), CityMunicipalityId = cityKoronadal.Id, Code = "0026", Name = "Brgy. Sto. Niño", District = "District 2" },
            new Barangay { Id = Guid.Parse("30000000-0000-0000-0000-000000000034"), CityMunicipalityId = cityKoronadal.Id, Code = "0027", Name = "Brgy. Topland", District = "District 2" },

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
