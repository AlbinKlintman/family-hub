using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Training;

public class LogWorkoutModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList ExerciseOptions { get; set; } = default!;

    public async Task OnGetAsync()
    {
        await LoadExerciseOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var exerciseOwned = await context.Exercises
            .AnyAsync(e => e.Id == Input.ExerciseId && e.UserId == userId);

        if (!exerciseOwned)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ExerciseId)}", "Exercise not found.");
        }

        if (!ModelState.IsValid)
        {
            await LoadExerciseOptionsAsync();
            return Page();
        }

        context.WorkoutLogs.Add(new WorkoutLog
        {
            UserId = userId,
            ExerciseId = Input.ExerciseId,
            SessionType = Input.SessionType,
            WeightKg = Input.WeightKg,
            Reps = Input.Reps,
            Sets = Input.Sets,
            Date = Input.Date
        });
        await context.SaveChangesAsync();

        return RedirectToPage("/Training/Index");
    }

    private async Task LoadExerciseOptionsAsync()
    {
        var userId = userManager.GetUserId(User)!;
        var exercises = await context.Exercises
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Name)
            .ToListAsync();

        ExerciseOptions = new SelectList(exercises, nameof(Exercise.Id), nameof(Exercise.Name), Input.ExerciseId);
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Exercise")]
        public int ExerciseId { get; set; }

        [Display(Name = "Session type")]
        public TrainingSessionType SessionType { get; set; } = TrainingSessionType.Push;

        [Required]
        [Range(1, 500)]
        [Display(Name = "Weight (kg)")]
        public decimal WeightKg { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Reps")]
        public int Reps { get; set; }

        [Required]
        [Range(1, 20)]
        [Display(Name = "Sets")]
        public int Sets { get; set; } = 3;

        [Required]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
