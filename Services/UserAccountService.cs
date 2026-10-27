using Microsoft.AspNetCore.Identity;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class UserAccountService : IUserAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<UserAccount> _passwordHasher = new();

    public UserAccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<UserAccount> GetAll(string? search = null, UserRole? role = null, bool? isActive = null, int? divisionId = null)
    {
        var query = _context.UserAccounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(term)
                                  || u.FullName.ToLower().Contains(term)
                                  || u.Email.ToLower().Contains(term)
                                  || u.EmployeeNo.ToLower().Contains(term)
                                  || u.Department.ToLower().Contains(term)
                                  || (u.WorkflowRoleName != null && u.WorkflowRoleName.ToLower().Contains(term)));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (divisionId.HasValue && divisionId.Value > 0)
        {
            query = query.Where(u => u.DivisionId == divisionId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return query.OrderByDescending(u => u.IsActive).ThenBy(u => u.FullName).ToList();
    }

    public UserAccount? GetById(Guid id)
    {
        return _context.UserAccounts.FirstOrDefault(u => u.Id == id);
    }

    public UserAccount? GetByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var term = username.Trim().ToLower();
        return _context.UserAccounts.FirstOrDefault(u => u.Username.ToLower() == term);
    }

    public void Add(UserAccount user)
    {
        if (user.Id == Guid.Empty)
        {
            user.Id = Guid.NewGuid();
        }
        user.CreatedDate = DateTime.Now;

        var rawPassword = !string.IsNullOrWhiteSpace(user.Password) ? user.Password : "P@ssword2026!";
        user.PasswordHash = _passwordHasher.HashPassword(user, rawPassword);

        _context.UserAccounts.Add(user);
        _context.SaveChanges();
    }

    public void Update(UserAccount user)
    {
        var existing = _context.UserAccounts.FirstOrDefault(u => u.Id == user.Id);
        if (existing != null)
        {
            existing.Username = user.Username;
            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.EmployeeNo = user.EmployeeNo;
            existing.Role = user.Role;
            existing.Department = user.Department;
            existing.ContactNo = user.ContactNo;
            existing.IsActive = user.IsActive;
            existing.DivisionId = user.DivisionId;
            existing.WorkflowRoleId = user.WorkflowRoleId;
            existing.WorkflowRoleName = user.WorkflowRoleName;
            existing.LastLoginDate = user.LastLoginDate;

            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                existing.PasswordHash = _passwordHasher.HashPassword(existing, user.Password);
            }

            _context.SaveChanges();
        }
    }

    public void SetActiveStatus(Guid id, bool isActive)
    {
        var user = GetById(id);
        if (user != null)
        {
            user.IsActive = isActive;
            _context.SaveChanges();
        }
    }

    public bool VerifyPassword(UserAccount user, string providedPassword)
    {
        if (user == null || string.IsNullOrWhiteSpace(providedPassword) || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        // 1. ASP.NET Core PasswordHasher PBKDF2 check against PasswordHash
        try
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return true;
            }
        }
        catch { }

        // 2. Legacy CAST SHA256 lower-case hex check against PasswordHash
        try
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(providedPassword);
            var hash = BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", "").ToLower();
            if (string.Equals(user.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    public int GetTotalCount() => _context.UserAccounts.Count();
    public int GetActiveCount() => _context.UserAccounts.Count(u => u.IsActive);
    public int GetInactiveCount() => _context.UserAccounts.Count(u => !u.IsActive);
}
