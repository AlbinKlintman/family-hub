namespace WebApp.Models;

/// <summary>
/// The top-level pages a person can hide from Settings. Home and Settings
/// themselves deliberately aren't here -- they can never be hidden.
/// </summary>
public enum AppSection
{
    Calendar,
    Notes,
    JobApplications,
    Media,
    Training,
    Statistics,
    Family
}

public static class AppSectionExtensions
{
    public static string ToDisplayName(this AppSection section) => section switch
    {
        AppSection.JobApplications => "Job Applications",
        _ => section.ToString()
    };

    /// <summary>Which section a Razor page path (e.g. "/Applications/Edit") belongs to, or null for pages that are always available.</summary>
    public static AppSection? SectionForPage(string pagePath)
    {
        var folder = pagePath.TrimStart('/').Split('/')[0];
        return folder switch
        {
            "Calendar" => AppSection.Calendar,
            "Notes" or "Folders" or "Colleagues" => AppSection.Notes,
            "Board" or "Applications" or "Companies" => AppSection.JobApplications,
            "Media" => AppSection.Media,
            "Training" or "Exercises" => AppSection.Training,
            "Statistics" => AppSection.Statistics,
            "Family" => AppSection.Family,
            _ => null
        };
    }
}
