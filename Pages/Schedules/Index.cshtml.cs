using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Schedules;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<ScheduleRow> Schedules { get; set; } = [];

    [BindProperty]
    [Required]
    [StringLength(100)]
    public string NewScheduleName { get; set; } = string.Empty;

    [BindProperty]
    public FolderColor NewScheduleColor { get; set; } = FolderColor.Blue;

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        context.Schedules.Add(new Schedule { UserId = userId, Name = NewScheduleName.Trim(), Color = NewScheduleColor });
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var raw = await context.Schedules
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Color,
                NoteCount = s.Notes.Count,
                FolderCount = s.Folders.Count,
                SharedWithUserIds = s.Shares.Select(sh => sh.SharedWithUserId).ToList()
            })
            .ToListAsync();

        var allSharedIds = raw.SelectMany(s => s.SharedWithUserIds).Distinct().ToList();
        var usernamesById = await context.UserProfiles
            .Where(p => allSharedIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.Username);

        Schedules = raw
            .Select(s => new ScheduleRow(
                s.Id, s.Name, s.Color, s.NoteCount, s.FolderCount,
                s.SharedWithUserIds.Select(id => usernamesById.GetValueOrDefault(id, "someone")).ToList()))
            .ToList();
    }

    public record ScheduleRow(int Id, string Name, FolderColor Color, int NoteCount, int FolderCount, List<string> SharedWithUsernames);
}
