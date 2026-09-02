using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;

namespace WebApp.Pages.Notes;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    /// <summary>Sentinel FolderId/ScheduleId value meaning "show only notes with none set" (real ids are always positive).</summary>
    public const int NoneSentinel = -1;

    public List<Note> Notes { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? FolderId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ScheduleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowCompleted { get; set; }

    [BindProperty(SupportsGet = true)]
    public NoteType? NoteType { get; set; }

    [BindProperty(SupportsGet = true)]
    public NoteSortOrder Sort { get; set; } = NoteSortOrder.DueDate;

    [BindProperty(SupportsGet = true)]
    public NoteDueFilter? DueFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    /// <summary>Posted back by the ToggleDone form so it can return to the same filtered list + scroll spot, same mechanism as Edit's ReturnUrl.</summary>
    [BindProperty]
    public string? ReturnUrl { get; set; }

    public Folder? CurrentFolder { get; set; }
    public Schedule? CurrentSchedule { get; set; }
    public SelectList FolderOptions { get; set; } = default!;
    public SelectList ScheduleOptions { get; set; } = default!;
    public string SummaryText { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (FolderId is not null && FolderId != NoneSentinel)
        {
            CurrentFolder = await context.Folders.FirstOrDefaultAsync(f => f.Id == FolderId && f.UserId == userId);
            if (CurrentFolder is null)
            {
                FolderId = null;
            }
        }

        if (ScheduleId is not null && ScheduleId != NoneSentinel)
        {
            CurrentSchedule = await context.Schedules.FirstOrDefaultAsync(s => s.Id == ScheduleId && s.UserId == userId);
            if (CurrentSchedule is null)
            {
                ScheduleId = null;
            }
        }

        var notesQuery = context.Notes
            .Include(n => n.Folder)
            .Include(n => n.Schedule)
            .Include(n => (n as WorkShiftNote)!.Colleagues)
            .Include(n => (n as ToDoNote)!.Reminders)
            .Where(n => n.UserId == userId);

        if (FolderId == NoneSentinel)
        {
            notesQuery = notesQuery.Where(n => n.FolderId == null);
        }
        else if (FolderId is not null)
        {
            notesQuery = notesQuery.Where(n => n.FolderId == FolderId);
        }

        if (ScheduleId == NoneSentinel)
        {
            notesQuery = notesQuery.Where(n => n.ScheduleId == null && (n.Folder == null || n.Folder.ScheduleId == null));
        }
        else if (ScheduleId is not null)
        {
            notesQuery = notesQuery.Where(n => n.ScheduleId == ScheduleId || (n.Folder != null && n.Folder.ScheduleId == ScheduleId));
        }

        notesQuery = NoteType switch
        {
            Models.NoteType.ToDo => notesQuery.Where(n => n is ToDoNote),
            Models.NoteType.Laundry => notesQuery.Where(n => n is LaundryNote),
            Models.NoteType.WorkShift => notesQuery.Where(n => n is WorkShiftNote),
            Models.NoteType.Fasting => notesQuery.Where(n => n is FastingNote),
            _ => notesQuery
        };

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            notesQuery = notesQuery.Where(n =>
                (n.Title != null && EF.Functions.ILike(n.Title, $"%{term}%")) ||
                ((n as WorkShiftNote) != null && EF.Functions.ILike((n as WorkShiftNote)!.Location, $"%{term}%")));
        }

        var notes = await notesQuery.ToListAsync();
        var today = DateTime.Now.Date;

        if (DueFilter is { } dueFilter)
        {
            var todayDate = DateOnly.FromDateTime(today);
            notes = notes.Where(n => MatchesDueFilter(SortDate(n), dueFilter, todayDate)).ToList();
        }

        var filtered = notes.Where(n => IsPastOrCompleted(n, today) == ShowCompleted).ToList();

        Notes = Sort switch
        {
            Models.NoteSortOrder.Newest => filtered.OrderByDescending(n => n.CreatedAtUtc).ToList(),
            Models.NoteSortOrder.Oldest => filtered.OrderBy(n => n.CreatedAtUtc).ToList(),
            _ => ShowCompleted
                ? filtered.OrderByDescending(SortDate).ToList()
                : filtered.OrderBy(SortDate).ThenByDescending(n => n.CreatedAtUtc).ToList()
        };

        var folderLabel = FolderId == NoneSentinel
            ? "no folder"
            : CurrentFolder is not null ? $"folder \"{CurrentFolder.Name}\"" : null;

        var scheduleLabel = ScheduleId == NoneSentinel
            ? "no schedule"
            : CurrentSchedule is not null ? $"schedule \"{CurrentSchedule.Name}\"" : null;

        var dueFilterLabel = DueFilter switch
        {
            Models.NoteDueFilter.Today => "due today",
            Models.NoteDueFilter.Tomorrow => "due tomorrow",
            Models.NoteDueFilter.ThisWeek => "due this week",
            Models.NoteDueFilter.ThisMonth => "due this month",
            _ => null
        };

        SummaryText = BuildSummaryText(Notes.Count, NoteType, ShowCompleted, folderLabel, scheduleLabel, dueFilterLabel);

        await LoadOptionsAsync(userId);
    }

    /// <summary>
    /// e.g. "5 notes", "3 completed/past to-do notes in folder "Work" and schedule "Jobb"".
    /// folderLabel/scheduleLabel are pre-resolved (already the sentinel-aware
    /// "no folder"/"folder \"X\"" text, or null when that filter isn't active)
    /// so this stays pure string composition, no DB-shaped types.
    /// </summary>
    internal static string BuildSummaryText(int count, NoteType? noteType, bool showCompleted, string? folderLabel, string? scheduleLabel, string? dueFilterLabel = null)
    {
        var typeWord = noteType switch
        {
            Models.NoteType.ToDo => "to-do",
            Models.NoteType.Laundry => "laundry",
            Models.NoteType.WorkShift => "work shift",
            Models.NoteType.Fasting => "fasting",
            _ => null
        };

        var noun = count == 1 ? "note" : "notes";
        var label = typeWord is not null ? $"{typeWord} {noun}" : noun;

        if (showCompleted)
        {
            label = $"completed/past {label}";
        }

        var scopeParts = new List<string?> { folderLabel, scheduleLabel }.Where(p => p is not null).ToList();
        var scopeText = scopeParts.Count > 0 ? $" in {string.Join(" and ", scopeParts)}" : "";
        var dueText = dueFilterLabel is not null ? $"{(scopeParts.Count > 0 ? "," : "")} {dueFilterLabel}" : "";

        return $"{count} {label}{scopeText}{dueText}";
    }

    /// <summary>
    /// A note is "past/completed" once it's marked done, or its date has
    /// already gone by. Notes with no date at all (e.g. a to-do with no due
    /// date) only ever leave the active list once explicitly marked done.
    /// </summary>
    internal static bool IsPastOrCompleted(Note note, DateTime today)
    {
        if (note.IsDone)
        {
            return true;
        }

        var date = SortDate(note);
        return date != DateTime.MaxValue && date.Date < today;
    }

    /// <summary>sortDate == DateTime.MaxValue means "no date at all" -- never matches any due filter.</summary>
    internal static bool MatchesDueFilter(DateTime sortDate, NoteDueFilter filter, DateOnly today)
    {
        if (sortDate == DateTime.MaxValue)
        {
            return false;
        }

        var date = DateOnly.FromDateTime(sortDate);
        return filter switch
        {
            Models.NoteDueFilter.Today => date == today,
            Models.NoteDueFilter.Tomorrow => date == today.AddDays(1),
            Models.NoteDueFilter.ThisWeek => date >= today && date <= today.AddDays(6),
            Models.NoteDueFilter.ThisMonth => date >= today && date <= today.AddDays(30),
            _ => true
        };
    }

    /// <summary>Single-step interval advance -- used when completing a recurring to-do.</summary>
    internal static (DateOnly Date, TimeOnly Time) AdvanceOccurrence(DateOnly date, TimeOnly time, int intervalValue, TimeUnit intervalUnit)
    {
        var next = date.ToDateTime(time) + intervalUnit.ToTimeSpan(intervalValue);
        return (DateOnly.FromDateTime(next), TimeOnly.FromDateTime(next));
    }

    internal static DateTime SortDate(Note note) => note switch
    {
        ToDoNote { DueDate: { } d } t => d.ToDateTime(t.DueTime ?? TimeOnly.MinValue),
        LaundryNote { Day: { } d } l => d.ToDateTime(l.TimeWindow.ToStartTime()),
        WorkShiftNote { Day: { } d } w => d.ToDateTime(w.StartTime),
        FastingNote f => f.Day.ToDateTime(TimeOnly.MinValue),
        _ => DateTime.MaxValue
    };

    public async Task<IActionResult> OnPostToggleDoneAsync(int id)
    {
        if (ReturnUrl is not null && !Url.IsLocalUrl(ReturnUrl))
        {
            ReturnUrl = null;
        }

        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes
            .Include(n => (n as ToDoNote)!.Reminders)
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        if (!note.IsDone && note is ToDoNote { RecurrenceIntervalValue: { } value, RecurrenceIntervalUnit: { } unit, DueDate: { } dueDate } todo)
        {
            (todo.DueDate, todo.DueTime) = AdvanceOccurrence(dueDate, todo.DueTime ?? TimeOnly.MinValue, value, unit);
            todo.Reminder24hSentAtUtc = null;
            foreach (var reminder in todo.Reminders)
            {
                reminder.SentAtUtc = null;
            }
            // IsDone stays false -- a completed recurring to-do reappears as its next occurrence instead of being closed out.
        }
        else
        {
            note.IsDone = !note.IsDone;
        }

        await context.SaveChangesAsync();

        var fragment = $"note-{id}";
        return ReturnUrl is not null
            ? LocalRedirect($"{ReturnUrl}#{fragment}")
            : RedirectToPage("/Notes/Index", pageHandler: null, routeValues: null, fragment: fragment);
    }

    private async Task LoadOptionsAsync(string userId)
    {
        var folders = await context.Folders.Where(f => f.UserId == userId).ToListAsync();
        var flattened = folders.FlattenOrdered();
        FolderOptions = new SelectList(
            flattened.Select(x => new { x.Folder.Id, Name = new string(' ', x.Depth * 2) + x.Folder.Name }),
            "Id", "Name", FolderId);

        var schedules = await context.Schedules.Where(s => s.UserId == userId).OrderBy(s => s.Name).ToListAsync();
        ScheduleOptions = new SelectList(schedules, nameof(Schedule.Id), nameof(Schedule.Name), ScheduleId);
    }
}
