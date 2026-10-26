using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

public enum UserRole
{
    [Display(Name = "System Administrator")]
    SystemAdmin,

    [Display(Name = "City Assessor")]
    CityAssessor,

    [Display(Name = "Assessment Officer")]
    AssessmentOfficer,

    [Display(Name = "Property Appraiser")]
    Appraiser,

    [Display(Name = "Tax Mapper")]
    TaxMapper,

    [Display(Name = "Assessment Clerk")]
    AssessmentClerk
}

public class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [ValidateNever]
    [Display(Name = "Hashed Password")]
    public string PasswordHash { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Account Password")]
    [ValidateNever]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Employee Number is required.")]
    [Display(Name = "Employee No.")]
    public string EmployeeNo { get; set; } = string.Empty;

    [Display(Name = "User Position")]
    public UserRole Role { get; set; } = UserRole.AssessmentClerk;

    [Display(Name = "Office Division")]
    public int? DivisionId { get; set; }

    [Display(Name = "User Role (Workflow Step)")]
    public int? WorkflowRoleId { get; set; }

    [Display(Name = "Workflow Role")]
    public string? WorkflowRoleName { get; set; }

    [Display(Name = "Department / Section")]
    public string Department { get; set; } = "Real Property Assessment Division";

    [Display(Name = "Contact Phone No.")]
    public string ContactNo { get; set; } = string.Empty;

    [Display(Name = "Account Active Status")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Account Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Last Login Timestamp")]
    public DateTime? LastLoginDate { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string DivisionName => DivisionId switch
    {
        1 => "Division 1: Appraisal and Assessment",
        2 => "Division 2: Tax Mapping",
        3 => "Division 3: Records",
        _ => "General / Unassigned"
    };
}
