namespace WebApp.Models;

public enum MediaType
{
    Anime,
    Manga,
    Series,
    Movie
}

public static class MediaTypeExtensions
{
    public static string ToDisplayName(this MediaType type) => type switch
    {
        MediaType.Anime => "Anime",
        MediaType.Manga => "Manga",
        MediaType.Series => "Series",
        MediaType.Movie => "Movie",
        _ => type.ToString()
    };
}
