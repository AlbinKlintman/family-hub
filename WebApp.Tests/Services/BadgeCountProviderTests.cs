using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests.Services;

public class BadgeCountProviderTests
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    private static ToDoNote NewToDo(DateOnly? dueDate = null) => new() { UserId = "u1", Title = "test", DueDate = dueDate };

    [Fact]
    public void IsNoteDue_PastDueDate_IsTrue()
    {
        Assert.True(BadgeCountProvider.IsNoteDue(NewToDo(Today.AddDays(-1)), Today));
    }

    [Fact]
    public void IsNoteDue_DueToday_IsTrue()
    {
        Assert.True(BadgeCountProvider.IsNoteDue(NewToDo(Today), Today));
    }

    [Fact]
    public void IsNoteDue_FutureDueDate_IsFalse()
    {
        Assert.False(BadgeCountProvider.IsNoteDue(NewToDo(Today.AddDays(1)), Today));
    }

    [Fact]
    public void IsNoteDue_NoDueDate_IsFalse()
    {
        Assert.False(BadgeCountProvider.IsNoteDue(NewToDo(), Today));
    }

    private static JobApplication NewApplication(ApplicationStatus status, DateOnly? testDate = null, DateOnly? interviewDate = null) => new()
    {
        UserId = "u1",
        RoleName = "role",
        Status = status,
        TestDate = testDate,
        InterviewDate = interviewDate
    };

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void IsApplicationDue_TestScheduled_ChecksTestDate(int daysFromToday, bool expected)
    {
        var application = NewApplication(ApplicationStatus.TestScheduled, testDate: Today.AddDays(daysFromToday));
        Assert.Equal(expected, BadgeCountProvider.IsApplicationDue(application, Today));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void IsApplicationDue_InterviewScheduled_ChecksInterviewDate(int daysFromToday, bool expected)
    {
        var application = NewApplication(ApplicationStatus.InterviewScheduled, interviewDate: Today.AddDays(daysFromToday));
        Assert.Equal(expected, BadgeCountProvider.IsApplicationDue(application, Today));
    }

    [Theory]
    [InlineData(ApplicationStatus.Searching)]
    [InlineData(ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.TestDone)]
    [InlineData(ApplicationStatus.InterviewDone)]
    [InlineData(ApplicationStatus.Rejected)]
    public void IsApplicationDue_OtherStatuses_AreNeverDue(ApplicationStatus status)
    {
        // Even with a past test/interview date set, once the card has moved past that stage it no longer counts.
        var application = NewApplication(status, testDate: Today.AddDays(-10), interviewDate: Today.AddDays(-10));
        Assert.False(BadgeCountProvider.IsApplicationDue(application, Today));
    }

    [Fact]
    public void IsApplicationDue_TestScheduled_WithNoTestDateSet_IsFalse()
    {
        Assert.False(BadgeCountProvider.IsApplicationDue(NewApplication(ApplicationStatus.TestScheduled), Today));
    }
}
