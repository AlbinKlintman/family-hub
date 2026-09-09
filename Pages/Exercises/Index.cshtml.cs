using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Exercises;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<ExerciseRow> Exercises { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        Exercises = await context.Exercises
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.SessionType).ThenBy(e => e.Name)
            .Select(e => new ExerciseRow(e.Id, e.Name, e.SessionType, e.WeightType, e.SeatForwardPosition, e.SeatHeightPosition, e.WorkoutExercises.Count))
            .ToListAsync();
    }

    public record ExerciseRow(int Id, string Name, TrainingSessionType SessionType, ExerciseWeightType WeightType, decimal? SeatForwardPosition, decimal? SeatHeightPosition, int LogCount);
}
