using WebApp.Models;

namespace WebApp.Tests.Models;

public class MediaEntryTests
{
    [Theory]
    [InlineData(MediaType.Anime, "Watching")]
    [InlineData(MediaType.Series, "Watching")]
    [InlineData(MediaType.Movie, "Watching")]
    [InlineData(MediaType.Manga, "Reading")]
    [InlineData(MediaType.Book, "Reading")]
    public void InProgress_UsesWatchingOrReading_BasedOnType(MediaType type, string expected)
    {
        Assert.Equal(expected, MediaStatus.InProgress.ToDisplayName(type));
    }

    [Theory]
    [InlineData(MediaType.Anime, "Plan to Watch")]
    [InlineData(MediaType.Series, "Plan to Watch")]
    [InlineData(MediaType.Movie, "Plan to Watch")]
    [InlineData(MediaType.Manga, "Plan to Read")]
    [InlineData(MediaType.Book, "Plan to Read")]
    public void PlanToStart_UsesWatchOrReadWording_BasedOnType(MediaType type, string expected)
    {
        Assert.Equal(expected, MediaStatus.PlanToStart.ToDisplayName(type));
    }

    [Fact]
    public void Book_ToDisplayName_IsBook()
    {
        Assert.Equal("Book", MediaType.Book.ToDisplayName());
    }

    [Theory]
    [InlineData(MediaStatus.Completed, "Completed")]
    [InlineData(MediaStatus.OnHold, "On Hold")]
    [InlineData(MediaStatus.Dropped, "Dropped")]
    public void OtherStatuses_AreTypeNeutral(MediaStatus status, string expected)
    {
        Assert.Equal(expected, status.ToDisplayName(MediaType.Anime));
        Assert.Equal(expected, status.ToDisplayName(MediaType.Manga));
    }

    [Theory]
    [InlineData(10, "Masterpiece")]
    [InlineData(9, "Great")]
    [InlineData(8, "Very Good")]
    [InlineData(7, "Good")]
    [InlineData(6, "Fine")]
    [InlineData(5, "Average")]
    [InlineData(4, "Bad")]
    [InlineData(3, "Very Bad")]
    [InlineData(2, "Horrible")]
    [InlineData(1, "Appalling")]
    public void ToRatingLabel_MapsMyAnimeListScale(int rating, string expected)
    {
        Assert.Equal(expected, ((int?)rating).ToRatingLabel());
    }

    [Fact]
    public void ToRatingLabel_NullRating_IsNull()
    {
        Assert.Null(((int?)null).ToRatingLabel());
    }
}
