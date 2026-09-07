namespace WebApp.Models;

public enum MediaType
{
    Anime,
    Manga,
    Series,
    Movie,
    Book
}

public static class MediaTypeExtensions
{
    public static string ToDisplayName(this MediaType type) => type switch
    {
        MediaType.Anime => "Anime",
        MediaType.Manga => "Manga",
        MediaType.Series => "Series",
        MediaType.Movie => "Movie",
        MediaType.Book => "Book",
        _ => type.ToString()
    };
}
