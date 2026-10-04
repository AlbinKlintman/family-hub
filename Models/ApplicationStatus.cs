namespace WebApp.Models;

public enum ApplicationStatus
{
    Searching,
    Applied,
    TestScheduled,
    TestDone,
    InterviewScheduled,
    InterviewDone,
    Offered,
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
        ApplicationStatus.Offered => "Offer Received",
        ApplicationStatus.Rejected => "Rejected",
        _ => status.ToString()
    };
}
