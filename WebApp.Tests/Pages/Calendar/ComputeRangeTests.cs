using WebApp.Models;
using WebApp.Pages.Calendar;

namespace WebApp.Tests.Pages.Calendar;

public class ComputeRangeTests
{
    // Wednesday, Sep 2 2026 -- ISO week 36, mid-week so week-alignment is actually exercised.
    private static readonly DateOnly Anchor = new(2026, 9, 2);

    [Fact]
    public void RollingWeek_StartsAtAnchor_SpansSevenDays()
    {
        var range = IndexModel.ComputeRange(CalendarViewMode.Week, rolling: true, Anchor);

        Assert.Equal(Anchor, range.Start);
        Assert.Equal(Anchor.AddDays(6), range.End);
    }

    [Fact]
    public void RollingMonth_StartsAtAnchor_SpansThirtyOneDays()
    {
        var range = IndexModel.ComputeRange(CalendarViewMode.Month, rolling: true, Anchor);

        Assert.Equal(Anchor, range.Start);
        Assert.Equal(Anchor.AddDays(30), range.End);
    }

    [Fact]
    public void FullWeek_SnapsToMondayStartSundayEnd()
    {
        var range = IndexModel.ComputeRange(CalendarViewMode.Week, rolling: false, Anchor);

        Assert.Equal(new DateOnly(2026, 8, 31), range.Start); // Monday
        Assert.Equal(new DateOnly(2026, 9, 6), range.End);    // Sunday
        Assert.Equal(DayOfWeek.Monday, range.Start.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, range.End.DayOfWeek);
    }

    [Fact]
    public void FullWeek_LabelIncludesIsoWeekNumber()
    {
        var range = IndexModel.ComputeRange(CalendarViewMode.Week, rolling: false, Anchor);

        Assert.Equal("Week 36, 2026", range.Label);
    }

    [Fact]
    public void FullMonth_SnapsToFirstAndLastDayOfCalendarMonth()
    {
        var range = IndexModel.ComputeRange(CalendarViewMode.Month, rolling: false, Anchor);

        Assert.Equal(new DateOnly(2026, 9, 1), range.Start);
        Assert.Equal(new DateOnly(2026, 9, 30), range.End);
        Assert.Equal("September 2026", range.Label);
    }

    [Fact]
    public void FullWeek_AnchorAlreadyMonday_StaysOnSameWeek()
    {
        var monday = new DateOnly(2026, 8, 31);

        var range = IndexModel.ComputeRange(CalendarViewMode.Week, rolling: false, monday);

        Assert.Equal(monday, range.Start);
        Assert.Equal(new DateOnly(2026, 9, 6), range.End);
    }
}
