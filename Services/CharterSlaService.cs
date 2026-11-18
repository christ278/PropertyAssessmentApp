using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface ICharterSlaService
{
    OfficeScheduleSetting GetScheduleSetting();
    void UpdateScheduleSetting(OfficeScheduleSetting updated);
    List<OfficeHoliday> GetHolidays(bool activeOnly = false);
    OfficeHoliday SaveHoliday(OfficeHoliday holiday);
    bool DeleteHoliday(int id);
    bool ToggleHoliday(int id);

    string? GetHolidayName(DateTime date, IReadOnlyCollection<OfficeHoliday> holidays);
    bool IsWorkingDay(DateTime date, OfficeScheduleSetting setting, IReadOnlyCollection<OfficeHoliday> holidays);
    string? GetClockPauseReason(DateTime timestamp, OfficeScheduleSetting setting, IReadOnlyCollection<OfficeHoliday> holidays, bool isDeferred = false);

    double CalculateWorkingMinutes(DateTime start, DateTime end, OfficeScheduleSetting setting, IReadOnlyCollection<OfficeHoliday> holidays);

    TransactionSlaStatusDto EvaluateTransactionSla(
        int transactionTypeId,
        DateTime dateSubmitted,
        WorkflowStage currentStage,
        bool isFinished,
        bool isDeferred,
        DateTime? finishedDate,
        IReadOnlyList<DocumentForwardingLog> logs,
        OfficeScheduleSetting? cachedSetting = null,
        IReadOnlyCollection<OfficeHoliday>? cachedHolidays = null,
        IReadOnlyCollection<WorkflowSequence>? cachedSequences = null,
        int? divisionCharterMinutes = null);

    string FormatDuration(double minutes);
}

public class CharterSlaService : ICharterSlaService
{
    private readonly ApplicationDbContext _context;

    public CharterSlaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public OfficeScheduleSetting GetScheduleSetting()
    {
        var setting = _context.OfficeScheduleSettings.OrderBy(s => s.Id).FirstOrDefault();
        if (setting == null)
        {
            setting = new OfficeScheduleSetting
            {
                WorkStartTime = new TimeSpan(8, 0, 0),
                WorkEndTime = new TimeSpan(17, 0, 0),
                HasBreakTime = true,
                BreakStartTime = new TimeSpan(12, 0, 0),
                BreakEndTime = new TimeSpan(13, 0, 0),
                WorkMonday = true,
                WorkTuesday = true,
                WorkWednesday = true,
                WorkThursday = true,
                WorkFriday = true,
                WorkSaturday = false,
                WorkSunday = false,
                PauseWhenDeferred = true,
                UpdatedDate = DateTime.Now
            };
            _context.OfficeScheduleSettings.Add(setting);
            _context.SaveChanges();
        }
        return setting;
    }

    public void UpdateScheduleSetting(OfficeScheduleSetting updated)
    {
        var setting = GetScheduleSetting();
        setting.WorkStartTime = updated.WorkStartTime;
        setting.WorkEndTime = updated.WorkEndTime;
        setting.HasBreakTime = updated.HasBreakTime;
        setting.BreakStartTime = updated.BreakStartTime;
        setting.BreakEndTime = updated.BreakEndTime;
        setting.WorkMonday = updated.WorkMonday;
        setting.WorkTuesday = updated.WorkTuesday;
        setting.WorkWednesday = updated.WorkWednesday;
        setting.WorkThursday = updated.WorkThursday;
        setting.WorkFriday = updated.WorkFriday;
        setting.WorkSaturday = updated.WorkSaturday;
        setting.WorkSunday = updated.WorkSunday;
        setting.PauseWhenDeferred = updated.PauseWhenDeferred;
        setting.UpdatedDate = DateTime.Now;
        _context.SaveChanges();
    }

