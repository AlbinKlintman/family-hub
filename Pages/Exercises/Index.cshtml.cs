using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Exercises;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<ExerciseRow> Exercises { get; set; } = new();

    [BindProperty]
    [Required]
    [StringLength(200)]
    public string NewExerciseName { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadExercisesAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            await LoadExercisesAsync();
            return Page();
        }

        context.Exercises.Add(new Exercise { UserId = userId, Name = NewExerciseName.Trim() });
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadExercisesAsync()
    {
        var userId = userManager.GetUserId(User)!;

        Exercises = await context.Exercises
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Name)
            .Select(e => new ExerciseRow(e.Id, e.Name, e.WorkoutLogs.Count))
            .ToListAsync();
    }

    public record ExerciseRow(int Id, string Name, int LogCount);
}
