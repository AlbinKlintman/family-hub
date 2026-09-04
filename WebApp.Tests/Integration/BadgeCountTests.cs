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
    public async Task Counts_notes_and_applications_separately_and_ignores_future_ones()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var today = DateOnly.FromDateTime(DateTime.Now);

        await CreateToDoNoteAsync(client, $"Past due note {Guid.NewGuid():N}", today.AddDays(-1));
        await CreateToDoNoteAsync(client, $"Due today note {Guid.NewGuid():N}", today);
        await CreateToDoNoteAsync(client, $"Future note {Guid.NewGuid():N}", today.AddDays(3));

        await CreateApplicationAsync(client, $"Overdue interview role {Guid.NewGuid():N}",
            "InterviewScheduled", "Input.InterviewDate", today.AddDays(-2));
        await CreateApplicationAsync(client, $"Test due today role {Guid.NewGuid():N}",
            "TestScheduled", "Input.TestDate", today);
        await CreateApplicationAsync(client, $"Future interview role {Guid.NewGuid():N}",
            "InterviewScheduled", "Input.InterviewDate", today.AddDays(5));

        var response = await client.GetAsync("/api/badge-count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("notes").GetInt32());
        Assert.Equal(2, body.GetProperty("applications").GetInt32());
        Assert.Equal(4, body.GetProperty("total").GetInt32());
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

    private static async Task CreateApplicationAsync(HttpClient client, string roleName, string status, string dateField, DateOnly date)
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

        // Newest card on the board is the one we just created -- its id is the highest data-id present.
        var boardHtml = await client.GetStringAsync("/Board/Index");
        var id = ExtractHighestDataId(boardHtml);

        var editPageHtml = await client.GetStringAsync($"/Applications/Edit/{id}");
        var editToken = HtmlHelpers.ExtractAntiforgeryToken(editPageHtml);
        var editResponse = await client.PostAsync($"/Applications/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.RoleName"] = roleName,
            ["Input.Descriptions[0]"] = "",
            ["Input.Links[0]"] = "",
            ["Input.Status"] = status,
            [dateField] = date.ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = editToken
        }));
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
    }

    private static int ExtractHighestDataId(string html)
    {
        var matches = DataIdRegex().Matches(html);
        Assert.NotEmpty(matches);
        return matches.Select(m => int.Parse(m.Groups[1].Value)).Max();
    }

    [GeneratedRegex("data-id=\"(\\d+)\"")]
    private static partial Regex DataIdRegex();
}
