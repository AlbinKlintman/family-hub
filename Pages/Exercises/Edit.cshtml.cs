using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Pages.Exercises;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var exercise = await context.Exercises
            .FirstOrDefaultAsync(e => e.Id == Id && e.UserId == userId);

        if (exercise is null)
        {
            return NotFound();
        }

        Name = exercise.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var exercise = await context.Exercises
            .FirstOrDefaultAsync(e => e.Id == Id && e.UserId == userId);

        if (exercise is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        exercise.Name = Name.Trim();
        await context.SaveChangesAsync();

        return RedirectToPage("/Exercises/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var exercise = await context.Exercises
            .FirstOrDefaultAsync(e => e.Id == Id && e.UserId == userId);

        if (exercise is null)
        {
            return NotFound();
        }

        var hasLogs = await context.WorkoutLogs.AnyAsync(w => w.ExerciseId == Id);
        if (hasLogs)
        {
            ModelState.Clear();
            Name = exercise.Name;
            ModelState.AddModelError(string.Empty, "Can't delete an exercise with logged workouts. Delete its workout logs first.");
            return Page();
        }

        context.Exercises.Remove(exercise);
        await context.SaveChangesAsync();

        return RedirectToPage("/Exercises/Index");
    }
}
