using System.Collections.Generic;

namespace PropertyAssessmentApp.Models;

public class WorkflowSequenceDto
{
    public int CurrentStatusId { get; set; }
    public int? NextStatusId { get; set; }
    public bool IsApproval { get; set; }
}

public class UpdateWorkflowSettingsRequest
{
    public int TransactionTypeId { get; set; }
    public List<WorkflowSequenceDto> Sequences { get; set; } = new();
}
