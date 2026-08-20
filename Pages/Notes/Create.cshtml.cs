using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Notes;

public class CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        switch (Input.NoteType)
        {
            case NoteType.ToDo:
                if (string.IsNullOrWhiteSpace(Input.Title))
                {
                    ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Title)}", "Title is required for a to-do.");
                }
                break;
            case NoteType.Laundry:
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

        var userId = userManager.GetUserId(User)!;

        Note note = Input.NoteType switch
        {
            NoteType.ToDo => new ToDoNote
            {
                UserId = userId,
                Title = Input.Title,
                DueDate = Input.DueDate,
                DueTime = Input.DueTime
            },
            NoteType.Laundry => new LaundryNote
            {
                UserId = userId,
                Title = Input.Title,
                Room = Input.Room,
                Day = Input.Day,
                TimeWindowStart = Input.TimeWindowStart,
                TimeWindowEnd = Input.TimeWindowEnd
            },
            _ => throw new InvalidOperationException("Unknown note type.")
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        return RedirectToPage("/Notes/Index");
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Type")]
        public NoteType NoteType { get; set; } = NoteType.ToDo;

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
