using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public partial class BadgeCountTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task Unauthenticated_request_is_rejected()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/badge-count");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Counts_past_due_note_and_overdue_interview_but_not_a_future_note()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var today = DateOnly.FromDateTime(DateTime.Now);

        await CreateToDoNoteAsync(client, $"Past due note {Guid.NewGuid():N}", today.AddDays(-1));
        await CreateToDoNoteAsync(client, $"Future note {Guid.NewGuid():N}", today.AddDays(3));
        await CreateApplicationWithOverdueInterviewAsync(client, $"Overdue interview role {Guid.NewGuid():N}", today.AddDays(-2));

        var response = await client.GetAsync("/api/badge-count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("count").GetInt32());
    }

    private static async Task CreateToDoNoteAsync(HttpClient client, string title, DateOnly dueDate)
    {
        var createNotePageHtml = await client.GetStringAsync("/Notes/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createNotePageHtml);

        var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NoteType"] = "ToDo",
            ["Input.Title"] = title,
            ["Input.DueDate"] = dueDate.ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task CreateApplicationWithOverdueInterviewAsync(HttpClient client, string roleName, DateOnly interviewDate)
    {
        var createPageHtml = await client.GetStringAsync("/Applications/Create");
        var createToken = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);
        var createResponse = await client.PostAsync("/Applications/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.RoleName"] = roleName,
            ["Input.Descriptions[0]"] = "",
            ["Input.Links[0]"] = "",
            ["__RequestVerificationToken"] = createToken
        }));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        // Fresh throwaway account -- this is the only application on the board, so the first id found is it.
        var boardHtml = await client.GetStringAsync("/Board/Index");
        var id = ExtractFirstDataId(boardHtml);

        var editPageHtml = await client.GetStringAsync($"/Applications/Edit/{id}");
        var editToken = HtmlHelpers.ExtractAntiforgeryToken(editPageHtml);
        var editResponse = await client.PostAsync($"/Applications/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.RoleName"] = roleName,
            ["Input.Descriptions[0]"] = "",
            ["Input.Links[0]"] = "",
            ["Input.Status"] = "InterviewScheduled",
            ["Input.InterviewDate"] = interviewDate.ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = editToken
        }));
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
    }

    private static int ExtractFirstDataId(string html)
    {
        var match = DataIdRegex().Match(html);
        Assert.True(match.Success);
        return int.Parse(match.Groups[1].Value);
    }

    [GeneratedRegex("data-id=\"(\\d+)\"")]
    private static partial Regex DataIdRegex();
}
