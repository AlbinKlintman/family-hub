using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Training;

/// <summary>
/// Creates the workout "shell" (date, time, session type) first; exercises and
/// sets are added afterward on the Workout detail page.
/// </summary>
public class CreateWorkoutModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = userManager.GetUserId(User)!;

        var workout = new Workout
        {
            UserId = userId,
            Date = Input.Date,
            Time = Input.Time,
            SessionType = Input.SessionType
        };

        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        return RedirectToPage("/Training/Workout", new { id = workout.Id });
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Display(Name = "Time")]
        public TimeOnly? Time { get; set; }

        [Display(Name = "Session type")]
        public TrainingSessionType SessionType { get; set; } = TrainingSessionType.Push;
    }
}
