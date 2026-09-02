using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    /// <summary>Null when there's no fasting note for today at all (not the same as an explicit NoFast entry).</summary>
    public FastingLevel? TodayFastingLevel { get; private set; }

    public async Task OnGetAsync()
    {
        if (userManager.GetUserId(User) is not { } userId)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Now);

        var todayFasting = await context.Notes.OfType<FastingNote>()
            .FirstOrDefaultAsync(n => n.UserId == userId && n.Day == today);

        TodayFastingLevel = todayFasting?.Level;
    }
}
