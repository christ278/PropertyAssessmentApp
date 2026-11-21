using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IAssessmentService
{
    IEnumerable<TaxDeclaration> GetAll(string? search = null, PropertyKind? kind = null, string? status = null);
    (List<TaxDeclaration> Items, int TotalCount) GetPaged(string? search = null, PropertyKind? kind = null, string? status = null, int page = 1, int limit = 10);
    TaxDeclaration? GetById(Guid id);
    TaxDeclaration? GetByTdNumber(string tdNumber);
    void Add(TaxDeclaration declaration);
    void Update(TaxDeclaration declaration);
    void Delete(Guid id);
    
    // Summary statistics for Assessor Dashboard
    decimal GetTotalMarketValue();
    decimal GetTotalAssessedValue();
    decimal GetTotalEstimatedTaxRevenue();
    int GetTotalActivePropertiesCount();
    decimal GetDefaultAssessmentLevel(PropertyClassification classification);
    string GenerateCastTdNumber(int targetYear, string? barangay);
    string FormatCastArpNumber(string? barangay, string? sectionNo, string? lotOrParcelNo);
    void SyncChildEntities(TaxDeclaration declaration);
}
