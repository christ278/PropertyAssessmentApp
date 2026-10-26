using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("RoleModulePermission")]
public class RoleModulePermission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PermissionId { get; set; }

    public Guid RoleId { get; set; }

    public int ModuleId { get; set; }

    public bool CanView { get; set; } = false;

    public bool CanCreate { get; set; } = false;

    public bool CanEdit { get; set; } = false;

    public bool CanDelete { get; set; } = false;

    public bool CanApprove { get; set; } = false;

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    [ForeignKey("ModuleId")]
    public virtual SystemModule? SystemModule { get; set; }
}
