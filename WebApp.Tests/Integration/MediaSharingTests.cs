using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public partial class MediaSharingTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task SharedEntry_AppearsInRecipientsList_WithSharedByLabel()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var title = $"Frieren {Guid.NewGuid():N}";
        var entryId = await CreateAnimeEntryAsync(owner, title);
        await ShareEntryAsync(owner, entryId, viewerUsername);

        var viewerHtml = await viewer.GetStringAsync("/Media/Index");
        Assert.Contains(title, viewerHtml);
        Assert.Contains("Shared by", viewerHtml);
    }

    [Fact]
    public async Task Rating_IsIndependentPerPerson()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var title = $"Frieren {Guid.NewGuid():N}";
        var entryId = await CreateAnimeEntryAsync(owner, title);
        await ShareEntryAsync(owner, entryId, viewerUsername);
        await SetOwnRatingAsync(owner, entryId, "9");

        var viewerEditHtml = await viewer.GetStringAsync($"/Media/Edit/{entryId}");
        var token = HtmlHelpers.ExtractAntiforgeryToken(viewerEditHtml);
        var response = await viewer.PostAsync($"/Media/Edit/{entryId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Rating"] = "3",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ownerHtml = await owner.GetStringAsync("/Media/Index");
        var viewerHtml = await viewer.GetStringAsync("/Media/Index");
        Assert.Contains("&#9733; 9", ownerHtml);
        Assert.Contains("&#9733; 3", viewerHtml);
    }

    [Fact]
    public async Task IncrementProgress_ByViewer_UpdatesTheSharedCanonicalProgress()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var title = $"One Piece {Guid.NewGuid():N}";
        var entryId = await CreateMangaEntryAsync(owner, title);
        await ShareEntryAsync(owner, entryId, viewerUsername);

        var viewerListHtml = await viewer.GetStringAsync("/Media/Index");
        var token = HtmlHelpers.ExtractAntiforgeryToken(viewerListHtml);
        var response = await viewer.PostAsync($"/Media/Index?handler=IncrementProgress&id={entryId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ownerHtml = await owner.GetStringAsync("/Media/Index");
        Assert.Contains("Ch. 1", ownerHtml);
    }

    [Fact]
    public async Task Viewer_CannotEditTitleOrDeleteEntry()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var title = $"Berserk {Guid.NewGuid():N}";
        var entryId = await CreateMangaEntryAsync(owner, title);
        await ShareEntryAsync(owner, entryId, viewerUsername);

        var editHtml = await viewer.GetStringAsync($"/Media/Edit/{entryId}");
        Assert.DoesNotContain("name=\"Input.Title\"", editHtml);
        Assert.Contains(title, editHtml);

        var deleteToken = HtmlHelpers.ExtractAntiforgeryToken(editHtml);
        var deleteResponse = await viewer.PostAsync($"/Media/Edit/{entryId}?handler=Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = deleteToken
        }));
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        Assert.Contains(title, await owner.GetStringAsync("/Media/Index"));
    }

    private async Task<(HttpClient Client, string Username)> CreateConnectedAccountAsync()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);
        await client.GetStringAsync("/Settings/Index");
        return (client, email[..email.IndexOf('@')]);
    }

    private static async Task ConnectAsync(HttpClient requester, HttpClient recipient, string recipientUsername)
    {
        var requestPageHtml = await requester.GetStringAsync("/Family/Index");
        var requestToken = HtmlHelpers.ExtractAntiforgeryToken(requestPageHtml);
        await requester.PostAsync("/Family/Index?handler=SendRequest", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NewRequestUsername"] = recipientUsername,
            ["__RequestVerificationToken"] = requestToken
        }));

        var recipientHtml = await recipient.GetStringAsync("/Family/Index");
        var actionMatch = Regex.Match(recipientHtml, "action=\"([^\"]*handler=Accept[^\"]*)\"");
        Assert.True(actionMatch.Success);
        var idMatch = Regex.Match(actionMatch.Groups[1].Value, "id=(\\d+)");
        Assert.True(idMatch.Success);

        var acceptToken = HtmlHelpers.ExtractAntiforgeryToken(recipientHtml);
        await recipient.PostAsync($"/Family/Index?handler=Accept&id={idMatch.Groups[1].Value}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = acceptToken
        }));
    }

    private static async Task<int> CreateAnimeEntryAsync(HttpClient client, string title) => await CreateEntryAsync(client, title, "Anime");
    private static async Task<int> CreateMangaEntryAsync(HttpClient client, string title) => await CreateEntryAsync(client, title, "Manga");

    private static async Task<int> CreateEntryAsync(HttpClient client, string title, string type)
    {
        var createPageHtml = await client.GetStringAsync("/Media/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);
        var response = await client.PostAsync("/Media/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Title"] = title,
            ["Input.Type"] = type,
            ["Input.Status"] = "InProgress",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var listHtml = await client.GetStringAsync("/Media/Index");
        var idMatches = Regex.Matches(listHtml, "id=\"media-(\\d+)\"");
        Assert.NotEmpty(idMatches);
        return idMatches.Select(m => int.Parse(m.Groups[1].Value)).Max();
    }

    /// <summary>
    /// Posts the owner's Edit form back, same as a real browser would -- the
    /// whole form resubmits every time, so this preserves Title/Type/Status and
    /// every currently-checked share checkbox rather than assuming just one field.
    /// </summary>
    private static async Task<Dictionary<string, string>> ScrapeOwnerFormFieldsAsync(HttpClient owner, int entryId)
    {
        var editHtml = await owner.GetStringAsync($"/Media/Edit/{entryId}");
        var token = HtmlHelpers.ExtractAntiforgeryToken(editHtml);

        var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };

        AddIfPresent(fields, "Input.Title", ExtractInputValue(editHtml, "Input.Title"));
        AddIfPresent(fields, "Input.Type", ExtractSelectedOption(editHtml, "Input.Type"));
        AddIfPresent(fields, "Input.Status", ExtractSelectedOption(editHtml, "Input.Status"));

        return fields;
    }

    private static async Task ShareEntryAsync(HttpClient owner, int entryId, string withUsername)
    {
        var editHtml = await owner.GetStringAsync($"/Media/Edit/{entryId}");
        var checkboxMatch = Regex.Match(editHtml, $"value=\"([^\"]+)\" id=\"share-[^\"]+\"[^>]*>\\s*<label class=\"form-check-label\" for=\"share-\\1\">{Regex.Escape(withUsername)}</label>");
        Assert.True(checkboxMatch.Success, $"Could not find a share checkbox for '{withUsername}' in:\n{editHtml}");

        var fields = await ScrapeOwnerFormFieldsAsync(owner, entryId);
        fields["Input.ShareWithUserIds"] = checkboxMatch.Groups[1].Value;

        var response = await owner.PostAsync($"/Media/Edit/{entryId}", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task SetOwnRatingAsync(HttpClient owner, int entryId, string rating)
    {
        var editHtml = await owner.GetStringAsync($"/Media/Edit/{entryId}");
        var shareIds = Regex.Matches(editHtml, "name=\"Input\\.ShareWithUserIds\" value=\"([^\"]+)\"[^>]*checked=\"checked\"")
            .Select(m => m.Groups[1].Value)
            .ToList();

        var fields = await ScrapeOwnerFormFieldsAsync(owner, entryId);
        fields["Input.Rating"] = rating;
        for (var i = 0; i < shareIds.Count; i++)
        {
            fields[$"Input.ShareWithUserIds[{i}]"] = shareIds[i];
        }

        var response = await owner.PostAsync($"/Media/Edit/{entryId}", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static void AddIfPresent(Dictionary<string, string> fields, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            fields[key] = value;
        }
    }

    private static string? ExtractInputValue(string html, string fieldName)
    {
        var match = Regex.Match(html, $"name=\"{Regex.Escape(fieldName)}\"[^>]*value=\"([^\"]*)\"");
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    private static string? ExtractSelectedOption(string html, string selectName)
    {
        var selectMatch = Regex.Match(html, $"<select[^>]*name=\"{Regex.Escape(selectName)}\"[\\s\\S]*?</select>");
        if (!selectMatch.Success)
        {
            return null;
        }

        var optionMatch = Regex.Match(selectMatch.Value, "<option value=\"([^\"]*)\" selected=\"selected\"");
        return optionMatch.Success ? optionMatch.Groups[1].Value : null;
    }
}
