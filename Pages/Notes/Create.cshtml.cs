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
        if (Input.NoteType == NoteType.ToDo && string.IsNullOrWhiteSpace(Input.Title))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Title)}", "Note text is required.");
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
                LaundryType = Input.LaundryType,
                Room = Input.Room,
                Day = Input.Day,
                TimeWindow = Input.TimeWindow
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

        [StringLength(2000)]
        public string? Title { get; set; }

        [Display(Name = "Due date")]
        public DateOnly? DueDate { get; set; }

        [Display(Name = "Due time")]
        public TimeOnly? DueTime { get; set; }

        [Display(Name = "Type")]
        public LaundryType LaundryType { get; set; } = LaundryType.NormalClothes;

        [Display(Name = "Room")]
        public LaundryRoom Room { get; set; } = LaundryRoom.Room2Right;

        [Display(Name = "Day")]
        public DateOnly? Day { get; set; }

        [Display(Name = "Time window")]
        public LaundryTimeWindow TimeWindow { get; set; } = LaundryTimeWindow.Afternoon;
    }
}
