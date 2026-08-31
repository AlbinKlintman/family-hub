using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Notes;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<Note> OpenNotes { get; set; } = [];
    public List<Note> DoneNotes { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? FolderId { get; set; }

    public Folder? CurrentFolder { get; set; }

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (FolderId is not null)
        {
            CurrentFolder = await context.Folders.FirstOrDefaultAsync(f => f.Id == FolderId && f.UserId == userId);
            if (CurrentFolder is null)
            {
                FolderId = null;
            }
        }

        var notesQuery = context.Notes
            .Include(n => n.Folder)
            .Include(n => n.Schedule)
            .Include(n => (n as WorkShiftNote)!.Colleagues)
            .Where(n => n.UserId == userId);
        if (FolderId is not null)
        {
            notesQuery = notesQuery.Where(n => n.FolderId == FolderId);
        }
        var notes = await notesQuery.ToListAsync();

        var sorted = notes
            .OrderBy(SortDate)
            .ThenByDescending(n => n.CreatedAtUtc)
            .ToList();

        OpenNotes = sorted.Where(n => !n.IsDone).ToList();
        DoneNotes = sorted.Where(n => n.IsDone).OrderByDescending(n => n.CreatedAtUtc).ToList();
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

        return RedirectToPage();
    }
}
