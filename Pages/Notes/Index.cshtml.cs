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

    public Folder? CurrentFolder { get; set; }
    public Schedule? CurrentSchedule { get; set; }
    public SelectList FolderOptions { get; set; } = default!;
    public SelectList ScheduleOptions { get; set; } = default!;

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

        var notes = await notesQuery.ToListAsync();
        var today = DateTime.Now.Date;

        var filtered = notes.Where(n => IsPastOrCompleted(n, today) == ShowCompleted).ToList();

        Notes = ShowCompleted
            ? filtered.OrderByDescending(SortDate).ToList()
            : filtered.OrderBy(SortDate).ThenByDescending(n => n.CreatedAtUtc).ToList();

        await LoadOptionsAsync(userId);
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

    private static DateTime SortDate(Note note) => note switch
    {
        ToDoNote { DueDate: { } d } t => d.ToDateTime(t.DueTime ?? TimeOnly.MinValue),
        LaundryNote { Day: { } d } l => d.ToDateTime(l.TimeWindow.ToStartTime()),
        WorkShiftNote { Day: { } d } w => d.ToDateTime(w.StartTime),
        FastingNote f => f.Day.ToDateTime(TimeOnly.MinValue),
        _ => DateTime.MaxValue
    };

    public async Task<IActionResult> OnPostToggleDoneAsync(int id)
    {
        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        note.IsDone = !note.IsDone;
        await context.SaveChangesAsync();

        return RedirectToPage(new { FolderId, ScheduleId, ShowCompleted, NoteType });
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
