using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class TaxMappingService : ITaxMappingService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public TaxMappingService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
        SeedSampleRequestsIfEmpty();
    }

    private void SeedSampleRequestsIfEmpty()
    {
        try
        {
            if (!_context.TaxMappingRequests.Any())
            {
                var provId = Guid.Parse("10000000-0000-0000-0000-000000000001");
                var cityId = Guid.Parse("20000000-0000-0000-0000-000000000001");
                var brgy1 = Guid.Parse("30000000-0000-0000-0000-000000000001"); // Dadiangas East
                var brgy2 = Guid.Parse("30000000-0000-0000-0000-000000000002"); // Lagao

                _context.TaxMappingRequests.AddRange(new[]
                {
                    new TaxMappingRequest
                    {
                        Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                        RequestNumber = "TMR-2026-0001",
                        ApplicantName = "Alexander Vidal Sterling",
                        PropertyIndexNumber = "126-03-0012-004-12",
                        TaxDeclarationNumber = "TD-2026-GS-1004",
                        ProvinceId = provId,
                        CityMunicipalityId = cityId,
                        BarangayId = brgy1,
                        ProvinceName = "South Cotabato",
                        CityMunicipalityName = "General Santos City",
                        BarangayName = "Brgy. Dadiangas East",
                        StreetAddress = "124 Maplewood Drive",
                        Status = RequestStatus.UnderReview,
                        ParcelAreaSqm = 450.00m,
                        AssignedStaff = "Fernando K. Valdez (Tax Mapper)",
                        AttachmentFileName = "Subdivision_Plan_Lot12.pdf",
                        AttachmentFileSize = "2.4 MB",
                        WebRelativePath = "/uploads/taxmapping/sample_subdivision_plan.pdf",
                        PhysicalStoragePath = @"C:\Users\HP_02\Desktop\PropertyAssessmentApp\wwwroot\uploads\taxmapping\sample_subdivision_plan.pdf",
                        RequestDate = DateTime.Now.AddDays(-10),
                        UpdatedDate = DateTime.Now.AddDays(-10)
                    },
                    new TaxMappingRequest
                    {
                        Id = Guid.Parse("90000000-0000-0000-0000-000000000002"),
                        RequestNumber = "TMR-2026-0002",
                        ApplicantName = "Metro Plaza Commercial Corp.",
                        PropertyIndexNumber = "126-03-0005-001-08",
                        TaxDeclarationNumber = "TD-2026-GS-2005",
                        ProvinceId = provId,
                        CityMunicipalityId = cityId,
                        BarangayId = brgy2,
                        ProvinceName = "South Cotabato",
                        CityMunicipalityName = "General Santos City",
                        BarangayName = "Brgy. Lagao",
                        StreetAddress = "Suite 500, City Center Tower",
                        Status = RequestStatus.Approved,
                        ParcelAreaSqm = 1200.50m,
                        AssignedStaff = "Fernando K. Valdez (Tax Mapper)",
                        AttachmentFileName = "Consolidation_Approved_Map.pdf",
                        AttachmentFileSize = "4.1 MB",
                        WebRelativePath = "/uploads/taxmapping/sample_consolidation_map.pdf",
                        PhysicalStoragePath = @"C:\Users\HP_02\Desktop\PropertyAssessmentApp\wwwroot\uploads\taxmapping\sample_consolidation_map.pdf",
                        RequestDate = DateTime.Now.AddDays(-25),
                        UpdatedDate = DateTime.Now.AddDays(-25)
                    },
                    new TaxMappingRequest
                    {
                        Id = Guid.Parse("90000000-0000-0000-0000-000000000003"),
                        RequestNumber = "TMR-2026-0003",
                        ApplicantName = "Maria Clara De La Cruz",
                        PropertyIndexNumber = "126-03-0009-002-15",
                        TaxDeclarationNumber = "TD-2026-GS-4012",
                        ProvinceId = provId,
                        CityMunicipalityId = cityId,
                        BarangayId = brgy2,
                        ProvinceName = "South Cotabato",
                        CityMunicipalityName = "General Santos City",
                        BarangayName = "Brgy. Lagao",
                        StreetAddress = "Sitio Greenfields",
                        Status = RequestStatus.Pending,
                        ParcelAreaSqm = 320.00m,
                        AssignedStaff = "Fernando K. Valdez (Tax Mapper)",
                        RequestDate = DateTime.Now.AddDays(-2),
                        UpdatedDate = DateTime.Now.AddDays(-2)
                    },
                    new TaxMappingRequest
                    {
                        Id = Guid.Parse("23be3543-0ce2-4e8a-a337-7b53bb3babbd"),
                        RequestNumber = "TMR-2026-5880",
                        ApplicantName = "Taxpayer Applicant",
                        PropertyIndexNumber = "126-03-0012-004-12",
                        TaxDeclarationNumber = "TD-2026-GS-1004",
                        ProvinceId = provId,
                        CityMunicipalityId = cityId,
                        BarangayId = brgy1,
                        ProvinceName = "South Cotabato",
                        CityMunicipalityName = "General Santos City",
                        BarangayName = "Brgy. Dadiangas East",
                        StreetAddress = "General Santos City",
                        Status = RequestStatus.UnderReview,
                        ParcelAreaSqm = 350.00m,
                        AssignedStaff = "Charisse B. Pagay",
                        RequestDate = DateTime.Now.AddHours(-1),
                        UpdatedDate = DateTime.Now
                    }
                });

                _context.SaveChanges();
            }
        }
        catch
        {
            // Ignore seeding exception if table is already populated by concurrent request
        }
    }

    public string GetConfiguredStorageDirectory(string? webRootPath = null)
    {
        return _fileStorage.GetPhysicalStorageDirectory(FileDefaultLocation.Attachment, webRootPath);
    }

    public IEnumerable<TaxMappingRequest> GetAll(string? search = null, RequestStatus? status = null)
    {
        var query = _context.TaxMappingRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.RequestNumber.ToLower().Contains(term)
                                  || r.ApplicantName.ToLower().Contains(term)
                                  || r.PropertyIndexNumber.ToLower().Contains(term)
                                  || r.TaxDeclarationNumber.ToLower().Contains(term)
                                  || r.BarangayName.ToLower().Contains(term)
                                  || r.StreetAddress.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.RequestDate).ToList();
    }

    public TaxMappingRequest? GetById(Guid id)
    {
        return _context.TaxMappingRequests.FirstOrDefault(r => r.Id == id);
    }

    public TaxMappingRequest? GetByRequestNumber(string requestNo)
    {
        return _context.TaxMappingRequests.FirstOrDefault(r => r.RequestNumber.Equals(requestNo, StringComparison.OrdinalIgnoreCase));
    }

    public void Add(TaxMappingRequest request, IFormFile? fileUpload, string? webRootPath = null)
    {
        if (request.Id == Guid.Empty)
        {
            request.Id = Guid.NewGuid();
        }
        request.RequestDate = DateTime.Now;
        request.UpdatedDate = DateTime.Now;

        ProcessFileUpload(request, fileUpload, webRootPath);

        // Ensure navigation properties don't cause EF foreign key insertion attempts
        request.Province = null;
        request.CityMunicipality = null;
        request.Barangay = null;

        _context.TaxMappingRequests.Add(request);
        _context.SaveChanges();
    }

    public void Update(TaxMappingRequest request, IFormFile? fileUpload, string? webRootPath = null)
    {
        var existing = GetById(request.Id);
        if (existing != null)
        {
            existing.UpdatedDate = DateTime.Now;
            existing.Status = request.Status;
            existing.ApplicantName = request.ApplicantName;
            existing.OwnerContactNo = request.OwnerContactNo;
            existing.PropertyIndexNumber = request.PropertyIndexNumber;
            existing.TaxDeclarationNumber = request.TaxDeclarationNumber;
            existing.RequesterName = request.RequesterName;
            existing.ORNumber = request.ORNumber;
            existing.ORDateIssued = request.ORDateIssued;
            existing.Purposes = request.Purposes;
            existing.ProvinceId = request.ProvinceId;
            existing.CityMunicipalityId = request.CityMunicipalityId;
            existing.BarangayId = request.BarangayId;
            existing.ProvinceName = request.ProvinceName;
            existing.CityMunicipalityName = request.CityMunicipalityName;
            existing.BarangayName = request.BarangayName;
            existing.StreetAddress = request.StreetAddress;
            existing.ParcelAreaSqm = request.ParcelAreaSqm;
            existing.AssignedStaff = request.AssignedStaff;
            existing.TotalFeesAmount = request.TotalFeesAmount;
            existing.RequestedDocumentsJson = request.RequestedDocumentsJson;

            existing.ReqIdOfOwner = request.ReqIdOfOwner;
            existing.ReqAuthorizationLetter = request.ReqAuthorizationLetter;
            existing.ReqCertIndigency = request.ReqCertIndigency;
            existing.ReqIdOfRepresentative = request.ReqIdOfRepresentative;
            existing.ReqDeedOfConveyance = request.ReqDeedOfConveyance;

            existing.CertTaxDeclaration = request.CertTaxDeclaration;
            existing.CertRecordsVerification = request.CertRecordsVerification;
            existing.CertTaxDecTimeOfDeath = request.CertTaxDecTimeOfDeath;
            existing.CertAnnotationBailBond = request.CertAnnotationBailBond;
            existing.CertPropertyHistory = request.CertPropertyHistory;
            existing.CertAnnotationMortgages = request.CertAnnotationMortgages;
            existing.CertNoImprovement = request.CertNoImprovement;
            existing.CertCancellationAnnotation = request.CertCancellationAnnotation;
            existing.CertSupportingDocuments = request.CertSupportingDocuments;
            existing.CertRoadRightOfWay = request.CertRoadRightOfWay;
            existing.CertNoRoadRightOfWay = request.CertNoRoadRightOfWay;
            existing.CertTaxMap = request.CertTaxMap;
            existing.CertImageMap = request.CertImageMap;
            existing.CertPropertyIdVerification = request.CertPropertyIdVerification;
            existing.CertFieldInspection = request.CertFieldInspection;
            existing.CertOthers = request.CertOthers;
            existing.CertOthersSpecification = request.CertOthersSpecification;

            if (fileUpload != null && fileUpload.Length > 0)
            {
                ProcessFileUpload(existing, fileUpload, webRootPath);
            }

            _context.SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var item = GetById(id);
        if (item != null)
        {
            _context.TaxMappingRequests.Remove(item);
            _context.SaveChanges();
        }
    }

    private void ProcessFileUpload(TaxMappingRequest request, IFormFile? fileUpload, string? webRootPath)
    {
        if (fileUpload != null && fileUpload.Length > 0)
        {
            try
            {
                var result = _fileStorage.SaveFile(fileUpload, FileDefaultLocation.Attachment, webRootPath, $"TMR_{request.RequestNumber}");
                if (result.Success)
                {
                    request.AttachmentFileName = result.OriginalFileName;
                    request.AttachmentFileSize = result.FileSizeFormatted;
                    request.PhysicalStoragePath = result.PhysicalPath;
                    request.WebRelativePath = result.WebRelativePath;
                }
            }
            catch
            {
                // File upload failure should not prevent request saving
            }
        }
    }

    public int GetTotalCount() => _context.TaxMappingRequests.Count();
    public int GetPendingCount() => _context.TaxMappingRequests.Count(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.UnderReview);
    public int GetApprovedCount() => _context.TaxMappingRequests.Count(r => r.Status == RequestStatus.Approved);
}
