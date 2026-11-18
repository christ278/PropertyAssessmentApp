using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAssessmentApp.Models;

[Table("OfficeScheduleSettings")]
public class OfficeScheduleSetting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Display(Name = "Office Start Time")]
    public TimeSpan WorkStartTime { get; set; } = new TimeSpan(8, 0, 0);

    [Display(Name = "Office End Time")]
    public TimeSpan WorkEndTime { get; set; } = new TimeSpan(17, 0, 0);

    [Display(Name = "Enable Breaktime")]
    public bool HasBreakTime { get; set; } = true;

    [Display(Name = "Break Start Time")]
    public TimeSpan BreakStartTime { get; set; } = new TimeSpan(12, 0, 0);

    [Display(Name = "Break End Time")]
    public TimeSpan BreakEndTime { get; set; } = new TimeSpan(13, 0, 0);

    public bool WorkMonday { get; set; } = true;
    public bool WorkTuesday { get; set; } = true;
    public bool WorkWednesday { get; set; } = true;
    public bool WorkThursday { get; set; } = true;
    public bool WorkFriday { get; set; } = true;
    public bool WorkSaturday { get; set; } = false;
    public bool WorkSunday { get; set; } = false;

    [Display(Name = "Pause Clock When Deferred")]
    public bool PauseWhenDeferred { get; set; } = true;

    public DateTime UpdatedDate { get; set; } = DateTime.Now;
}

[Table("OfficeHolidays")]
public class OfficeHoliday
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime HolidayDate { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public bool IsRecurringYearly { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

public class TransactionSlaStatusDto
{
    public int TargetMinutes { get; set; }
    public double ElapsedWorkingMinutes { get; set; }
    public double RemainingWorkingMinutes { get; set; }
    public int StageTargetMinutes { get; set; }
    public double StageElapsedWorkingMinutes { get; set; }
    public string SlaState { get; set; } = "NoTarget"; // OnTrack, DueSoon, Overdue, NoTarget
    public bool IsCurrentlyPaused { get; set; }
    public string? PauseReason { get; set; }
    public string SummaryText { get; set; } = string.Empty;
    public string StageSummaryText { get; set; } = string.Empty;
}
