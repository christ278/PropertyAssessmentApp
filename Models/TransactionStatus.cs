using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("TransactionStatus")]
public class TransactionStatus
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int TransactionStatusId { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public bool IsComplete { get; set; } = false;

    public int DivisionId { get; set; } = 1;
}
