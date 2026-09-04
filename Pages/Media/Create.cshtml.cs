using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Helpers;
using WebApp.Models;

namespace WebApp.Pages.Media;

public class CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
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

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var entry = new MediaEntry
        {
            UserId = userId,
            Title = Input.Title.Trim(),
            Type = Input.Type,
            Status = Input.Status,
            Rating = Input.Rating,
            Season = Input.Season,
            Episode = Input.Episode,
            Chapter = Input.Chapter,
            Volume = Input.Volume,
            Watched = Input.Watched,
            Links = Input.Links
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Select(l => new MediaLink { Url = l })
                .ToList(),
            CreatedAtUtc = DateTime.UtcNow
        };

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

        [Range(0, int.MaxValue)]
        public int? Season { get; set; }

        [Range(0, int.MaxValue)]
        public int? Episode { get; set; }

        [Range(0, int.MaxValue)]
        public int? Chapter { get; set; }

        [Range(0, int.MaxValue)]
        public int? Volume { get; set; }

        public bool Watched { get; set; }

        public List<string> Links { get; set; } = [""];
    }
}
