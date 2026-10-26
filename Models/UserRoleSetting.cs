using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class UserRoleSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Role Name is required.")]
    [Display(Name = "Role Name / Title")]
    public string RoleName { get; set; } = string.Empty;

    [Display(Name = "Workflow Step ID")]
    public int? TransactionStatusId { get; set; }

    [Display(Name = "Division Assignment")]
    public DivisionType? Division { get; set; }

    [Display(Name = "Role Description & Responsibilities")]
    public string? Description { get; set; }

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Updated Date")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<string> AllowedTargetSteps { get; set; } = new();
}
