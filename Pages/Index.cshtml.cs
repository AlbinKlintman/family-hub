using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    private const int UpcomingRangeDays = 7;

    /// <summary>Null when there's no fasting note for today at all (not the same as an explicit NoFast entry).</summary>
    public FastingLevel? TodayFastingLevel { get; private set; }

    public List<Note> NotesDueSoon { get; private set; } = [];
    public int ApplicationsInProgressCount { get; private set; }
    public JobApplication? NextInterview { get; private set; }
    public List<CalendarEvent> UpcomingEvents { get; private set; } = [];

    public async Task OnGetAsync()
    {
        if (userManager.GetUserId(User) is not { } userId)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Now);

        var todayFasting = await context.Notes.OfType<FastingNote>()
            .FirstOrDefaultAsync(n => n.UserId == userId && n.Day == today);

        TodayFastingLevel = todayFasting?.Level;

        var openNotes = await context.Notes
            .Where(n => n.UserId == userId && !n.IsDone)
            .ToListAsync();

        var rangeEnd = today.AddDays(UpcomingRangeDays - 1);
        NotesDueSoon = openNotes
            .Select(n => new { Note = n, Date = Notes.IndexModel.SortDate(n) })
            .Where(x => x.Date != DateTime.MaxValue && x.Date.Date <= rangeEnd.ToDateTime(TimeOnly.MaxValue))
            .OrderBy(x => x.Date)
            .Take(5)
            .Select(x => x.Note)
            .ToList();

        var applications = await context.JobApplications
            .Where(a => a.UserId == userId)
            .ToListAsync();

        ApplicationsInProgressCount = applications.Count(a => a.Status != ApplicationStatus.Rejected);
        NextInterview = applications
            .Where(a => a.Status != ApplicationStatus.Rejected && a.InterviewDate >= today)
            .OrderBy(a => a.InterviewDate)
            .ThenBy(a => a.InterviewTime)
            .FirstOrDefault();

        var eventsByDate = await CalendarEventProvider.GetEventsForRangeAsync(context, userId, today, rangeEnd);
        UpcomingEvents = eventsByDate
            .SelectMany(kvp => kvp.Value)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.TimeLabel)
            .Take(5)
            .ToList();
    }

    internal static string NoteDisplayText(Note note) => note switch
    {
        ToDoNote { Title: { Length: > 0 } title } => title,
        ToDoNote => "To-Do",
        LaundryNote l => l.LaundryType.ToDisplayName(),
        WorkShiftNote w => w.Location,
        FastingNote f => f.Level.ToShortLabel(),
        _ => "Note"
    };
}
