using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Notes;

public class FastingCalendarModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    [BindProperty(SupportsGet = true)]
    public FastingLevel Level { get; set; } = FastingLevel.Meat;

    [BindProperty]
    public List<int> CheckedDays { get; set; } = [];

    public int DisplayYear { get; private set; }
    public int DisplayMonth { get; private set; }
    public List<List<DateOnly?>> Weeks { get; private set; } = [];

    /// <summary>Day-of-month -> the level already assigned, for every day in the month that has any fasting entry.</summary>
    public Dictionary<int, FastingLevel> ExistingLevelsByDay { get; private set; } = [];

    public (int Year, int Month) PreviousMonth => AddMonths(-1);
    public (int Year, int Month) NextMonth => AddMonths(1);

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        DisplayYear = Year ?? today.Year;
        DisplayMonth = Month ?? today.Month;

        await LoadMonthAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        DisplayYear = Year ?? today.Year;
        DisplayMonth = Month ?? today.Month;

        var userId = userManager.GetUserId(User)!;
        var start = new DateOnly(DisplayYear, DisplayMonth, 1);
        var end = start.AddMonths(1);

        var existing = await context.Notes.OfType<FastingNote>()
            .Where(n => n.UserId == userId && n.Day >= start && n.Day < end)
            .ToListAsync();
        var existingByDay = existing.ToDictionary(n => n.Day.Day);

        var checkedSet = CheckedDays.ToHashSet();
        var daysInMonth = DateTime.DaysInMonth(DisplayYear, DisplayMonth);

        for (var day = 1; day <= daysInMonth; day++)
        {
            existingByDay.TryGetValue(day, out var note);
            var action = DecidePaintAction(checkedSet.Contains(day), Level, note?.Level);

            switch (action)
            {
                case PaintAction.Create:
                    context.Notes.Add(new FastingNote { UserId = userId, Day = new DateOnly(DisplayYear, DisplayMonth, day), Level = Level });
                    break;
                case PaintAction.Update:
                    note!.Level = Level;
                    break;
                case PaintAction.Delete:
                    context.Notes.Remove(note!);
                    break;
                case PaintAction.NoOp:
                    break;
            }
        }

        await context.SaveChangesAsync();

        return RedirectToPage(new { Year = DisplayYear, Month = DisplayMonth, Level });
    }

    private async Task LoadMonthAsync()
    {
        var userId = userManager.GetUserId(User)!;
        var start = new DateOnly(DisplayYear, DisplayMonth, 1);
        var end = start.AddMonths(1);

        var existing = await context.Notes.OfType<FastingNote>()
            .Where(n => n.UserId == userId && n.Day >= start && n.Day < end)
            .ToListAsync();

        ExistingLevelsByDay = existing.ToDictionary(n => n.Day.Day, n => n.Level);
        Weeks = CalendarEventProvider.BuildWeeks(DisplayYear, DisplayMonth);
    }

    private (int Year, int Month) AddMonths(int delta)
    {
        var date = new DateOnly(DisplayYear, DisplayMonth, 1).AddMonths(delta);
        return (date.Year, date.Month);
    }

    internal enum PaintAction { NoOp, Create, Update, Delete }

    /// <summary>
    /// A day is "checked" iff the currently posted form checked it. That's compared
    /// against whatever level (if any) the day already has assigned:
    ///  - checked + no existing note -> Create
    ///  - checked + existing note at a different level -> Update (overwrite)
    ///  - checked + existing note already at this level -> NoOp
    ///  - unchecked + existing note at *this* level -> Delete (paint it off)
    ///  - unchecked + existing note at a *different* level -> NoOp (never touch other levels)
    ///  - unchecked + no existing note -> NoOp
    /// </summary>
    internal static PaintAction DecidePaintAction(bool isChecked, FastingLevel selectedLevel, FastingLevel? existingLevel)
    {
        if (isChecked)
        {
            if (existingLevel is null)
            {
                return PaintAction.Create;
            }

            return existingLevel == selectedLevel ? PaintAction.NoOp : PaintAction.Update;
        }

        return existingLevel == selectedLevel ? PaintAction.Delete : PaintAction.NoOp;
    }
}
