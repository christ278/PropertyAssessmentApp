using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IUserAccountService
{
    IEnumerable<UserAccount> GetAll(string? search = null, UserRole? role = null, bool? isActive = null, int? divisionId = null);
    UserAccount? GetById(Guid id);
    UserAccount? GetByUsername(string username);
    void Add(UserAccount user);
    void Update(UserAccount user);
    void SetActiveStatus(Guid id, bool isActive);
    bool VerifyPassword(UserAccount user, string providedPassword);
    int GetTotalCount();
    int GetActiveCount();
    int GetInactiveCount();
}
