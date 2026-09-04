using WebApp.Models;
using WebApp.Pages.Media;

namespace WebApp.Tests.Pages.Media;

public class IndexTests
{
    private static MediaEntry NewEntry(MediaType type) => new() { UserId = "u1", Title = "test", Type = type };

    [Fact]
    public void ProgressText_Anime_WithSeasonAndEpisode_FormatsBoth()
    {
        var entry = NewEntry(MediaType.Anime);
        entry.Season = 2;
        entry.Episode = 5;

        Assert.Equal("S2 E5", IndexModel.ProgressText(entry));
    }

    [Fact]
    public void ProgressText_Series_WithOnlyEpisode_OmitsSeason()
    {
        var entry = NewEntry(MediaType.Series);
        entry.Episode = 5;

        Assert.Equal("E5", IndexModel.ProgressText(entry));
    }

    [Fact]
    public void ProgressText_Manga_WithChapterAndVolume_FormatsBoth()
    {
        var entry = NewEntry(MediaType.Manga);
        entry.Chapter = 12;
        entry.Volume = 3;

        Assert.Equal("Ch. 12 (Vol. 3)", IndexModel.ProgressText(entry));
    }

    [Theory]
    [InlineData(true, "Watched")]
    [InlineData(false, "Not watched")]
    public void ProgressText_Movie_ReflectsWatchedFlag(bool watched, string expected)
    {
        var entry = NewEntry(MediaType.Movie);
        entry.Watched = watched;

        Assert.Equal(expected, IndexModel.ProgressText(entry));
    }

    [Fact]
    public void ProgressText_NoProgressRecorded_IsNull()
    {
        Assert.Null(IndexModel.ProgressText(NewEntry(MediaType.Anime)));
        Assert.Null(IndexModel.ProgressText(NewEntry(MediaType.Manga)));
    }

    [Fact]
    public void BuildSummaryText_NoFilters_JustCount()
    {
        Assert.Equal("3 entries", IndexModel.BuildSummaryText(3, null, null));
    }

    [Fact]
    public void BuildSummaryText_SingleEntry_UsesSingularNoun()
    {
        Assert.Equal("1 entry", IndexModel.BuildSummaryText(1, null, null));
    }

    [Fact]
    public void BuildSummaryText_WithTypeAndStatus_IncludesBoth()
    {
        Assert.Equal("2 entries (Anime, In Progress)", IndexModel.BuildSummaryText(2, MediaType.Anime, MediaStatus.InProgress));
    }
}
