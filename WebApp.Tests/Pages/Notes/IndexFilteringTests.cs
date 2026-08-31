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
}
