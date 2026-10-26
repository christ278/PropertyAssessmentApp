using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("WorkflowSequence")]
public class WorkflowSequence
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SequenceId { get; set; }

    public int TransactionTypeId { get; set; }

    public int CurrentStatusId { get; set; }

    public int? NextStatusId { get; set; }

    public bool IsApproval { get; set; } = false;
}
