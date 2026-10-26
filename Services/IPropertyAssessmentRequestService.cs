using Microsoft.AspNetCore.Http;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IPropertyAssessmentRequestService
{
    IEnumerable<PropertyAssessmentRequest> GetAll(string? search = null, PropertyAssessmentStatus? status = null);
    PropertyAssessmentRequest? GetById(Guid id);
    PropertyAssessmentRequest? GetByTrackingNumber(string trackingNo);
    void Add(PropertyAssessmentRequest request, IFormFile? fileUpload = null, string? webRootPath = null);
    void Update(PropertyAssessmentRequest request, IFormFile? fileUpload = null, string? webRootPath = null);
    void UpdateStatus(Guid id, PropertyAssessmentStatus newStatus, string? remarks = null, string? appraiser = null);
    void ForwardRequest(Guid id, string? orNumber, DateTime? orDateIssued, PropertyAssessmentStatus targetStatus = PropertyAssessmentStatus.Replotting, string? remarks = null, string? appraiser = null);
    void SyncPartitionTaxDeclarationStatuses(PropertyAssessmentRequest request);
    void Delete(Guid id);
    IEnumerable<TaxDeclaration> PartitionAssessment(Guid requestId, int partitionCount);
    string GetConfiguredStorageDirectory(string? webRootPath = null);
    int GetTotalCount();
    int GetPendingCount();
    int GetApprovedCount();
    List<BuildingClassification> GetBuildingClassifications();
    List<BuildingAdditionalItem> GetBuildingAdditionalItems();
    List<BuildingStructuralType> GetBuildingStructuralTypes();
    List<BuildingType> GetBuildingTypes();
    List<BuildingPropertyTypeAccordUse> GetBuildingPropertyTypesAccordUse();
    decimal GetUnitValue(string structuralType, string classification, string mode = "Min");
    decimal GetAssessmentLevel(string actualUse, decimal marketValue, int revisionYear = 2011);
    decimal GetDepreciationRate(string structuralType, string actualUse, int age, string maintenance = "AVERAGE");
}
