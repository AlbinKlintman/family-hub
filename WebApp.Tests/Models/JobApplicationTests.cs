using WebApp.Models;

namespace WebApp.Tests.Models;

public class JobApplicationTests
{
    private static JobApplication NewApplication() => new()
    {
        UserId = "user-1",
        RoleName = "Backend Developer"
    };

    [Fact]
    public void SetStatus_ToApplied_SetsAppliedDateToToday_WhenNotAlreadySet()
    {
        var application = NewApplication();

        application.SetStatus(ApplicationStatus.Applied);

        Assert.Equal(ApplicationStatus.Applied, application.Status);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), application.AppliedDate);
    }

    [Fact]
    public void SetStatus_ToApplied_DoesNotOverwriteExistingAppliedDate()
    {
        var application = NewApplication();
        var originalDate = new DateOnly(2020, 1, 1);
        application.AppliedDate = originalDate;

        application.SetStatus(ApplicationStatus.Applied);

        Assert.Equal(originalDate, application.AppliedDate);
    }

    [Theory]
    [InlineData(ApplicationStatus.Searching)]
    [InlineData(ApplicationStatus.TestScheduled)]
    [InlineData(ApplicationStatus.TestDone)]
    [InlineData(ApplicationStatus.InterviewScheduled)]
    [InlineData(ApplicationStatus.InterviewDone)]
    [InlineData(ApplicationStatus.Rejected)]
    public void SetStatus_ToNonApplied_DoesNotSetAppliedDate(ApplicationStatus status)
    {
        var application = NewApplication();

        application.SetStatus(status);

        Assert.Equal(status, application.Status);
        Assert.Null(application.AppliedDate);
    }

    [Fact]
    public void SetStatus_MovingBackward_PreservesAlreadySetAppliedDate()
    {
        var application = NewApplication();
        application.SetStatus(ApplicationStatus.Applied);
        var appliedDate = application.AppliedDate;

        application.SetStatus(ApplicationStatus.Searching);

        Assert.Equal(ApplicationStatus.Searching, application.Status);
        Assert.Equal(appliedDate, application.AppliedDate);
    }
}
