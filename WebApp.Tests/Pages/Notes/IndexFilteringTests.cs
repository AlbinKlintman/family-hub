using WebApp.Models;
using WebApp.Pages.Notes;

namespace WebApp.Tests.Pages.Notes;

public class IndexFilteringTests
{
    private static readonly DateTime Today = new(2026, 6, 15);

    private static ToDoNote NewToDo(string userId = "u1") => new() { UserId = userId, Title = "test" };

    [Fact]
    public void DoneNote_IsPastOrCompleted_RegardlessOfDate()
    {
        var note = NewToDo();
        note.IsDone = true;
        note.DueDate = DateOnly.FromDateTime(Today.AddDays(5)); // future, but done

        Assert.True(IndexModel.IsPastOrCompleted(note, Today));
    }

    [Fact]
    public void NotDone_FutureDueDate_IsActive()
    {
        var note = NewToDo();
        note.DueDate = DateOnly.FromDateTime(Today.AddDays(1));

        Assert.False(IndexModel.IsPastOrCompleted(note, Today));
    }

    [Fact]
    public void NotDone_PastDueDate_IsPastOrCompleted()
    {
        var note = NewToDo();
        note.DueDate = DateOnly.FromDateTime(Today.AddDays(-1));

        Assert.True(IndexModel.IsPastOrCompleted(note, Today));
    }

    [Fact]
    public void NotDone_DueToday_IsStillActive()
    {
        // Today isn't "past" yet -- only strictly earlier days are.
        var note = NewToDo();
        note.DueDate = DateOnly.FromDateTime(Today);

        Assert.False(IndexModel.IsPastOrCompleted(note, Today));
    }

    [Fact]
    public void NotDone_NoDueDateAtAll_IsActive()
    {
        // A to-do with no date never ages out on its own -- only IsDone moves it.
        var note = NewToDo();

        Assert.False(IndexModel.IsPastOrCompleted(note, Today));
    }

    [Fact]
    public void LaundryNote_PastDay_IsPastOrCompleted()
    {
        var note = new LaundryNote { UserId = "u1", Day = DateOnly.FromDateTime(Today.AddDays(-2)) };

        Assert.True(IndexModel.IsPastOrCompleted(note, Today));
    }

    private static readonly DateOnly TodayDate = DateOnly.FromDateTime(Today);

    [Theory]
    [InlineData(0, NoteDueFilter.Today, true)]
    [InlineData(1, NoteDueFilter.Today, false)]
    [InlineData(1, NoteDueFilter.Tomorrow, true)]
    [InlineData(2, NoteDueFilter.Tomorrow, false)]
    [InlineData(6, NoteDueFilter.ThisWeek, true)]
    [InlineData(7, NoteDueFilter.ThisWeek, false)]
    [InlineData(-1, NoteDueFilter.ThisWeek, false)]
    [InlineData(30, NoteDueFilter.ThisMonth, true)]
    [InlineData(31, NoteDueFilter.ThisMonth, false)]
    public void MatchesDueFilter_ChecksDateAgainstRange(int daysFromToday, NoteDueFilter filter, bool expected)
    {
        var sortDate = TodayDate.AddDays(daysFromToday).ToDateTime(new TimeOnly(9, 0));

        Assert.Equal(expected, IndexModel.MatchesDueFilter(sortDate, filter, TodayDate));
    }

    [Fact]
    public void MatchesDueFilter_NoDateAtAll_NeverMatches()
    {
        Assert.False(IndexModel.MatchesDueFilter(DateTime.MaxValue, NoteDueFilter.ThisMonth, TodayDate));
    }

    [Theory]
    [InlineData(1, TimeUnit.Days)]
    [InlineData(3, TimeUnit.Hours)]
    [InlineData(45, TimeUnit.Minutes)]
    public void AdvanceOccurrence_AddsIntervalOnce(int value, TimeUnit unit)
    {
        var date = TodayDate;
        var time = new TimeOnly(9, 0);

        var (nextDate, nextTime) = IndexModel.AdvanceOccurrence(date, time, value, unit);

        var expected = date.ToDateTime(time) + unit.ToTimeSpan(value);
        Assert.Equal(DateOnly.FromDateTime(expected), nextDate);
        Assert.Equal(TimeOnly.FromDateTime(expected), nextTime);
    }
}
