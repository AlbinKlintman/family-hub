using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Media;

public class IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    public List<MediaEntry> Entries { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public MediaType? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public MediaStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    /// <summary>Posted back by the IncrementProgress form so it can return to the same filtered list + scroll spot, same mechanism as Notes' ToggleDone.</summary>
    [BindProperty]
    public string? ReturnUrl { get; set; }

    public string SummaryText { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var query = context.MediaEntries
            .Include(m => m.Links)
            .Where(m => m.UserId == userId);

        if (Type is not null)
        {
            query = query.Where(m => m.Type == Type);
        }

        if (Status is not null)
        {
            query = query.Where(m => m.Status == Status);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            query = query.Where(m => EF.Functions.ILike(m.Title, $"%{term}%"));
        }

        Entries = await query
            .OrderBy(m => m.Status == MediaStatus.InProgress ? 0 : 1)
            .ThenBy(m => m.Title)
            .ToListAsync();

        SummaryText = BuildSummaryText(Entries.Count, Type, Status);
    }

    public async Task<IActionResult> OnPostIncrementProgressAsync(int id)
    {
        if (ReturnUrl is not null && !Url.IsLocalUrl(ReturnUrl))
        {
            ReturnUrl = null;
        }

        var userId = userManager.GetUserId(User)!;

        var entry = await context.MediaEntries.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        if (entry is null)
        {
            return NotFound();
        }

        switch (entry.Type)
        {
            case MediaType.Anime or MediaType.Series:
                entry.Episode = (entry.Episode ?? 0) + 1;
                break;
            case MediaType.Manga:
                entry.Chapter = (entry.Chapter ?? 0) + 1;
                break;
        }

        await context.SaveChangesAsync();

        var fragment = $"media-{id}";
        return ReturnUrl is not null
            ? LocalRedirect($"{ReturnUrl}#{fragment}")
            : RedirectToPage("/Media/Index", pageHandler: null, routeValues: null, fragment: fragment);
    }

    /// <summary>Label for the quick-increment button -- null (no button shown) for types with no simple "next unit" (movies are watched/not, not counted).</summary>
    internal static string? IncrementProgressLabel(MediaType type) => type switch
    {
        MediaType.Anime or MediaType.Series => "+1 episode",
        MediaType.Manga => "+1 chapter",
        _ => null
    };

    internal static string BuildSummaryText(int count, MediaType? type, MediaStatus? status)
    {
        var noun = count == 1 ? "entry" : "entries";
        var parts = new List<string?> { type?.ToDisplayName(), status?.ToDisplayName() }
            .Where(p => p is not null)
            .ToList();
        var suffix = parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";

        return $"{count} {noun}{suffix}";
    }

    /// <summary>e.g. "S2 E5" for anime/series, "Ch. 12 (Vol. 3)" for manga, "Watched"/"Not watched" for a movie -- null if nothing has been recorded yet.</summary>
    internal static string? ProgressText(MediaEntry entry) => entry.Type switch
    {
        MediaType.Anime or MediaType.Series when entry.Season is not null || entry.Episode is not null =>
            string.Join(" ", new[]
            {
                entry.Season is { } s ? $"S{s}" : null,
                entry.Episode is { } e ? $"E{e}" : null
            }.Where(p => p is not null)),
        MediaType.Manga when entry.Chapter is not null || entry.Volume is not null =>
            string.Join(" ", new[]
            {
                entry.Chapter is { } c ? $"Ch. {c}" : null,
                entry.Volume is { } v ? $"(Vol. {v})" : null
            }.Where(p => p is not null)),
        MediaType.Movie => entry.Watched ? "Watched" : "Not watched",
        _ => null
    };
}
