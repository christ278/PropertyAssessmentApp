using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IUserRoleService
{
    IEnumerable<UserRoleSetting> GetAll(string? search = null);
    UserRoleSetting? GetById(Guid id);
    void Add(UserRoleSetting role);
    void Update(UserRoleSetting role);
    void Delete(Guid id);
    int GetTotalCount();
}
