using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Board;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public Dictionary<ApplicationStatus, List<JobApplication>> Columns { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var applications = await context.JobApplications
            .Include(a => a.Company)
            .Include(a => a.Links)
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.SortOrder)
            .ToListAsync();

        Columns = Enum.GetValues<ApplicationStatus>()
            .ToDictionary(status => status, status => applications.Where(a => a.Status == status).ToList());
    }

    public record ReorderRequest(int CardId, ApplicationStatus Status, List<int> OrderedCardIds);

    public async Task<IActionResult> OnPostReorderAsync([FromBody] ReorderRequest request)
    {
        var userId = userManager.GetUserId(User)!;

        var movedCard = await context.JobApplications
            .FirstOrDefaultAsync(a => a.Id == request.CardId && a.UserId == userId);

        if (movedCard is null)
        {
            return NotFound();
        }

        var cardsInColumn = await context.JobApplications
            .Where(a => a.UserId == userId && request.OrderedCardIds.Contains(a.Id))
            .ToListAsync();

        if (cardsInColumn.Count != request.OrderedCardIds.Count)
        {
            return BadRequest();
        }

        movedCard.SetStatus(request.Status);

        for (var index = 0; index < request.OrderedCardIds.Count; index++)
        {
            var id = request.OrderedCardIds[index];
            var card = cardsInColumn.First(a => a.Id == id);
            card.SortOrder = index;
        }

        await context.SaveChangesAsync();

        return new JsonResult(new { success = true, appliedDate = movedCard.AppliedDate });
    }
}
