using Microsoft.AspNetCore.Http;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface ILandHoldingRequestService
{
    IEnumerable<LandHoldingRequest> GetAll(string? search = null, LandHoldingRequestStatus? status = null);
    LandHoldingRequest? GetById(Guid id);
    LandHoldingRequest? GetByTrackingNumber(string trackingNo);
    void Add(LandHoldingRequest request, IFormFile? fileUpload, string? webRootPath = null);
    void Update(LandHoldingRequest request, IFormFile? fileUpload, string? webRootPath = null);
    void Delete(Guid id);
    string GetConfiguredStorageDirectory(string? webRootPath = null);
    int GetTotalCount();
    int GetPendingCount();
    int GetApprovedCount();
}