    public List<OfficeHoliday> GetHolidays(bool activeOnly = false)
    {
        var query = _context.OfficeHolidays.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(h => h.IsActive);
        return query.OrderBy(h => h.HolidayDate).ToList();
    }

    public OfficeHoliday SaveHoliday(OfficeHoliday holiday)
    {
        var targetDate = holiday.HolidayDate.Date;
        if (holiday.Id > 0)
        {
            var existing = _context.OfficeHolidays.FirstOrDefault(h => h.Id == holiday.Id);
            if (existing != null)
            {
                existing.HolidayDate = targetDate;
                existing.Name = holiday.Name.Trim();
                existing.IsRecurringYearly = holiday.IsRecurringYearly;
                existing.IsActive = holiday.IsActive;
                _context.SaveChanges();
                return existing;
            }
        }

        // Upsert if same exact date already exists
        var sameDate = _context.OfficeHolidays.FirstOrDefault(h => h.HolidayDate.Date == targetDate);
        if (sameDate != null)
        {
            sameDate.Name = holiday.Name.Trim();
            sameDate.IsRecurringYearly = holiday.IsRecurringYearly;
            sameDate.IsActive = true;
            _context.SaveChanges();
            return sameDate;
        }

        holiday.HolidayDate = targetDate;
        holiday.Name = holiday.Name.Trim();
        holiday.IsActive = true;
        holiday.CreatedDate = DateTime.Now;
        _context.OfficeHolidays.Add(holiday);
        _context.SaveChanges();
        return holiday;
    }

    public bool DeleteHoliday(int id)
    {
        var existing = _context.OfficeHolidays.FirstOrDefault(h => h.Id == id);
        if (existing == null) return false;
        _context.OfficeHolidays.Remove(existing);
        _context.SaveChanges();
        return true;
    }

    public bool ToggleHoliday(int id)
    {
        var existing = _context.OfficeHolidays.FirstOrDefault(h => h.Id == id);
        if (existing == null) return false;
        existing.IsActive = !existing.IsActive;
        _context.SaveChanges();
        return true;
    }

    public string? GetHolidayName(DateTime date, IReadOnlyCollection<OfficeHoliday> holidays)
    {
        var d = date.Date;
        var match = holidays.FirstOrDefault(h =>
            h.IsActive &&
            (h.HolidayDate.Date == d || (h.IsRecurringYearly && h.HolidayDate.Month == d.Month && h.HolidayDate.Day == d.Day)));
        return match?.Name;
    }

    public bool IsWorkingDay(DateTime date, OfficeScheduleSetting setting, IReadOnlyCollection<OfficeHoliday> holidays)
    {
        bool dayEnabled = date.DayOfWeek switch
        {
            DayOfWeek.Monday => setting.WorkMonday,
            DayOfWeek.Tuesday => setting.WorkTuesday,
            DayOfWeek.Wednesday => setting.WorkWednesday,
            DayOfWeek.Thursday => setting.WorkThursday,
            DayOfWeek.Friday => setting.WorkFriday,
            DayOfWeek.Saturday => setting.WorkSaturday,
            DayOfWeek.Sunday => setting.WorkSunday,
            _ => false
        };

        if (!dayEnabled) return false;
        return GetHolidayName(date, holidays) == null;
    }

    public string? GetClockPauseReason(DateTime timestamp, OfficeScheduleSetting setting, IReadOnlyCollection<OfficeHoliday> holidays, bool isDeferred = false)
    {
        if (isDeferred && setting.PauseWhenDeferred)
            return "Deferred / On-Hold";

        var holidayName = GetHolidayName(timestamp, holidays);
        if (holidayName != null)
            return $"Holiday ({holidayName})";

        if (!IsWorkingDay(timestamp, setting, holidays))
            return "Non-Working Day";

        var t = timestamp.TimeOfDay;
        if (t < setting.WorkStartTime)
            return $"Before {DateTime.Today.Add(setting.WorkStartTime):h:mm tt}";

        if (t >= setting.WorkEndTime)
            return $"After {DateTime.Today.Add(setting.WorkEndTime):h:mm tt}";

        if (setting.HasBreakTime && t >= setting.BreakStartTime && t < setting.BreakEndTime)
            return $"Breaktime ({DateTime.Today.Add(setting.BreakStartTime):h:mm tt} - {DateTime.Today.Add(setting.BreakEndTime):h:mm tt})";

        return null;
    }

