using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Calendar;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateOnly? Anchor { get; set; }

    [BindProperty(SupportsGet = true)]
    public CalendarViewMode Mode { get; set; } = CalendarViewMode.Month;

    /// <summary>true = rolling window starting today/anchor; false = the full calendar-aligned week/month.</summary>
    [BindProperty(SupportsGet = true)]
    public bool Rolling { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public int? ScheduleId { get; set; }

    public DateOnly DisplayAnchor { get; private set; }
    public DateOnly RangeStart { get; private set; }
    public DateOnly RangeEnd { get; private set; }
    public string PeriodLabel { get; private set; } = "";

    /// <summary>Every day in the range, in order -- used for both the full-week single row and the rolling agenda list.</summary>
    public List<DateOnly> Days { get; private set; } = [];

    /// <summary>Only populated for the full-month grid, which needs leading/trailing blanks to align columns.</summary>
    public List<List<DateOnly?>> Weeks { get; private set; } = [];

    public Dictionary<DateOnly, List<CalendarEvent>> EventsByDate { get; private set; } = new();
    public List<Schedule> Schedules { get; private set; } = [];
    public Schedule? CurrentSchedule { get; private set; }

    public DateOnly PreviousAnchor => Mode == CalendarViewMode.Week ? DisplayAnchor.AddDays(-7) : DisplayAnchor.AddMonths(-1);
    public DateOnly NextAnchor => Mode == CalendarViewMode.Week ? DisplayAnchor.AddDays(7) : DisplayAnchor.AddMonths(1);

    public async Task OnGetAsync()
    {
        DisplayAnchor = Anchor ?? DateOnly.FromDateTime(DateTime.Now);

        var userId = userManager.GetUserId(User)!;

        Schedules = await context.Schedules.Where(s => s.UserId == userId).OrderBy(s => s.Name).ToListAsync();

        if (ScheduleId is not null)
        {
            CurrentSchedule = Schedules.FirstOrDefault(s => s.Id == ScheduleId);
            if (CurrentSchedule is null)
            {
                ScheduleId = null;
            }
        }

        var range = ComputeRange(Mode, Rolling, DisplayAnchor);
        RangeStart = range.Start;
        RangeEnd = range.End;
        PeriodLabel = range.Label;

        if (Mode == CalendarViewMode.Month && !Rolling)
        {
            Weeks = CalendarEventProvider.BuildWeeks(DisplayAnchor.Year, DisplayAnchor.Month);
        }
        else
        {
            Days = Enumerable.Range(0, RangeEnd.DayNumber - RangeStart.DayNumber + 1)
                .Select(RangeStart.AddDays)
                .ToList();
        }

        EventsByDate = await CalendarEventProvider.GetEventsForRangeAsync(context, userId, RangeStart, RangeEnd, ScheduleId);
    }

    internal readonly record struct CalendarRange(DateOnly Start, DateOnly End, string Label);

    internal static CalendarRange ComputeRange(CalendarViewMode mode, bool rolling, DateOnly anchor)
    {
        if (mode == CalendarViewMode.Week)
        {
            if (rolling)
            {
                var end = anchor.AddDays(6);
                return new CalendarRange(anchor, end, $"{anchor:MMM d} – {end:MMM d}");
            }

            var weekDays = CalendarEventProvider.BuildWeekDays(anchor);
            var weekStart = weekDays[0];
            var weekNumber = ISOWeek.GetWeekOfYear(weekStart);
            return new CalendarRange(weekStart, weekDays[^1], $"Week {weekNumber}, {weekStart.Year}");
        }

        if (rolling)
        {
            var end = anchor.AddDays(30);
            return new CalendarRange(anchor, end, $"{anchor:MMM d} – {end:MMM d}");
        }

        var monthStart = new DateOnly(anchor.Year, anchor.Month, 1);
        return new CalendarRange(monthStart, monthStart.AddMonths(1).AddDays(-1), monthStart.ToString("MMMM yyyy"));
    }
}
