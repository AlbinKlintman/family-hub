namespace WebApp.Models;

public enum ApplicationStatus
{
    Searching,
    Applied,
    TestScheduled,
    TestDone,
    InterviewScheduled,
    InterviewDone,
    Rejected
}

public static class ApplicationStatusExtensions
{
    public static string ToDisplayName(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.Searching => "Searching",
        ApplicationStatus.Applied => "Applied",
        ApplicationStatus.TestScheduled => "Test Scheduled",
        ApplicationStatus.TestDone => "Test Done",
        ApplicationStatus.InterviewScheduled => "Interview Scheduled",
        ApplicationStatus.InterviewDone => "Interview Done",
        ApplicationStatus.Rejected => "Rejected",
        _ => status.ToString()
    };
}