    public double CalculateWorkingMinutes(DateTime start, DateTime end, OfficeScheduleSetting setting, IReadOnlyCollection<OfficeHoliday> holidays)
    {
        if (end <= start || setting.WorkEndTime <= setting.WorkStartTime)
            return 0;

        double totalMinutes = 0;
        var currentDay = start.Date;
        var lastDay = end.Date;

        bool validBreak = setting.HasBreakTime &&
                          setting.BreakEndTime > setting.BreakStartTime &&
                          setting.BreakStartTime > setting.WorkStartTime &&
                          setting.BreakEndTime < setting.WorkEndTime;

        while (currentDay <= lastDay)
        {
            if (IsWorkingDay(currentDay, setting, holidays))
            {
                if (validBreak)
                {
                    totalMinutes += OverlapMinutes(start, end, currentDay + setting.WorkStartTime, currentDay + setting.BreakStartTime);
                    totalMinutes += OverlapMinutes(start, end, currentDay + setting.BreakEndTime, currentDay + setting.WorkEndTime);
                }
                else
                {
                    totalMinutes += OverlapMinutes(start, end, currentDay + setting.WorkStartTime, currentDay + setting.WorkEndTime);
                }
            }
            currentDay = currentDay.AddDays(1);
        }

        return Math.Round(totalMinutes, 1);
    }

