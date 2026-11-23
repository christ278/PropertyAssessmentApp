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
            bool newlyApproved = existing.Status != RequestStatus.Approved && request.Status == RequestStatus.Approved;
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

            if (newlyApproved)
            {
                SyncPropertyTaggingOnApproval(existing);
            }
        }
    }

    public void SyncPropertyTaggingOnApproval(TaxDeclarationRequest request, string? approvedBy = null)
    {
        if (!request.CertCancellationAnnotation && !request.CertAnnotationBailBond && !request.CertAnnotationMortgages)
        {
            return;
        }

        TaxDeclaration? td = null;
        if (request.TaxDeclarationId.HasValue && request.TaxDeclarationId.Value != Guid.Empty)
        {
            td = _context.TaxDeclarations.FirstOrDefault(t => t.Id == request.TaxDeclarationId.Value);
        }
        if (td == null && !string.IsNullOrWhiteSpace(request.TaxDeclarationNumber))
        {
            var tdn = request.TaxDeclarationNumber.Trim();
            td = _context.TaxDeclarations.FirstOrDefault(t => t.TaxDeclarationNumber == tdn);
        }
        if (td == null)
        {
            return;
        }

        if (request.CertCancellationAnnotation)
        {
            var existingTags = _context.PropertyTaggings
                .Where(x => x.TaxDeclarationId == td.Id)
                .ToList();
            if (existingTags.Any())
            {
                var removedTypes = string.Join(", ", existingTags.Select(t => t.TagType.ToString()).Distinct());
                _context.PropertyTaggings.RemoveRange(existingTags);
                _context.PropertyTaggingLogs.Add(new PropertyTaggingLog
                {
                    Id = Guid.NewGuid(),
                    TaxDeclarationId = td.Id,
                    TaxDeclarationNumber = td.TaxDeclarationNumber,
                    PropertyIndexNumber = td.PropertyIndexNumber,
                    OwnerName = td.OwnerName,
                    TagType = removedTypes,
                    Action = "Removed (Cancellation of Annotation)",
                    Details = $"Removed {existingTags.Count} tag(s) upon approval of TD Request {request.TrackingNumber}.",
                    Source = $"Tax Declaration Request ({request.TrackingNumber})",
                    PerformedBy = !string.IsNullOrWhiteSpace(approvedBy) ? approvedBy.Trim() : (request.RequesterName ?? "Workflow Approval"),
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                _context.PropertyTaggingLogs.Add(new PropertyTaggingLog
                {
                    Id = Guid.NewGuid(),
                    TaxDeclarationId = td.Id,
                    TaxDeclarationNumber = td.TaxDeclarationNumber,
                    PropertyIndexNumber = td.PropertyIndexNumber,
                    OwnerName = td.OwnerName,
                    TagType = "CancellationOfAnnotation",
                    Action = "Removed (Cancellation of Annotation)",
                    Details = $"Cancellation of Annotation approved on TD Request {request.TrackingNumber} (no active tag found).",
                    Source = $"Tax Declaration Request ({request.TrackingNumber})",
                    PerformedBy = !string.IsNullOrWhiteSpace(approvedBy) ? approvedBy.Trim() : (request.RequesterName ?? "Workflow Approval"),
                    Timestamp = DateTime.Now
                });
            }
        }

        var party = !string.IsNullOrWhiteSpace(request.RequesterName)
            ? request.RequesterName.Trim()
            : $"TD Request {request.TrackingNumber}";
        decimal? amt = request.TotalFeesAmount > 0 ? request.TotalFeesAmount : null;
        var createdBy = !string.IsNullOrWhiteSpace(approvedBy) ? approvedBy.Trim() : (request.RequesterName ?? "Workflow Approval");

        if (request.CertAnnotationBailBond)
        {
            _context.PropertyTaggings.Add(new PropertyTagging
            {
                Id = Guid.NewGuid(),
                TaxDeclarationId = td.Id,
                TaxDeclarationNumber = td.TaxDeclarationNumber,
                PropertyIndexNumber = td.PropertyIndexNumber,
                OwnerName = td.OwnerName,
                TagType = PropertyTagType.Bailbond,
                PartyName = party,
                Amount = amt,
                TagDate = DateTime.Now,
                CreatedBy = createdBy,
                CreatedDate = DateTime.Now
            });
            _context.PropertyTaggingLogs.Add(new PropertyTaggingLog
            {
                Id = Guid.NewGuid(),
                TaxDeclarationId = td.Id,
                TaxDeclarationNumber = td.TaxDeclarationNumber,
                PropertyIndexNumber = td.PropertyIndexNumber,
                OwnerName = td.OwnerName,
                TagType = PropertyTagType.Bailbond.ToString(),
                Action = "Tagged",
                Details = $"Annotation of Bail Bond | Party: {party}",
                Source = $"Tax Declaration Request ({request.TrackingNumber})",
                PerformedBy = createdBy,
                Timestamp = DateTime.Now
            });
        }

        if (request.CertAnnotationMortgages)
        {
            _context.PropertyTaggings.Add(new PropertyTagging
            {
                Id = Guid.NewGuid(),
                TaxDeclarationId = td.Id,
                TaxDeclarationNumber = td.TaxDeclarationNumber,
                PropertyIndexNumber = td.PropertyIndexNumber,
                OwnerName = td.OwnerName,
                TagType = PropertyTagType.Mortgage,
                PartyName = party,
                Amount = amt,
                TagDate = DateTime.Now,
                CreatedBy = createdBy,
                CreatedDate = DateTime.Now
            });
            _context.PropertyTaggingLogs.Add(new PropertyTaggingLog
            {
                Id = Guid.NewGuid(),
                TaxDeclarationId = td.Id,
                TaxDeclarationNumber = td.TaxDeclarationNumber,
                PropertyIndexNumber = td.PropertyIndexNumber,
                OwnerName = td.OwnerName,
                TagType = PropertyTagType.Mortgage.ToString(),
                Action = "Tagged",
                Details = $"Annotation of Mortgages | Party: {party}",
                Source = $"Tax Declaration Request ({request.TrackingNumber})",
                PerformedBy = createdBy,
                Timestamp = DateTime.Now
            });
        }

        _context.SaveChanges();
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
