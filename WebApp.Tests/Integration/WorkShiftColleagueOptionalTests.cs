using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public partial class WorkShiftColleagueOptionalTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task CreatePage_ColleaguesSelect_IsNotRenderedAsRequired()
    {
        using var client = factory.CreateClient();
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, $"{Guid.NewGuid():N}@example.com", "Sup3r$ecretPass!");

        var createNotePageHtml = await client.GetStringAsync("/Notes/Create");

        var colleagueSelectMatch = ColleagueSelectRegex().Match(createNotePageHtml);
        Assert.True(colleagueSelectMatch.Success, "Expected to find the ColleagueIds <select> on the Create Note page.");
        Assert.DoesNotContain("required", colleagueSelectMatch.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WorkShift_WithNoColleaguesSelected_Succeeds()
    {
        using var client = factory.CreateClient();
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, $"{Guid.NewGuid():N}@example.com", "Sup3r$ecretPass!");

        var createNotePageHtml = await client.GetStringAsync("/Notes/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createNotePageHtml);
        var location = $"Integration test shift {Guid.NewGuid():N}";

        var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NoteType"] = "WorkShift",
            ["Input.Location"] = location,
            ["Input.StartTime"] = "07:00",
            ["Input.EndTime"] = "19:00",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var notesIndexHtml = await client.GetStringAsync("/Notes");
        Assert.Contains(location, notesIndexHtml);
    }

    [GeneratedRegex("""<select[^>]*name="Input\.ColleagueIds"[^>]*>""", RegexOptions.Singleline)]
    private static partial Regex ColleagueSelectRegex();
}
