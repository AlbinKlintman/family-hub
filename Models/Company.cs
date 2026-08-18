namespace WebApp.Models;

public class Company
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }

    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
}
