namespace WebApp.Models;

public class JobApplication
{
    public int Id { get; set; }
    public required string UserId { get; set; }

    public required string RoleName { get; set; }
    public ICollection<ApplicationDescription> Descriptions { get; set; } = new List<ApplicationDescription>();
    public ICollection<ApplicationLink> Links { get; set; } = new List<ApplicationLink>();

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Searching;
    public ChanceLevel? Chance { get; set; }

    public DateOnly? AppliedDate { get; set; }
    public DateOnly? InterviewDate { get; set; }
    public TimeOnly? InterviewTime { get; set; }

    public DateTime? InterviewReminder24hSentAtUtc { get; set; }
    public DateTime? InterviewReminder1hSentAtUtc { get; set; }

    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Single choke point for status transitions so the AppliedDate
    /// auto-set rule never needs to be duplicated across PageModels.
    /// </summary>
    public void SetStatus(ApplicationStatus newStatus)
    {
        if (newStatus == ApplicationStatus.Applied && AppliedDate is null)
        {
            AppliedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        Status = newStatus;
    }
}
