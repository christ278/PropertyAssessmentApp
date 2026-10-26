using System.ComponentModel.DataAnnotations;

namespace PropertyAssessmentApp.Models;

public class OrderPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Display(Name = "Fee / Account Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Particular / Fee Description")]
    public string Particular { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 1000000.00, ErrorMessage = "Unit Cost must be greater than zero.")]
    [Display(Name = "Unit Cost (₱)")]
    public decimal UnitCost { get; set; }

    [Display(Name = "Fee Category")]
    public string Category { get; set; } = "Certification";

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Updated Date")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;
}
