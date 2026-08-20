using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Notes;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public NoteType CurrentType { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == Id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        Input.Title = note.Title;
        LoadTypeSpecificFields(note);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == Id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        switch (note)
        {
            case ToDoNote:
                CurrentType = NoteType.ToDo;
                if (string.IsNullOrWhiteSpace(Input.Title))
                {
                    ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Title)}", "Title is required for a to-do.");
                }
                break;
            case LaundryNote:
                CurrentType = NoteType.Laundry;
                if (string.IsNullOrWhiteSpace(Input.Room))
                {
                    ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Room)}", "Room is required for a laundry note.");
                }
                break;
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        note.Title = Input.Title;

        switch (note)
        {
            case ToDoNote todo:
                todo.DueDate = Input.DueDate;
                todo.DueTime = Input.DueTime;
                break;
            case LaundryNote laundry:
                laundry.Room = Input.Room;
                laundry.Day = Input.Day;
                laundry.TimeWindowStart = Input.TimeWindowStart;
                laundry.TimeWindowEnd = Input.TimeWindowEnd;
                break;
        }

        await context.SaveChangesAsync();

        return RedirectToPage("/Notes/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == Id && n.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        context.Notes.Remove(note);
        await context.SaveChangesAsync();

        return RedirectToPage("/Notes/Index");
    }

    private void LoadTypeSpecificFields(Note note)
    {
        switch (note)
        {
            case ToDoNote todo:
                CurrentType = NoteType.ToDo;
                Input.DueDate = todo.DueDate;
                Input.DueTime = todo.DueTime;
                break;
            case LaundryNote laundry:
                CurrentType = NoteType.Laundry;
                Input.Room = laundry.Room;
                Input.Day = laundry.Day;
                Input.TimeWindowStart = laundry.TimeWindowStart;
                Input.TimeWindowEnd = laundry.TimeWindowEnd;
                break;
        }
    }

    public class InputModel
    {
        [StringLength(200)]
        public string? Title { get; set; }

        [Display(Name = "Due date")]
        public DateOnly? DueDate { get; set; }

        [Display(Name = "Due time")]
        public TimeOnly? DueTime { get; set; }

        [StringLength(200)]
        public string? Room { get; set; }

        [Display(Name = "Day")]
        public DateOnly? Day { get; set; }

        [Display(Name = "Window start")]
        public TimeOnly? TimeWindowStart { get; set; }

        [Display(Name = "Window end")]
        public TimeOnly? TimeWindowEnd { get; set; }
    }
}