    private static double OverlapMinutes(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
    {
        var s = aStart > bStart ? aStart : bStart;
        var e = aEnd < bEnd ? aEnd : bEnd;
        return e > s ? (e - s).TotalMinutes : 0;
    }

    public TransactionSlaStatusDto EvaluateTransactionSla(
        int transactionTypeId,
        DateTime dateSubmitted,
        WorkflowStage currentStage,
        bool isFinished,
        bool isDeferred,
        DateTime? finishedDate,
        IReadOnlyList<DocumentForwardingLog> logs,
        OfficeScheduleSetting? cachedSetting = null,
        IReadOnlyCollection<OfficeHoliday>? cachedHolidays = null,
        IReadOnlyCollection<WorkflowSequence>? cachedSequences = null,
        int? divisionCharterMinutes = null)
    {
        var setting = cachedSetting ?? GetScheduleSetting();
        var holidays = cachedHolidays ?? GetHolidays(activeOnly: true);
        var sequences = (cachedSequences ?? _context.WorkflowSequences.AsNoTracking().ToList())
            .Where(s => s.TransactionTypeId == transactionTypeId)
            .ToList();

        int stageSumMinutes = sequences.Where(s => s.EstimatedMinutes > 0).Sum(s => s.EstimatedMinutes);
        int targetMinutes = (divisionCharterMinutes.HasValue && divisionCharterMinutes.Value > 0)
            ? divisionCharterMinutes.Value
            : stageSumMinutes;
        int stageTargetMinutes = sequences
            .Where(s => s.CurrentStatusId == (int)currentStage && s.EstimatedMinutes > 0)
            .Select(s => s.EstimatedMinutes)
            .FirstOrDefault();

        var effectiveEnd = isFinished ? (finishedDate ?? DateTime.Now) : DateTime.Now;
        if (effectiveEnd < dateSubmitted) effectiveEnd = dateSubmitted;

        double totalWorking = CalculateWorkingMinutes(dateSubmitted, effectiveEnd, setting, holidays);
        if (setting.PauseWhenDeferred && logs.Count > 0)
        {
            double deferredMinutes = CalculateDeferredWorkingMinutes(logs, effectiveEnd, setting, holidays);
            totalWorking = Math.Max(0, Math.Round(totalWorking - deferredMinutes, 1));
        }

        // Determine when the document arrived at its current stage
        var orderedAsc = logs.OrderBy(l => l.Timestamp).ToList();
        var stageArrivalLog = orderedAsc.LastOrDefault(l => l.ToStage == currentStage && !l.IsDeferred);
        var stageStart = stageArrivalLog?.Timestamp ?? dateSubmitted;
        double stageWorking = CalculateWorkingMinutes(stageStart, effectiveEnd, setting, holidays);
        if (setting.PauseWhenDeferred && logs.Count > 0)
        {
            var stageLogs = orderedAsc.Where(l => l.Timestamp >= stageStart).ToList();
            double stageDeferred = CalculateDeferredWorkingMinutes(stageLogs, effectiveEnd, setting, holidays);
            stageWorking = Math.Max(0, Math.Round(stageWorking - stageDeferred, 1));
        }

        string? pauseReason = isFinished ? null : GetClockPauseReason(DateTime.Now, setting, holidays, isDeferred);

        string slaState = "NoTarget";
        double remaining = 0;
        if (targetMinutes > 0)
        {
            remaining = Math.Round(targetMinutes - totalWorking, 1);
            if (totalWorking > targetMinutes)
                slaState = "Overdue";
            else if (totalWorking >= targetMinutes * 0.7)
                slaState = "DueSoon";
            else
                slaState = "OnTrack";
        }

        string summaryText = targetMinutes > 0
            ? $"{FormatDuration(totalWorking)} / {FormatDuration(targetMinutes)}"
            : $"{FormatDuration(totalWorking)} elapsed";

        string stageSummaryText = stageTargetMinutes > 0
            ? $"{FormatDuration(stageWorking)} / {FormatDuration(stageTargetMinutes)}"
            : FormatDuration(stageWorking);

        return new TransactionSlaStatusDto
        {
            TargetMinutes = targetMinutes,
            ElapsedWorkingMinutes = totalWorking,
            RemainingWorkingMinutes = remaining,
            StageTargetMinutes = stageTargetMinutes,
            StageElapsedWorkingMinutes = stageWorking,
            SlaState = slaState,
            IsCurrentlyPaused = pauseReason != null,
            PauseReason = pauseReason,
            SummaryText = summaryText,
            StageSummaryText = stageSummaryText
        };
    }

    private double CalculateDeferredWorkingMinutes(
        IReadOnlyList<DocumentForwardingLog> logs,
        DateTime effectiveEnd,
        OfficeScheduleSetting setting,
        IReadOnlyCollection<OfficeHoliday> holidays)
    {
        var ordered = logs.OrderBy(l => l.Timestamp).ToList();
        double deferredMins = 0;
        DateTime? deferStart = null;

        foreach (var l in ordered)
        {
            bool becomesDeferred = l.IsDeferred || l.ActionTaken.Equals("Deferred", StringComparison.OrdinalIgnoreCase);
            if (becomesDeferred && deferStart == null)
            {
                deferStart = l.Timestamp;
            }
            else if (!becomesDeferred && deferStart != null)
            {
                deferredMins += CalculateWorkingMinutes(deferStart.Value, l.Timestamp, setting, holidays);
                deferStart = null;
            }
        }

        if (deferStart != null && effectiveEnd > deferStart.Value)
        {
            deferredMins += CalculateWorkingMinutes(deferStart.Value, effectiveEnd, setting, holidays);
        }

        return deferredMins;
    }

    public string FormatDuration(double minutes)
    {
        if (minutes < 1) return "0m";
        int totalMins = (int)Math.Round(minutes);
        int hours = totalMins / 60;
        int mins = totalMins % 60;
        if (hours == 0) return $"{mins}m";
        if (mins == 0) return $"{hours}h";
        return $"{hours}h {mins}m";
    }
}
