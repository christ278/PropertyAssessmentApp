using Microsoft.AspNetCore.Http;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface ITaxDeclarationRequestService
{
    IEnumerable<TaxDeclarationRequest> GetAll(string? search = null, TaxDeclarationRequestStatus? status = null);
    TaxDeclarationRequest? GetById(Guid id);
    TaxDeclarationRequest? GetByTrackingNumber(string trackingNo);
    void Add(TaxDeclarationRequest request, IFormFile? fileUpload, string? webRootPath = null);
    void Update(TaxDeclarationRequest request, IFormFile? fileUpload, string? webRootPath = null);
    void Delete(Guid id);
    string GetConfiguredStorageDirectory(string? webRootPath = null);
    int GetTotalCount();
    int GetPendingCount();
    int GetApprovedCount();
}
