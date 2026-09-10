using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public partial class NoteSharingTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task SharedNote_AppearsInRecipientsList()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var title = $"Buy milk {Guid.NewGuid():N}";
        var noteId = await CreateToDoNoteAsync(owner, title);
        await ShareNoteAsync(owner, noteId, viewerUsername);

        var viewerHtml = await viewer.GetStringAsync("/Notes/Index");
        Assert.Contains(title, viewerHtml);
        Assert.Contains("Shared by", viewerHtml);
    }

    [Fact]
    public async Task ViewerOverlay_FolderScheduleAndPriority_AreIndependentOfOwner()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var title = $"Shared task {Guid.NewGuid():N}";
        var noteId = await CreateToDoNoteAsync(owner, title, priority: "High");
        await ShareNoteAsync(owner, noteId, viewerUsername);

        // Viewer sets their own priority.
        var editHtml = await viewer.GetStringAsync($"/Notes/Edit/{noteId}");
        var token = HtmlHelpers.ExtractAntiforgeryToken(editHtml);
        var response = await viewer.PostAsync($"/Notes/Edit/{noteId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Priority"] = "Low",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ownerListHtml = await owner.GetStringAsync("/Notes/Index");
        var viewerListHtml = await viewer.GetStringAsync("/Notes/Index");

        Assert.Contains("High priority", ownerListHtml);
        Assert.Contains("Low priority", viewerListHtml);
    }

    [Fact]
    public async Task Viewer_CannotEditNoteContentOrDeleteIt()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var originalTitle = $"Original {Guid.NewGuid():N}";
        var noteId = await CreateToDoNoteAsync(owner, originalTitle);
        await ShareNoteAsync(owner, noteId, viewerUsername);

        // The viewer's edit page shows a read-only view -- it never renders the title as an editable field,
        // so posting a different title has no way to reach the note's actual content.
        var editHtml = await viewer.GetStringAsync($"/Notes/Edit/{noteId}");
        Assert.DoesNotContain("name=\"Input.Title\"", editHtml);
        Assert.Contains(originalTitle, editHtml);

        // Deleting still requires ownership.
        var deleteToken = HtmlHelpers.ExtractAntiforgeryToken(editHtml);
        var deleteResponse = await viewer.PostAsync($"/Notes/Edit/{noteId}?handler=Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = deleteToken
        }));
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        var ownerListHtml = await owner.GetStringAsync("/Notes/Index");
        Assert.Contains(originalTitle, ownerListHtml);
    }

    [Fact]
    public async Task ToggleDone_ByViewer_RecordsWhoAndIsVisibleToOwner()
    {
        var (owner, ownerUsername) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var title = $"Shared task {Guid.NewGuid():N}";
        var noteId = await CreateToDoNoteAsync(owner, title);
        await ShareNoteAsync(owner, noteId, viewerUsername);

        var viewerListHtml = await viewer.GetStringAsync("/Notes/Index");
        var toggleToken = HtmlHelpers.ExtractAntiforgeryToken(viewerListHtml);
        var toggleResponse = await viewer.PostAsync("/Notes/Index?handler=ToggleDone&id=" + noteId, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = toggleToken
        }));
        Assert.Equal(HttpStatusCode.OK, toggleResponse.StatusCode);

        var ownerCompletedHtml = await owner.GetStringAsync("/Notes?ShowCompleted=true");
        Assert.Contains(title, ownerCompletedHtml);
        Assert.Contains($"Done by {viewerUsername}", ownerCompletedHtml);

        _ = ownerUsername;
    }

    [Fact]
    public async Task DoneByFilter_ShowsOnlyNotesCompletedByThatPerson()
    {
        var (owner, ownerUsername) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var ownDoneTitle = $"Done by owner {Guid.NewGuid():N}";
        var ownerNoteId = await CreateToDoNoteAsync(owner, ownDoneTitle);
        await ToggleDoneAsync(owner, ownerNoteId);

        var sharedTitle = $"Done by viewer {Guid.NewGuid():N}";
        var sharedNoteId = await CreateToDoNoteAsync(owner, sharedTitle);
        await ShareNoteAsync(owner, sharedNoteId, viewerUsername);
        await ToggleDoneAsync(viewer, sharedNoteId);

        var ownerHtml = await owner.GetStringAsync("/Notes/Index");
        var doneByOwnerId = ExtractDoneByOptionId(ownerHtml, ownerUsername);

        var filteredHtml = await owner.GetStringAsync($"/Notes?ShowCompleted=true&DoneBy={doneByOwnerId}");
        Assert.Contains(ownDoneTitle, filteredHtml);
        Assert.DoesNotContain(sharedTitle, filteredHtml);
    }

    [Fact]
    public async Task Unsharing_RemovesItFromViewersList()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var title = $"Temp share {Guid.NewGuid():N}";
        var noteId = await CreateToDoNoteAsync(owner, title);
        await ShareNoteAsync(owner, noteId, viewerUsername);

        var viewerHtmlBefore = await viewer.GetStringAsync("/Notes/Index");
        Assert.Contains(title, viewerHtmlBefore);

        // Re-save with nobody checked -- unshares it.
        var editHtml = await owner.GetStringAsync($"/Notes/Edit/{noteId}");
        var token = HtmlHelpers.ExtractAntiforgeryToken(editHtml);
        await owner.PostAsync($"/Notes/Edit/{noteId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Title"] = title,
            ["__RequestVerificationToken"] = token
        }));

        var viewerHtmlAfter = await viewer.GetStringAsync("/Notes/Index");
        Assert.DoesNotContain(title, viewerHtmlAfter);
    }

    private async Task<(HttpClient Client, string Username)> CreateConnectedAccountAsync()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);
        await client.GetStringAsync("/Settings/Index"); // lazily creates the profile/username
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
        var acceptId = ExtractActionRouteId(recipientHtml, "Accept");
        var acceptToken = HtmlHelpers.ExtractAntiforgeryToken(recipientHtml);
        await recipient.PostAsync($"/Family/Index?handler=Accept&id={acceptId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = acceptToken
        }));
    }

    private static async Task<int> CreateToDoNoteAsync(HttpClient client, string title, string? priority = null)
    {
        var createPageHtml = await client.GetStringAsync("/Notes/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        var fields = new Dictionary<string, string>
        {
            ["Input.NoteType"] = "ToDo",
            ["Input.Title"] = title,
            ["__RequestVerificationToken"] = token
        };
        if (priority is not null)
        {
            fields["Input.Priority"] = priority;
        }

        var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var listHtml = await client.GetStringAsync("/Notes/Index");
        var match = Regex.Match(listHtml, "id=\"note-(\\d+)\"[\\s\\S]{0,400}?" + Regex.Escape(title));
        if (!match.Success)
        {
            // Fall back to the highest note id present -- this is a fresh throwaway account, so it's the one just created.
            var idMatches = Regex.Matches(listHtml, "id=\"note-(\\d+)\"");
            Assert.NotEmpty(idMatches);
            return idMatches.Select(m => int.Parse(m.Groups[1].Value)).Max();
        }
        return int.Parse(match.Groups[1].Value);
    }

    /// <summary>
    /// Posts the Edit form back with sharing added, same as a real browser would --
    /// the whole form resubmits every time, so this must preserve the note's
    /// existing field values (Priority, due date, ...) rather than just Title.
    /// </summary>
    private static async Task ShareNoteAsync(HttpClient owner, int noteId, string withUsername)
    {
        var editHtml = await owner.GetStringAsync($"/Notes/Edit/{noteId}");
        var token = HtmlHelpers.ExtractAntiforgeryToken(editHtml);
        var titleMatch = Regex.Match(editHtml, "name=\"Input\\.Title\"[^>]*>([\\s\\S]*?)</textarea>");
        var checkboxMatch = Regex.Match(editHtml, $"value=\"([^\"]+)\" id=\"share-[^\"]+\"[^>]*>\\s*<label class=\"form-check-label\" for=\"share-\\1\">{Regex.Escape(withUsername)}</label>");
        Assert.True(checkboxMatch.Success, $"Could not find a share checkbox for '{withUsername}' in:\n{editHtml}");

        var fields = new Dictionary<string, string>
        {
            ["Input.Title"] = titleMatch.Success ? System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value).Trim() : "note",
            ["Input.ShareWithUserIds"] = checkboxMatch.Groups[1].Value,
            ["__RequestVerificationToken"] = token
        };

        AddIfPresent(fields, "Input.Priority", ExtractSelectedOption(editHtml, "Input.Priority"));
        AddIfPresent(fields, "Input.DueDate", ExtractInputValue(editHtml, "Input.DueDate"));
        AddIfPresent(fields, "Input.DueTime", ExtractInputValue(editHtml, "Input.DueTime"));

        var response = await owner.PostAsync($"/Notes/Edit/{noteId}", new FormUrlEncodedContent(fields));
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

    private static async Task ToggleDoneAsync(HttpClient client, int noteId)
    {
        var listHtml = await client.GetStringAsync("/Notes/Index");
        var token = HtmlHelpers.ExtractAntiforgeryToken(listHtml);
        var response = await client.PostAsync($"/Notes/Index?handler=ToggleDone&id={noteId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static int ExtractActionRouteId(string html, string handler)
    {
        var actionMatch = Regex.Match(html, $"action=\"([^\"]*handler={handler}[^\"]*)\"");
        Assert.True(actionMatch.Success, $"Could not find an action for handler '{handler}' in:\n{html}");

        var idMatch = Regex.Match(actionMatch.Groups[1].Value, "id=(\\d+)");
        Assert.True(idMatch.Success);
        return int.Parse(idMatch.Groups[1].Value);
    }

    private static string ExtractDoneByOptionId(string html, string username)
    {
        var match = Regex.Match(html, $"<option value=\"([^\"]+)\">{Regex.Escape(username)}</option>");
        Assert.True(match.Success, $"Could not find a Done-by option for '{username}' in:\n{html}");
        return match.Groups[1].Value;
    }
}
