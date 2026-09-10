using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Schedules;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>Every accepted connection, and whether this schedule is currently shared with them.</summary>
    public List<(string UserId, string Username, bool IsShared)> ShareOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var schedule = await context.Schedules
            .Include(s => s.Shares)
            .FirstOrDefaultAsync(s => s.Id == Id && s.UserId == userId);
        if (schedule is null)
        {
            return NotFound();
        }

        Input = new InputModel { Name = schedule.Name, Color = schedule.Color };
        await LoadShareOptionsAsync(userId, schedule);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var schedule = await context.Schedules
            .Include(s => s.Shares)
            .FirstOrDefaultAsync(s => s.Id == Id && s.UserId == userId);
        if (schedule is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadShareOptionsAsync(userId, schedule);
            return Page();
        }

        schedule.Name = Input.Name.Trim();
        schedule.Color = Input.Color;
        await SyncSharesAsync(schedule, userId);
        await context.SaveChangesAsync();

        return RedirectToPage("/Schedules/Index");
    }

    /// <summary>Adds/removes ScheduleShare rows to match what was checked, but only ever for actual accepted connections -- the posted ids are never trusted blindly.</summary>
    private async Task SyncSharesAsync(Schedule schedule, string ownerId)
    {
        var selected = new HashSet<string>(Input.ShareWithUserIds ?? []);
        var acceptedFriendIds = (await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, ownerId)).Keys;
        selected.IntersectWith(acceptedFriendIds);

        foreach (var toRemove in schedule.Shares.Where(s => !selected.Contains(s.SharedWithUserId)).ToList())
        {
            schedule.Shares.Remove(toRemove);
        }

        var existingIds = schedule.Shares.Select(s => s.SharedWithUserId).ToHashSet();
        foreach (var toAdd in selected.Except(existingIds))
        {
            schedule.Shares.Add(new ScheduleShare { SharedWithUserId = toAdd });
        }
    }

    private async Task LoadShareOptionsAsync(string userId, Schedule schedule)
    {
        var friendUsernames = await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId);
        var sharedWith = schedule.Shares.Select(s => s.SharedWithUserId).ToHashSet();

        ShareOptions = friendUsernames
            .Select(kv => (kv.Key, kv.Value, sharedWith.Contains(kv.Key)))
            .OrderBy(x => x.Value)
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var schedule = await context.Schedules.FirstOrDefaultAsync(s => s.Id == Id && s.UserId == userId);
        if (schedule is null)
        {
            return NotFound();
        }

        context.Schedules.Remove(schedule);
        await context.SaveChangesAsync();

        return RedirectToPage("/Schedules/Index");
    }

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public FolderColor Color { get; set; } = FolderColor.Blue;

        /// <summary>Which accepted connections this schedule (and every note tagged with it) should be visible to.</summary>
        public List<string> ShareWithUserIds { get; set; } = [];
    }
}
