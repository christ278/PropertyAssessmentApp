using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IAssessmentService
{
    IEnumerable<TaxDeclaration> GetAll(string? search = null, PropertyClassification? classification = null, AssessmentStatus? status = null);
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
}
