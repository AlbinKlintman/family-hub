using System.Net;
using System.Web;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class MediaTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task Create_then_list_shows_entry_with_progress_and_link()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var title = $"Frieren {Guid.NewGuid():N}";

        var createPageHtml = await client.GetStringAsync("/Media/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        var createResponse = await client.PostAsync("/Media/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Title"] = title,
            ["Input.Type"] = "Anime",
            ["Input.Status"] = "InProgress",
            ["Input.Rating"] = "9",
            ["Input.Season"] = "1",
            ["Input.Episode"] = "12",
            ["Input.Links[0]"] = "https://example.com/watch",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var listHtml = await client.GetStringAsync("/Media/Index");

        Assert.Contains(title, listHtml);
        Assert.Contains("Watching", listHtml);
        Assert.Contains("S1 E12", listHtml);
        Assert.Contains("Great", listHtml);
        Assert.Contains("https://example.com/watch", listHtml);
    }

    [Fact]
    public async Task Type_filter_excludes_other_types()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var mangaTitle = $"Berserk {Guid.NewGuid():N}";
        var movieTitle = $"Your Name {Guid.NewGuid():N}";

        await CreateEntryAsync(client, mangaTitle, "Manga");
        await CreateEntryAsync(client, movieTitle, "Movie");

        var filteredHtml = await client.GetStringAsync($"/Media?Type=Manga");

        Assert.Contains(mangaTitle, filteredHtml);
        Assert.DoesNotContain(movieTitle, filteredHtml);
    }

    [Fact]
    public async Task Search_matches_title_substring()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var matchingTitle = $"Alpha anime {Guid.NewGuid():N}";
        var otherTitle = $"Beta manga {Guid.NewGuid():N}";

        await CreateEntryAsync(client, matchingTitle, "Anime");
        await CreateEntryAsync(client, otherTitle, "Manga");

        var searchTerm = matchingTitle[..5];
        var resultsHtml = await client.GetStringAsync($"/Media?Search={HttpUtility.UrlEncode(searchTerm)}");

        Assert.Contains(matchingTitle, resultsHtml);
        Assert.DoesNotContain(otherTitle, resultsHtml);
    }

    private static async Task CreateEntryAsync(HttpClient client, string title, string type)
    {
        var createPageHtml = await client.GetStringAsync("/Media/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        var response = await client.PostAsync("/Media/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Title"] = title,
            ["Input.Type"] = type,
            ["Input.Status"] = "PlanToStart",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
