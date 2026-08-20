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

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var notes = await context.Notes
            .Where(n => n.UserId == userId)
            .ToListAsync();

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
        LaundryNote { Day: { } d } l => d.ToDateTime(l.TimeWindowStart ?? TimeOnly.MinValue),
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
