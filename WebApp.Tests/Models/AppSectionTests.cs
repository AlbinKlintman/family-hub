using WebApp.Models;

namespace WebApp.Tests.Models;

public class AppSectionTests
{
    [Theory]
    [InlineData("/Calendar/Index", AppSection.Calendar)]
    [InlineData("/Notes/Edit", AppSection.Notes)]
    [InlineData("/Folders/Index", AppSection.Notes)]
    [InlineData("/Applications/Edit", AppSection.JobApplications)]
    [InlineData("/Companies/Index", AppSection.JobApplications)]
    [InlineData("/Exercises/Create", AppSection.Training)]
    [InlineData("/Statistics/Index", AppSection.Statistics)]
    [InlineData("/Family/Index", AppSection.Family)]
    public void PagesMapToTheirSection(string pagePath, AppSection expected)
    {
        Assert.Equal(expected, AppSectionExtensions.SectionForPage(pagePath));
    }

    [Theory]
    [InlineData("/Index")]
    [InlineData("/Settings/Index")]
    [InlineData("/Schedules/Index")]
    [InlineData("/Error")]
    public void AlwaysAvailablePages_HaveNoSection(string pagePath)
    {
        Assert.Null(AppSectionExtensions.SectionForPage(pagePath));
    }
}
