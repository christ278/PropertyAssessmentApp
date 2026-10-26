using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class WorkflowStageSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Division")]
    public DivisionType Division { get; set; }

    [Required]
    [Display(Name = "Step Order")]
    public int StepOrder { get; set; } = 1;

    [Required(ErrorMessage = "Stage / Step Name is required.")]
    [Display(Name = "Stage / Step Name")]
    public string StageName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Assigned Role is required.")]
    [Display(Name = "Assigned Role / Officer")]
    public string AssignedRole { get; set; } = string.Empty;

    [Display(Name = "Description / Action Instruction")]
    public string? Description { get; set; }

    [Display(Name = "Is Final Approval / Issuance Stage")]
    public bool IsApprovalStage { get; set; } = false;

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;
}
