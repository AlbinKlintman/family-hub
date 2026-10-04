using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Pages.Shared;
using WebApp.Services;

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

    /// <summary>Which accepted connections the new schedule should be shared with straight away, instead of only via Edit afterwards.</summary>
    [BindProperty]
    public List<string> NewScheduleShareWithUserIds { get; set; } = [];

    public List<ShareOption> ShareOptions { get; private set; } = [];

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

        var schedule = new Schedule { UserId = userId, Name = NewScheduleName.Trim(), Color = NewScheduleColor };
        foreach (var shareWith in await FriendConnectionProvider.FilterToAcceptedAsync(context, userId, NewScheduleShareWithUserIds))
        {
            schedule.Shares.Add(new ScheduleShare { SharedWithUserId = shareWith });
        }

        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        return RedirectToPage();
    }

    /// <summary>Hidden schedules stay listed here (and only here) so they can be shown again.</summary>
    public async Task<IActionResult> OnPostToggleHiddenAsync(int id)
    {
        var userId = userManager.GetUserId(User)!;

        var schedule = await context.Schedules.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (schedule is null)
        {
            return NotFound();
        }

        schedule.IsHidden = !schedule.IsHidden;
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
                s.IsHidden,
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
                s.Id, s.Name, s.Color, s.IsHidden, s.NoteCount, s.FolderCount,
                s.SharedWithUserIds.Select(id => usernamesById.GetValueOrDefault(id, "someone")).ToList()))
            .ToList();

        var selected = NewScheduleShareWithUserIds.ToHashSet();
        ShareOptions = (await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId))
            .Select(kv => new ShareOption(kv.Key, kv.Value, selected.Contains(kv.Key)))
            .OrderBy(x => x.Username)
            .ToList();
    }

    public record ScheduleRow(int Id, string Name, FolderColor Color, bool IsHidden, int NoteCount, int FolderCount, List<string> SharedWithUsernames);
}
