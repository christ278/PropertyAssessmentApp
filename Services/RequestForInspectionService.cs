using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class RequestForInspectionService : IRequestForInspectionService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public RequestForInspectionService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public string GetConfiguredStorageDirectory(string? webRootPath = null)
    {
        return _fileStorage.GetPhysicalStorageDirectory(FileDefaultLocation.Inspection, webRootPath);
    }

    public IEnumerable<RequestForInspection> GetAll(string? search = null, RequestForInspectionStatus? status = null)
    {
        var query = _context.RequestForInspections.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.TrackingNumber.ToLower().Contains(term)
                                  || r.OwnerName.ToLower().Contains(term)
                                  || r.TaxDeclarationNumber.ToLower().Contains(term)
                                  || r.PropertyIndexNumber.ToLower().Contains(term)
                                  || (r.ContactPerson != null && r.ContactPerson.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.RequestDate).ToList();
    }

    public RequestForInspection? GetById(Guid id)
    {
        return _context.RequestForInspections.FirstOrDefault(r => r.Id == id);
    }

    public RequestForInspection? GetByTrackingNumber(string trackingNo)
    {
        return _context.RequestForInspections.FirstOrDefault(r => r.TrackingNumber == trackingNo);
    }

    public void Add(RequestForInspection request, IFormFile? fileUpload, string? webRootPath = null)
    {
        if (request.Id == Guid.Empty)
        {
            request.Id = Guid.NewGuid();
        }
        request.RequestDate = DateTime.Now;
        request.UpdatedDate = DateTime.Now;

        ProcessFileUpload(request, fileUpload, webRootPath);

        _context.RequestForInspections.Add(request);
        _context.SaveChanges();
    }

    public void Update(RequestForInspection request, IFormFile? fileUpload, string? webRootPath = null)
    {
        var existing = GetById(request.Id);
        if (existing != null)
        {
            existing.UpdatedDate = DateTime.Now;
            existing.Status = request.Status;
            existing.IsDepreciation = request.IsDepreciation;
            existing.IsReassessment = request.IsReassessment;
            existing.IsCancelled = request.IsCancelled;
            existing.IsOther = request.IsOther;
            existing.OtherPurposeDetails = request.OtherPurposeDetails;

            existing.OwnerName = request.OwnerName;
            existing.TaxDeclarationNumber = request.TaxDeclarationNumber;
            existing.PropertyIndexNumber = request.PropertyIndexNumber;
            existing.Kind = request.Kind;
            existing.LotNo = request.LotNo;
            existing.PropertyLocation = request.PropertyLocation;

            existing.ContactPerson = request.ContactPerson;
            existing.ContactNumber = request.ContactNumber;
            existing.EmailAddress = request.EmailAddress;
            existing.Representative = request.Representative;

            existing.RequestedDocumentsJson = request.RequestedDocumentsJson;
            existing.TotalFeesAmount = request.TotalFeesAmount;
            existing.ORNumber = request.ORNumber;
            existing.ORDateIssued = request.ORDateIssued;
            existing.Remarks = request.Remarks;
            existing.AssignedStaff = request.AssignedStaff;

            if (fileUpload != null && fileUpload.Length > 0)
            {
                ProcessFileUpload(existing, fileUpload, webRootPath);
            }

            _context.SaveChanges();
        }
    }

    public void UpdateStatus(Guid id, RequestForInspectionStatus newStatus, string? remarks = null, string? staff = null)
    {
        var item = GetById(id);
        if (item != null)
        {
            item.Status = newStatus;
            item.UpdatedDate = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(remarks)) item.Remarks = remarks;
            if (!string.IsNullOrWhiteSpace(staff)) item.AssignedStaff = staff;
            _context.SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var item = GetById(id);
        if (item != null)
        {
            _context.RequestForInspections.Remove(item);
            _context.SaveChanges();
        }
    }

    private void ProcessFileUpload(RequestForInspection request, IFormFile? fileUpload, string? webRootPath)
    {
        if (fileUpload != null && fileUpload.Length > 0)
        {
            try
            {
                var result = _fileStorage.SaveFile(fileUpload, FileDefaultLocation.Inspection, webRootPath, $"RFI_{request.TrackingNumber}");
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

    public int GetTotalCount() => _context.RequestForInspections.Count();
    public int GetPendingCount() => _context.RequestForInspections.Count(r => r.Status != RequestForInspectionStatus.Releasing && r.Status != RequestForInspectionStatus.Deferred);
    public int GetApprovedCount() => _context.RequestForInspections.Count(r => r.Status == RequestForInspectionStatus.Approval || r.Status == RequestForInspectionStatus.Printing || r.Status == RequestForInspectionStatus.Releasing);
}
