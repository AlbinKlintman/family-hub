using System.Net;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class DashboardTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task Home_page_shows_due_soon_note_and_application_count()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var noteTitle = $"Dashboard test note {Guid.NewGuid():N}";
        var createNotePageHtml = await client.GetStringAsync("/Notes/Create");
        var createNoteToken = HtmlHelpers.ExtractAntiforgeryToken(createNotePageHtml);
        var createNoteResponse = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NoteType"] = "ToDo",
            ["Input.Title"] = noteTitle,
            ["Input.DueDate"] = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = createNoteToken
        }));
        Assert.Equal(HttpStatusCode.OK, createNoteResponse.StatusCode);

        var roleName = $"Dashboard test role {Guid.NewGuid():N}";
        var createApplicationPageHtml = await client.GetStringAsync("/Applications/Create");
        var createApplicationToken = HtmlHelpers.ExtractAntiforgeryToken(createApplicationPageHtml);
        var createApplicationResponse = await client.PostAsync("/Applications/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.RoleName"] = roleName,
            ["Input.Descriptions[0]"] = "",
            ["Input.Links[0]"] = "",
            ["__RequestVerificationToken"] = createApplicationToken
        }));
        Assert.Equal(HttpStatusCode.OK, createApplicationResponse.StatusCode);

        var homeHtml = await client.GetStringAsync("/");

        Assert.Contains(noteTitle, homeHtml);
        Assert.Contains("Applications in progress", homeHtml);
    }
}
