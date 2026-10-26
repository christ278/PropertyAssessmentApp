using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class UserRoleService : IUserRoleService
{
    private readonly ApplicationDbContext _context;

    public UserRoleService(ApplicationDbContext context)
    {
        _context = context;
        EnsureSeedRoles();
    }

    private void EnsureSeedRoles()
    {
        if (!_context.UserRoleSettings.Any(r => r.RoleName == "System Administrator"))
        {
            _context.UserRoleSettings.Add(new UserRoleSetting
            {
                Id = Guid.Parse("50147D2F-24FE-4FA9-B270-9F217F58D333"),
                RoleName = "System Administrator",
                Division = null,
                TransactionStatusId = null,
                Description = "Full administrative access, user accounts, and system configuration.",
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }

        var statuses = _context.TransactionStatuses.Where(s => s.TransactionStatusId > 0).ToList();
        foreach (var status in statuses)
        {
            var divType = status.DivisionId == 1 ? DivisionType.Division1_TaxAssessmentAndLandHolding
                        : status.DivisionId == 2 ? DivisionType.Division2_GIS_TaxMapping
                        : status.DivisionId == 3 ? DivisionType.Division3_RecordsAndCertification
                        : (DivisionType?)null;

            var existing = _context.UserRoleSettings.FirstOrDefault(r => r.TransactionStatusId == status.TransactionStatusId);
            var suffix = status.DivisionId == 2 ? " (GIS)" : status.DivisionId == 3 ? " (Records)" : "";
            var roleName = $"{status.Name}{suffix}";

            if (existing == null)
            {
                _context.UserRoleSettings.Add(new UserRoleSetting
                {
                    Id = Guid.NewGuid(),
                    RoleName = roleName,
                    TransactionStatusId = status.TransactionStatusId,
                    Division = divType,
                    Description = $"Workflow step {status.TransactionStatusId}: {status.Name} for Division {status.DivisionId}.",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                });
            }
            else
            {
                existing.RoleName = roleName;
                existing.Division = divType;
            }
        }
        _context.SaveChanges();
    }

    public IEnumerable<UserRoleSetting> GetAll(string? search = null)
    {
        var query = _context.UserRoleSettings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.RoleName.ToLower().Contains(term)
                                  || (r.Description != null && r.Description.ToLower().Contains(term)));
        }

        var roles = query.OrderBy(r => r.Division).ThenBy(r => r.TransactionStatusId ?? 0).ThenBy(r => r.RoleName).ToList();

        // Populate Allowed Target Steps (Transfer to) from WorkflowSequence
        var sequences = _context.WorkflowSequences.ToList();
        var allStatuses = _context.TransactionStatuses.ToList();

        foreach (var r in roles)
        {
            if (r.TransactionStatusId.HasValue)
            {
                var targetIds = sequences
                    .Where(s => s.CurrentStatusId == r.TransactionStatusId.Value && s.NextStatusId.HasValue)
                    .Select(s => s.NextStatusId!.Value)
                    .Distinct()
                    .ToList();

                r.AllowedTargetSteps = allStatuses
                    .Where(s => targetIds.Contains(s.TransactionStatusId))
                    .Select(s => s.Name)
                    .ToList();
            }
            else if (r.RoleName.Contains("Admin", StringComparison.OrdinalIgnoreCase))
            {
                r.AllowedTargetSteps = new List<string> { "All Target Steps (Global Administrator)" };
            }
        }

        return roles;
    }

    public UserRoleSetting? GetById(Guid id)
    {
        var role = _context.UserRoleSettings.FirstOrDefault(r => r.Id == id);
        if (role != null)
        {
            if (role.TransactionStatusId.HasValue)
            {
                var targetIds = _context.WorkflowSequences
                    .Where(s => s.CurrentStatusId == role.TransactionStatusId.Value && s.NextStatusId.HasValue)
                    .Select(s => s.NextStatusId!.Value)
                    .Distinct()
                    .ToList();

                role.AllowedTargetSteps = _context.TransactionStatuses
                    .Where(s => targetIds.Contains(s.TransactionStatusId))
                    .Select(s => s.Name)
                    .ToList();
            }
            else if (role.RoleName.Contains("Admin", StringComparison.OrdinalIgnoreCase))
            {
                role.AllowedTargetSteps = new List<string> { "All Target Steps (Global Administrator)" };
            }
        }
        return role;
    }

    public void Add(UserRoleSetting role)
    {
        if (role.Id == Guid.Empty)
        {
            role.Id = Guid.NewGuid();
        }
        role.CreatedDate = DateTime.Now;
        role.UpdatedDate = DateTime.Now;

        _context.UserRoleSettings.Add(role);
        _context.SaveChanges();
    }

    public void Update(UserRoleSetting role)
    {
        var existing = GetById(role.Id);
        if (existing != null)
        {
            existing.RoleName = role.RoleName;
            existing.Division = role.Division;
            existing.Description = role.Description;
            existing.IsActive = role.IsActive;
            existing.UpdatedDate = DateTime.Now;

            _context.SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var item = GetById(id);
        if (item != null)
        {
            _context.UserRoleSettings.Remove(item);
            _context.SaveChanges();
        }
    }

    public int GetTotalCount()
    {
        return _context.UserRoleSettings.Count();
    }
}
