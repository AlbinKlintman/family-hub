using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

public record CalendarEvent(DateOnly Date, string Title, string Category, string? TimeLabel);

public static class CalendarEventProvider
{
    public static async Task<Dictionary<DateOnly, List<CalendarEvent>>> GetEventsForMonthAsync(
        ApplicationDbContext context, string userId, int year, int month, int? scheduleId = null)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var events = new List<CalendarEvent>();

        var todos = await FilterBySchedule(
                context.Notes.OfType<ToDoNote>().Where(n => n.UserId == userId && n.DueDate != null && n.DueDate >= start && n.DueDate < end),
                scheduleId)
            .ToListAsync();
        events.AddRange(todos.Select(t => new CalendarEvent(
            t.DueDate!.Value,
            string.IsNullOrWhiteSpace(t.Title) ? "To-do" : t.Title,
            "todo",
            t.DueTime?.ToString("HH:mm"))));

        var laundry = await FilterBySchedule(
                context.Notes.OfType<LaundryNote>().Where(n => n.UserId == userId && n.Day != null && n.Day >= start && n.Day < end),
                scheduleId)
            .ToListAsync();
        events.AddRange(laundry.Select(l => new CalendarEvent(
            l.Day!.Value,
            $"{l.LaundryType.ToDisplayName()} · {l.Room.ToDisplayName()}",
            "laundry",
            l.TimeWindow.ToDisplayName())));

        var shifts = await FilterBySchedule(
                context.Notes.OfType<WorkShiftNote>().Where(n => n.UserId == userId && n.Day != null && n.Day >= start && n.Day < end),
                scheduleId)
            .ToListAsync();
        events.AddRange(shifts.Select(s => new CalendarEvent(
            s.Day!.Value,
            s.Location,
            "workshift",
            $"{s.StartTime.ToString("HH:mm")}-{s.EndTime.ToString("HH:mm")}")));

        var fasts = await FilterBySchedule(
                context.Notes.OfType<FastingNote>().Where(n => n.UserId == userId && n.Day >= start && n.Day < end),
                scheduleId)
            .ToListAsync();
        events.AddRange(fasts.Select(f => new CalendarEvent(
            f.Day, f.Level.ToShortLabel(), "fasting", null)));

        if (scheduleId is null)
        {
            var applied = await context.JobApplications
                .Where(a => a.UserId == userId && a.AppliedDate != null && a.AppliedDate >= start && a.AppliedDate < end)
                .ToListAsync();
            events.AddRange(applied.Select(a => new CalendarEvent(
                a.AppliedDate!.Value, $"Applied: {a.RoleName}", "application", null)));

            var interviews = await context.JobApplications
                .Where(a => a.UserId == userId && a.InterviewDate != null && a.InterviewDate >= start && a.InterviewDate < end)
                .ToListAsync();
            events.AddRange(interviews.Select(a => new CalendarEvent(
                a.InterviewDate!.Value, $"Interview: {a.RoleName}", "application", null)));
        }

        return events
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.TimeLabel).ToList());
    }

    /// <summary>
    /// A note counts as belonging to a schedule if it's tagged with it directly,
    /// or it's filed in a folder that's linked to it.
    /// </summary>
    private static IQueryable<TNote> FilterBySchedule<TNote>(IQueryable<TNote> query, int? scheduleId) where TNote : Note
    {
        if (scheduleId is null)
        {
            return query;
        }

        return query.Where(n => n.ScheduleId == scheduleId || (n.Folder != null && n.Folder.ScheduleId == scheduleId));
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
}
