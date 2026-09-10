using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Media;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>False when this entry is only shared with the current user -- they can only set their own Rating, not the entry's actual content.</summary>
    public bool IsOwner { get; private set; }

    public string? OwnerUsername { get; private set; }

    /// <summary>Owner-only: every accepted connection, and whether this entry is currently shared with them.</summary>
    public List<(string UserId, string Username, bool IsShared)> ShareOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.MediaEntries
            .Include(m => m.Links)
            .Include(m => m.Shares)
            .FirstOrDefaultAsync(m => m.Id == Id && (m.UserId == userId || m.Shares.Any(s => s.SharedWithUserId == userId)));

        if (entry is null)
        {
            return NotFound();
        }

        IsOwner = entry.UserId == userId;

        Input = new InputModel
        {
            Title = entry.Title,
            Type = entry.Type,
            Status = entry.Status,
            CoverImageUrl = entry.CoverImageUrl,
            Season = entry.Season,
            Episode = entry.Episode,
            Chapter = entry.Chapter,
            Volume = entry.Volume,
            Page = entry.Page,
            Watched = entry.Watched,
            Links = entry.Links.Select(l => l.Url).ToList()
        };
        if (Input.Links.Count == 0)
        {
            Input.Links.Add("");
        }

        if (IsOwner)
        {
            Input.Rating = entry.Rating;
            await LoadShareOptionsAsync(userId, entry);
        }
        else
        {
            OwnerUsername = (await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == entry.UserId))?.Username;
            Input.Rating = entry.Shares.First(s => s.SharedWithUserId == userId).Rating;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.MediaEntries
            .Include(m => m.Links)
            .Include(m => m.Shares)
            .FirstOrDefaultAsync(m => m.Id == Id && (m.UserId == userId || m.Shares.Any(s => s.SharedWithUserId == userId)));

        if (entry is null)
        {
            return NotFound();
        }

        IsOwner = entry.UserId == userId;

        if (!IsOwner)
        {
            var share = entry.Shares.First(s => s.SharedWithUserId == userId);
            share.Rating = Input.Rating;
            await context.SaveChangesAsync();
            return RedirectToPage("/Media/Index");
        }

        for (var i = 0; i < Input.Links.Count; i++)
        {
            Input.Links[i] ??= string.Empty;
        }

        for (var i = 0; i < Input.Links.Count; i++)
        {
            var link = Input.Links[i].Trim();
            if (link.Length > 0 && !UrlValidator.IsValid(link))
            {
                ModelState.AddModelError(string.Empty, $"Link {i + 1}: enter a valid URL.");
            }
        }

        var coverImageUrl = Input.CoverImageUrl?.Trim();
        if (!string.IsNullOrEmpty(coverImageUrl) && !UrlValidator.IsValid(coverImageUrl))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.CoverImageUrl)}", "Enter a valid URL.");
        }

        if (!ModelState.IsValid)
        {
            await LoadShareOptionsAsync(userId, entry);
            return Page();
        }

        entry.Title = Input.Title.Trim();
        entry.Type = Input.Type;
        entry.Status = Input.Status;
        entry.Rating = Input.Rating;
        entry.CoverImageUrl = string.IsNullOrEmpty(coverImageUrl) ? null : coverImageUrl;
        entry.Season = Input.Season;
        entry.Episode = Input.Episode;
        entry.Chapter = Input.Chapter;
        entry.Volume = Input.Volume;
        entry.Page = Input.Page;
        entry.Watched = Input.Watched;

        entry.Links.Clear();
        foreach (var url in Input.Links.Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            entry.Links.Add(new MediaLink { Url = url });
        }

        await SyncSharesAsync(entry, userId);

        await context.SaveChangesAsync();

        return RedirectToPage("/Media/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var entry = await context.MediaEntries
            .FirstOrDefaultAsync(m => m.Id == Id && m.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        context.MediaEntries.Remove(entry);
        await context.SaveChangesAsync();

        return RedirectToPage("/Media/Index");
    }

    /// <summary>Adds/removes MediaEntryShare rows to match what the owner checked, but only ever for actual accepted connections -- the posted ids are never trusted blindly.</summary>
    private async Task SyncSharesAsync(MediaEntry entry, string ownerId)
    {
        var selected = new HashSet<string>(Input.ShareWithUserIds ?? []);
        var acceptedFriendIds = (await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, ownerId)).Keys;
        selected.IntersectWith(acceptedFriendIds);

        foreach (var toRemove in entry.Shares.Where(s => !selected.Contains(s.SharedWithUserId)).ToList())
        {
            entry.Shares.Remove(toRemove);
        }

        var existingIds = entry.Shares.Select(s => s.SharedWithUserId).ToHashSet();
        foreach (var toAdd in selected.Except(existingIds))
        {
            entry.Shares.Add(new MediaEntryShare { SharedWithUserId = toAdd });
        }
    }

    private async Task LoadShareOptionsAsync(string userId, MediaEntry entry)
    {
        var friendUsernames = await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId);
        var sharedWith = entry.Shares.Select(s => s.SharedWithUserId).ToHashSet();

        ShareOptions = friendUsernames
            .Select(kv => (kv.Key, kv.Value, sharedWith.Contains(kv.Key)))
            .OrderBy(x => x.Value)
            .ToList();
    }

    public class InputModel
    {
        [Required]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public MediaType Type { get; set; }

        [Required]
        public MediaStatus Status { get; set; } = MediaStatus.PlanToStart;

        [Range(1, 10)]
        [Display(Name = "Rating (1-10)")]
        public int? Rating { get; set; }

        [StringLength(2048)]
        [Display(Name = "Cover image URL")]
        public string? CoverImageUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int? Season { get; set; }

        [Range(0, int.MaxValue)]
        public int? Episode { get; set; }

        [Range(0, int.MaxValue)]
        public int? Chapter { get; set; }

        [Range(0, int.MaxValue)]
        public int? Volume { get; set; }

        [Range(0, int.MaxValue)]
        public int? Page { get; set; }

        public bool Watched { get; set; }

        public List<string> Links { get; set; } = [""];

        /// <summary>Owner-only: which accepted connections this entry should be shared with.</summary>
        public List<string> ShareWithUserIds { get; set; } = [];
    }
}
