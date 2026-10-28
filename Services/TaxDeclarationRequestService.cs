using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class TaxDeclarationRequestService : ITaxDeclarationRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public TaxDeclarationRequestService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public string GetConfiguredStorageDirectory(string? webRootPath = null)
    {
        return _fileStorage.GetPhysicalStorageDirectory(FileDefaultLocation.Attachment, webRootPath);
    }

    public IEnumerable<TaxDeclarationRequest> GetAll(string? search = null, RequestStatus? status = null)
    {
        var query = _context.TaxDeclarationRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.TrackingNumber.ToLower().Contains(term)
                                  || r.OwnerName.ToLower().Contains(term)
                                  || (r.TaxDeclarationNumber != null && r.TaxDeclarationNumber.ToLower().Contains(term))
                                  || (r.RequesterName != null && r.RequesterName.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.RequestDate).ToList();
    }

    public TaxDeclarationRequest? GetById(Guid id)
    {
        return _context.TaxDeclarationRequests.FirstOrDefault(r => r.Id == id);
    }

    public TaxDeclarationRequest? GetByTrackingNumber(string trackingNo)
    {
        return _context.TaxDeclarationRequests.FirstOrDefault(r => r.TrackingNumber == trackingNo);
    }

    public void Add(TaxDeclarationRequest request, IFormFile? fileUpload, string? webRootPath = null)
    {
        if (request.Id == Guid.Empty)
        {
            request.Id = Guid.NewGuid();
        }
        request.RequestDate = DateTime.Now;
        request.UpdatedDate = DateTime.Now;

        ProcessFileUpload(request, fileUpload, webRootPath);

        _context.TaxDeclarationRequests.Add(request);
        _context.SaveChanges();
    }

    public void Update(TaxDeclarationRequest request, IFormFile? fileUpload, string? webRootPath = null)
    {
        var existing = GetById(request.Id);
        if (existing != null)
        {
            existing.UpdatedDate = DateTime.Now;
            existing.Status = request.Status;
            existing.OwnerName = request.OwnerName;
            existing.ContactNo = request.ContactNo;
            existing.TaxDeclarationNumber = request.TaxDeclarationNumber;
            existing.LotNo = request.LotNo;
            existing.PropertyLocation = request.PropertyLocation;
            existing.RequesterName = request.RequesterName;
            existing.ORNumber = request.ORNumber;
            existing.ORDateIssued = request.ORDateIssued;
            existing.Purposes = request.Purposes;

            existing.ReqIdOfOwner = request.ReqIdOfOwner;
            existing.ReqIdOfRepresentative = request.ReqIdOfRepresentative;
            existing.ReqAuthorizationLetter = request.ReqAuthorizationLetter;
            existing.ReqDeedOfConveyance = request.ReqDeedOfConveyance;
            existing.ReqCertIndigency = request.ReqCertIndigency;

            existing.CertTaxDeclaration = request.CertTaxDeclaration;
            existing.CertRecordsVerification = request.CertRecordsVerification;
            existing.CertTaxDecTimeOfDeath = request.CertTaxDecTimeOfDeath;
            existing.CertAnnotationBailBond = request.CertAnnotationBailBond;
            existing.CertPropertyHistory = request.CertPropertyHistory;
            existing.CertAnnotationMortgages = request.CertAnnotationMortgages;
            existing.CertNoImprovement = request.CertNoImprovement;
            existing.CertCancellationAnnotation = request.CertCancellationAnnotation;
            existing.CertSupportingDocuments = request.CertSupportingDocuments;

            existing.RequestedDocumentsJson = request.RequestedDocumentsJson;
            existing.TotalFeesAmount = request.TotalFeesAmount;

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
            _context.TaxDeclarationRequests.Remove(item);
            _context.SaveChanges();
        }
    }

    private void ProcessFileUpload(TaxDeclarationRequest request, IFormFile? fileUpload, string? webRootPath)
    {
        if (fileUpload != null && fileUpload.Length > 0)
        {
            try
            {
                var result = _fileStorage.SaveFile(fileUpload, FileDefaultLocation.Attachment, webRootPath, $"TDReq_{request.TrackingNumber}");
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

    private string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{((double)bytes / (1024 * 1024)):N1} MB";
        if (bytes >= 1024)
            return $"{((double)bytes / 1024):N1} KB";
        return $"{bytes} Bytes";
    }

    public int GetTotalCount() => _context.TaxDeclarationRequests.Count();
    public int GetPendingCount() => _context.TaxDeclarationRequests.Count(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.UnderReview);
    public int GetApprovedCount() => _context.TaxDeclarationRequests.Count(r => r.Status == RequestStatus.Approved);
}
