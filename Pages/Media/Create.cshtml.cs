using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Media;

public class CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>Every accepted connection this entry could be shared with.</summary>
    public List<(string UserId, string Username)> ShareOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;
        var friendUsernames = await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId);
        ShareOptions = friendUsernames.Select(kv => (kv.Key, kv.Value)).OrderBy(x => x.Value).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        // Model binding leaves a null (not an empty string) for a blank indexed
        // form field, e.g. an untouched repeater row -- normalize before use.
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
            var friendUsernames = await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId);
            ShareOptions = friendUsernames.Select(kv => (kv.Key, kv.Value)).OrderBy(x => x.Value).ToList();
            return Page();
        }

        var entry = new MediaEntry
        {
            UserId = userId,
            Title = Input.Title.Trim(),
            Type = Input.Type,
            Status = Input.Status,
            Rating = Input.Rating,
            CoverImageUrl = string.IsNullOrEmpty(coverImageUrl) ? null : coverImageUrl,
            Season = Input.Season,
            Episode = Input.Episode,
            Chapter = Input.Chapter,
            Volume = Input.Volume,
            Page = Input.Page,
            Watched = Input.Watched,
            Links = Input.Links
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Select(l => new MediaLink { Url = l })
                .ToList(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var selected = new HashSet<string>(Input.ShareWithUserIds ?? []);
        var acceptedFriendIds = (await FriendConnectionProvider.GetAcceptedConnectionUsernamesAsync(context, userId)).Keys;
        selected.IntersectWith(acceptedFriendIds);
        foreach (var toAdd in selected)
        {
            entry.Shares.Add(new MediaEntryShare { SharedWithUserId = toAdd });
        }

        context.MediaEntries.Add(entry);
        await context.SaveChangesAsync();

        return RedirectToPage("/Media/Index");
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

        /// <summary>Which accepted connections this entry should be shared with.</summary>
        public List<string> ShareWithUserIds { get; set; } = [];
    }
}
