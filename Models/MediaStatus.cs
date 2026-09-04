namespace WebApp.Models;

public enum MediaStatus
{
    InProgress,
    Completed,
    OnHold,
    Dropped,
    PlanToStart
}

public static class MediaStatusExtensions
{
    /// <summary>Type-neutral labels, used for the filter dropdown and summary text (which aren't tied to one type).</summary>
    public static string ToDisplayName(this MediaStatus status) => status switch
    {
        MediaStatus.InProgress => "In Progress",
        MediaStatus.Completed => "Completed",
        MediaStatus.OnHold => "On Hold",
        MediaStatus.Dropped => "Dropped",
        MediaStatus.PlanToStart => "Plan to Start",
        _ => status.ToString()
    };

    /// <summary>
    /// MyAnimeList uses different verbs for its two list families -- "Watching" for
    /// anime/series/movies, "Reading" for manga -- even though it's the same underlying
    /// status. Used when showing an actual entry, as opposed to the type-neutral filter UI.
    /// </summary>
    public static string ToDisplayName(this MediaStatus status, MediaType type)
    {
        var reading = type == MediaType.Manga;
        return status switch
        {
            MediaStatus.InProgress => reading ? "Reading" : "Watching",
            MediaStatus.PlanToStart => reading ? "Plan to Read" : "Plan to Watch",
            _ => status.ToDisplayName()
        };
    }

    public static string ToBadgeCssClass(this MediaStatus status) => status switch
    {
        MediaStatus.InProgress => "bg-primary",
        MediaStatus.Completed => "bg-success",
        MediaStatus.OnHold => "bg-warning text-dark",
        MediaStatus.Dropped => "bg-danger",
        MediaStatus.PlanToStart => "bg-secondary",
        _ => "bg-secondary"
    };
}
