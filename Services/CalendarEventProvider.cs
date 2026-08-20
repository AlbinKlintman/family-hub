using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

public record CalendarEvent(DateOnly Date, string Title, string Category, string? TimeLabel);

public static class CalendarEventProvider
{
    public static async Task<Dictionary<DateOnly, List<CalendarEvent>>> GetEventsForMonthAsync(
        ApplicationDbContext context, string userId, int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var events = new List<CalendarEvent>();

        var todos = await context.Notes.OfType<ToDoNote>()
            .Where(n => n.UserId == userId && n.DueDate != null && n.DueDate >= start && n.DueDate < end)
            .ToListAsync();
        events.AddRange(todos.Select(t => new CalendarEvent(
            t.DueDate!.Value,
            string.IsNullOrWhiteSpace(t.Title) ? "To-do" : t.Title,
            "todo",
            t.DueTime?.ToString("HH:mm"))));

        var laundry = await context.Notes.OfType<LaundryNote>()
            .Where(n => n.UserId == userId && n.Day != null && n.Day >= start && n.Day < end)
            .ToListAsync();
        events.AddRange(laundry.Select(l => new CalendarEvent(
            l.Day!.Value,
            $"{l.LaundryType.ToDisplayName()} · {l.Room.ToDisplayName()}",
            "laundry",
            l.TimeWindow.ToDisplayName())));

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

        return events
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.TimeLabel).ToList());
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
