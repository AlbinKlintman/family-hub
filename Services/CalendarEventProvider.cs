using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

public record CalendarEvent(DateOnly Date, string Title, string Category, string? TimeLabel, string EditUrl, bool IsDone = false);

public static class CalendarEventProvider
{
    public static Task<Dictionary<DateOnly, List<CalendarEvent>>> GetEventsForMonthAsync(
        ApplicationDbContext context, string userId, int year, int month, int? scheduleId = null)
    {
        var start = new DateOnly(year, month, 1);
        return GetEventsForRangeAsync(context, userId, start, start.AddMonths(1).AddDays(-1), scheduleId);
    }

    /// <summary>endInclusive is the last day shown, e.g. a 7-day week passes start and start.AddDays(6).</summary>
    public static async Task<Dictionary<DateOnly, List<CalendarEvent>>> GetEventsForRangeAsync(
        ApplicationDbContext context, string userId, DateOnly start, DateOnly endInclusive, int? scheduleId = null)
    {
        var end = endInclusive.AddDays(1);

        var events = new List<CalendarEvent>();

        var todos = await LoadNotesInRangeAsync<ToDoNote>(context, userId, start, end, scheduleId, n => n.DueDate);
        events.AddRange(todos.Select(t => new CalendarEvent(
            t.DueDate!.Value,
            string.IsNullOrWhiteSpace(t.Title) ? "To-do" : t.Title,
            "todo",
            t.DueTime?.ToString("HH:mm"),
            $"/Notes/Edit/{t.Id}",
            t.IsDone)));

        var laundry = await LoadNotesInRangeAsync<LaundryNote>(context, userId, start, end, scheduleId, n => n.Day);
        events.AddRange(laundry.Select(l => new CalendarEvent(
            l.Day!.Value,
            $"{l.LaundryType.ToDisplayName()} · {l.Room.ToDisplayName()}",
            "laundry",
            l.TimeWindow.ToDisplayName(),
            $"/Notes/Edit/{l.Id}",
            l.IsDone)));

        var shifts = await LoadNotesInRangeAsync<WorkShiftNote>(context, userId, start, end, scheduleId, n => n.Day);
        events.AddRange(shifts.Select(s => new CalendarEvent(
            s.Day!.Value,
            s.Location,
            "workshift",
            $"{s.StartTime.ToString("HH:mm")}-{s.EndTime.ToString("HH:mm")}",
            $"/Notes/Edit/{s.Id}",
            s.IsDone)));

        var fasts = await LoadNotesInRangeAsync<FastingNote>(context, userId, start, end, scheduleId, n => n.Day);
        events.AddRange(fasts.Select(f => new CalendarEvent(
            f.Day, f.Level.ToShortLabel(), "fasting", null, $"/Notes/Edit/{f.Id}", f.IsDone)));

        var appliedQuery = context.JobApplications
            .Where(a => a.UserId == userId && a.AppliedDate != null && a.AppliedDate >= start && a.AppliedDate < end);
        var interviewQuery = context.JobApplications
            .Where(a => a.UserId == userId && a.InterviewDate != null && a.InterviewDate >= start && a.InterviewDate < end);

        if (scheduleId is not null)
        {
            appliedQuery = appliedQuery.Where(a => a.ScheduleId == scheduleId);
            interviewQuery = interviewQuery.Where(a => a.ScheduleId == scheduleId);
        }

        var applied = await appliedQuery.ToListAsync();
        events.AddRange(applied.Select(a => new CalendarEvent(
            a.AppliedDate!.Value, $"Applied: {a.RoleName}", "application", null, $"/Applications/Edit/{a.Id}")));

        var interviews = await interviewQuery.ToListAsync();
        events.AddRange(interviews.Select(a => new CalendarEvent(
            a.InterviewDate!.Value, $"Interview: {a.RoleName}", "application", null, $"/Applications/Edit/{a.Id}")));

        return events
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.TimeLabel).ToList());
    }

    /// <summary>
    /// Loads a user's own notes of one type, plus any of that type shared with
    /// them, in one range. For a shared note, the viewer's own overlay
    /// (Folder/Schedule from NoteShare, not the owner's) decides whether it
    /// counts as "belonging" to scheduleId -- same rule as owned notes: tagged
    /// directly, or filed in a folder linked to it.
    /// </summary>
    private static async Task<List<TNote>> LoadNotesInRangeAsync<TNote>(
        ApplicationDbContext context, string userId, DateOnly start, DateOnly end, int? scheduleId, Func<TNote, DateOnly?> dayOf)
        where TNote : Note
    {
        var sharedScheduleIds = await NoteVisibilityProvider.GetVisibleScheduleIdsAsync(context, userId);

        var notes = await context.Notes.OfType<TNote>()
            .Include(n => n.Folder)
            .Include(n => n.Shares.Where(s => s.SharedWithUserId == userId))
                .ThenInclude(s => s.Folder)
            .Where(NoteVisibilityProvider.VisibleTo<TNote>(userId, sharedScheduleIds))
            .ToListAsync();

        // No NoteShare row is normal here -- this note may only be visible via a shared schedule.
        foreach (var note in notes.Where(n => n.UserId != userId))
        {
            var share = note.Shares.FirstOrDefault(s => s.SharedWithUserId == userId);
            note.ScheduleId = share?.ScheduleId;
            note.Folder = share?.Folder;
        }

        var inRange = notes.Where(n => dayOf(n) is { } day && day >= start && day < end);

        return (scheduleId is null
            ? inRange
            : inRange.Where(n => n.ScheduleId == scheduleId || (n.Folder != null && n.Folder.ScheduleId == scheduleId))
        ).ToList();
    }

    /// <summary>
    /// Builds a Monday-first month grid, padded with nulls so every week has 7 slots.
    /// </summary>
    public static List<List<DateOnly?>> BuildWeeks(int year, int month)
    {
        var firstOfMonth = new DateOnly(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var mondayOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;

        var days = new List<DateOnly?>();
        for (var i = 0; i < mondayOffset; i++)
        {
            days.Add(null);
        }
        for (var d = 1; d <= daysInMonth; d++)
        {
            days.Add(new DateOnly(year, month, d));
        }
        while (days.Count % 7 != 0)
        {
            days.Add(null);
        }

        return days.Chunk(7).Select(week => week.ToList()).ToList();
    }

    /// <summary>The Monday-Sunday calendar week containing the given date.</summary>
    public static List<DateOnly> BuildWeekDays(DateOnly anyDayInWeek)
    {
        var mondayOffset = ((int)anyDayInWeek.DayOfWeek + 6) % 7;
        var monday = anyDayInWeek.AddDays(-mondayOffset);
        return Enumerable.Range(0, 7).Select(monday.AddDays).ToList();
    }
}
