using Microsoft.AspNetCore.Http;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface ITaxMappingService
{
    IEnumerable<TaxMappingRequest> GetAll(string? search = null, RequestStatus? status = null);
    TaxMappingRequest? GetById(Guid id);
    TaxMappingRequest? GetByRequestNumber(string requestNo);
    void Add(TaxMappingRequest request, IFormFile? fileUpload, string? webRootPath = null);
    void Update(TaxMappingRequest request, IFormFile? fileUpload, string? webRootPath = null);
    void Delete(Guid id);
    string GetConfiguredStorageDirectory(string? webRootPath = null);
    int GetTotalCount();
    int GetPendingCount();
    int GetApprovedCount();
}
