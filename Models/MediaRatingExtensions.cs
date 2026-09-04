namespace WebApp.Models;

public static class MediaRatingExtensions
{
    /// <summary>MyAnimeList's 1-10 rating labels, e.g. 10 => "Masterpiece", 1 => "Appalling".</summary>
    public static string? ToRatingLabel(this int? rating) => rating switch
    {
        10 => "Masterpiece",
        9 => "Great",
        8 => "Very Good",
        7 => "Good",
        6 => "Fine",
        5 => "Average",
        4 => "Poor",
        3 => "Bad",
        2 => "Very Bad",
        1 => "Appalling",
        _ => null
    };
}
