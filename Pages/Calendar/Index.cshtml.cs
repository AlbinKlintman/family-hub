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
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ScheduleId { get; set; }

    public int DisplayYear { get; private set; }
    public int DisplayMonth { get; private set; }
    public Dictionary<DateOnly, List<CalendarEvent>> EventsByDate { get; private set; } = new();
    public List<List<DateOnly?>> Weeks { get; private set; } = new();
    public List<Schedule> Schedules { get; private set; } = [];
    public Schedule? CurrentSchedule { get; private set; }

    public (int Year, int Month) PreviousMonth => AddMonths(-1);
    public (int Year, int Month) NextMonth => AddMonths(1);

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        DisplayYear = Year ?? today.Year;
        DisplayMonth = Month ?? today.Month;

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

        EventsByDate = await CalendarEventProvider.GetEventsForMonthAsync(context, userId, DisplayYear, DisplayMonth, ScheduleId);

        Weeks = CalendarEventProvider.BuildWeeks(DisplayYear, DisplayMonth);
    }

    private (int Year, int Month) AddMonths(int delta)
    {
        var date = new DateOnly(DisplayYear, DisplayMonth, 1).AddMonths(delta);
        return (date.Year, date.Month);
    }
}
