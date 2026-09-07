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
        Assert.Contains("media-tile-rating", listHtml);
        Assert.Contains("https://example.com/watch", listHtml);

        var idMatch = System.Text.RegularExpressions.Regex.Match(listHtml, "id=\"media-(\\d+)\"");
        Assert.True(idMatch.Success);
        var editPageHtml = await client.GetStringAsync($"/Media/Edit/{idMatch.Groups[1].Value}");
        Assert.Contains("9 - Great", editPageHtml);
    }

    [Fact]
    public async Task Create_with_cover_image_url_shows_it_on_the_card_and_edit_page()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var title = $"Berserk {Guid.NewGuid():N}";
        var coverUrl = "https://example.com/covers/berserk.jpg";

        var createPageHtml = await client.GetStringAsync("/Media/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        var createResponse = await client.PostAsync("/Media/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Title"] = title,
            ["Input.Type"] = "Manga",
            ["Input.Status"] = "InProgress",
            ["Input.CoverImageUrl"] = coverUrl,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var listHtml = await client.GetStringAsync("/Media/Index");
        Assert.Contains($"src=\"{coverUrl}\"", listHtml);
        Assert.Contains("media-tile-cover", listHtml);

        var idMatch = System.Text.RegularExpressions.Regex.Match(listHtml, "id=\"media-(\\d+)\"");
        Assert.True(idMatch.Success);

        var editPageHtml = await client.GetStringAsync($"/Media/Edit/{idMatch.Groups[1].Value}");
        Assert.Contains(coverUrl, editPageHtml);
    }

    [Fact]
    public async Task Create_with_invalid_cover_image_url_shows_validation_error()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var title = $"Bad Cover {Guid.NewGuid():N}";

        var createPageHtml = await client.GetStringAsync("/Media/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        var createResponse = await client.PostAsync("/Media/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Title"] = title,
            ["Input.Type"] = "Manga",
            ["Input.Status"] = "InProgress",
            ["Input.CoverImageUrl"] = "not-a-url",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var body = await createResponse.Content.ReadAsStringAsync();
        Assert.Contains("Enter a valid URL", body);

        var listHtml = await client.GetStringAsync("/Media/Index");
        Assert.DoesNotContain(title, listHtml);
    }

    [Fact]
    public async Task Create_shows_rating_dropdown_with_labeled_options()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var createPageHtml = await client.GetStringAsync("/Media/Create");

        Assert.Contains("10 - Masterpiece", createPageHtml);
        Assert.Contains("4 - Bad", createPageHtml);
        Assert.Contains("2 - Horrible", createPageHtml);
        Assert.Contains("1 - Appalling", createPageHtml);
    }

    [Fact]
    public async Task InProgress_entries_are_listed_before_others()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var completedTitle = $"Aaa Already Done {Guid.NewGuid():N}";
        var inProgressTitle = $"Zzz Still Going {Guid.NewGuid():N}";

        await CreateEntryAsync(client, completedTitle, "Anime", "Completed");
        await CreateEntryAsync(client, inProgressTitle, "Anime", "InProgress");

        var listHtml = await client.GetStringAsync("/Media/Index");

        Assert.True(listHtml.IndexOf(inProgressTitle, StringComparison.Ordinal) < listHtml.IndexOf(completedTitle, StringComparison.Ordinal));
    }

    [Fact]
    public async Task IncrementProgress_Manga_AddsOneChapter_AndCardLinksToEdit()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var title = $"One Piece {Guid.NewGuid():N}";
        await CreateEntryAsync(client, title, "Manga", "InProgress");

        var listHtml = await client.GetStringAsync("/Media/Index");
        Assert.Contains("data-href=\"/Media/Edit/", listHtml);
        Assert.Contains("+1 chapter", System.Net.WebUtility.HtmlDecode(listHtml));

        var token = HtmlHelpers.ExtractAntiforgeryToken(listHtml);
        var idMatch = System.Text.RegularExpressions.Regex.Match(listHtml, "id=\"media-(\\d+)\"");
        Assert.True(idMatch.Success);
        var id = idMatch.Groups[1].Value;

        var incrementResponse = await client.PostAsync($"/Media/Index?handler=IncrementProgress&id={id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, incrementResponse.StatusCode);

        var updatedHtml = await client.GetStringAsync("/Media/Index");
        Assert.Contains("Ch. 1", updatedHtml);
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

    [Fact]
    public async Task Create_Book_tracks_chapter_and_page_shows_reading_status_and_increment_button()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var title = $"Mistborn {Guid.NewGuid():N}";

        var createPageHtml = await client.GetStringAsync("/Media/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        var createResponse = await client.PostAsync("/Media/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Title"] = title,
            ["Input.Type"] = "Book",
            ["Input.Status"] = "InProgress",
            ["Input.Chapter"] = "5",
            ["Input.Page"] = "112",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var listHtml = await client.GetStringAsync("/Media/Index");

        Assert.Contains(title, listHtml);
        Assert.Contains("Reading", listHtml);
        Assert.Contains("Ch. 5 (p. 112)", listHtml);
        Assert.Contains("+1 chapter", System.Net.WebUtility.HtmlDecode(listHtml));
    }

    private static async Task CreateEntryAsync(HttpClient client, string title, string type, string status = "PlanToStart")
    {
        var createPageHtml = await client.GetStringAsync("/Media/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        var response = await client.PostAsync("/Media/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Title"] = title,
            ["Input.Type"] = type,
            ["Input.Status"] = status,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
