using System.Net;
using System.Web;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class SearchTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task Notes_search_matches_title_and_excludes_non_matching_notes()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var matchingTitle = $"Alpha groceries {Guid.NewGuid():N}";
        var otherTitle = $"Beta laundry run {Guid.NewGuid():N}";

        await CreateToDoNoteAsync(client, matchingTitle);
        await CreateToDoNoteAsync(client, otherTitle);

        var searchTerm = matchingTitle[..5];
        var resultsHtml = await client.GetStringAsync($"/Notes?Search={HttpUtility.UrlEncode(searchTerm)}");

        Assert.Contains(matchingTitle, resultsHtml);
        Assert.DoesNotContain(otherTitle, resultsHtml);
    }

    private static async Task CreateToDoNoteAsync(HttpClient client, string title)
    {
        var createNotePageHtml = await client.GetStringAsync("/Notes/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createNotePageHtml);

        var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NoteType"] = "ToDo",
            ["Input.Title"] = title,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
