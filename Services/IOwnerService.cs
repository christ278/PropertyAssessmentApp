using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IOwnerService
{
    IEnumerable<Owner> GetAll(string? search = null, OwnerType? type = null, bool? isActive = null);
    Owner? GetById(Guid id);
    Owner? GetByRptin(string rptin);
    void Add(Owner owner);
    void Update(Owner owner);
    void Delete(Guid id);

    int GetTotalCount();
    int GetIndividualCount();
    int GetCorporateCount();
}
