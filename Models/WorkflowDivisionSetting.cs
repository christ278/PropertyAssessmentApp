using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class WorkflowDivisionSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Division")]
    public DivisionType Division { get; set; }

    [Required]
    [Display(Name = "Division Title")]
    public string DivisionName { get; set; } = string.Empty;

    [Display(Name = "Description / Office Scope")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Require Appraiser Review Stage")]
    public bool RequireAppraiserReview { get; set; } = true;

    [Display(Name = "Require Mapper Verification Stage")]
    public bool RequireMapperVerification { get; set; } = true;

    [Display(Name = "Require Head Assessor Final Approval")]
    public bool RequireHeadAssessorApproval { get; set; } = true;

    [Display(Name = "Default Turnaround SLA (Hours)")]
    public int SlaHoursTarget { get; set; } = 48;

    [Display(Name = "Auto-Notify Next Stage Officer")]
    public bool AutoNotifyNextStage { get; set; } = true;

    [Display(Name = "Updated Date")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;
}
