using Microsoft.AspNetCore.Http;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IRequestForInspectionService
{
    IEnumerable<RequestForInspection> GetAll(string? search = null, RequestForInspectionStatus? status = null);
    RequestForInspection? GetById(Guid id);
    RequestForInspection? GetByTrackingNumber(string trackingNo);
    void Add(RequestForInspection request, IFormFile? fileUpload, string? webRootPath = null);
    void Update(RequestForInspection request, IFormFile? fileUpload, string? webRootPath = null);
    void UpdateStatus(Guid id, RequestForInspectionStatus newStatus, string? remarks = null, string? staff = null);
    void Delete(Guid id);
    string GetConfiguredStorageDirectory(string? webRootPath = null);
    int GetTotalCount();
    int GetPendingCount();
    int GetApprovedCount();
}
