using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public partial class ScheduleSharingTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task NoteTaggedWithSharedSchedule_IsVisibleToRecipient_WithoutExplicitNoteShare()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var scheduleId = await CreateScheduleAsync(owner, $"Family {Guid.NewGuid():N}");
        await ShareScheduleAsync(owner, scheduleId, viewerUsername);

        var title = $"Buy milk {Guid.NewGuid():N}";
        await CreateToDoNoteWithScheduleAsync(owner, title, scheduleId);

        var viewerHtml = await viewer.GetStringAsync("/Notes/Index");
        Assert.Contains(title, viewerHtml);
    }

    [Fact]
    public async Task Unsharing_TheSchedule_RemovesItsNotesFromViewersList()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var scheduleId = await CreateScheduleAsync(owner, $"Family {Guid.NewGuid():N}");
        await ShareScheduleAsync(owner, scheduleId, viewerUsername);

        var title = $"Buy milk {Guid.NewGuid():N}";
        await CreateToDoNoteWithScheduleAsync(owner, title, scheduleId);

        Assert.Contains(title, await viewer.GetStringAsync("/Notes/Index"));

        await ShareScheduleAsync(owner, scheduleId, withUsername: null); // re-save with nobody checked

        Assert.DoesNotContain(title, await viewer.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task Viewer_CanSetOwnPriority_OnANoteVisibleOnlyViaSharedSchedule()
    {
        var (owner, _) = await CreateConnectedAccountAsync();
        var (viewer, viewerUsername) = await CreateConnectedAccountAsync();
        await ConnectAsync(owner, viewer, viewerUsername);

        var scheduleId = await CreateScheduleAsync(owner, $"Family {Guid.NewGuid():N}");
        await ShareScheduleAsync(owner, scheduleId, viewerUsername);

        var title = $"Buy milk {Guid.NewGuid():N}";
        var noteId = await CreateToDoNoteWithScheduleAsync(owner, title, scheduleId);

        // No explicit note share exists yet -- the viewer's edit page must still work and let them set a priority.
        var editHtml = await viewer.GetStringAsync($"/Notes/Edit/{noteId}");
        Assert.Contains(title, editHtml);
        var token = HtmlHelpers.ExtractAntiforgeryToken(editHtml);
        var response = await viewer.PostAsync($"/Notes/Edit/{noteId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Priority"] = "High",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var viewerHtml = await viewer.GetStringAsync("/Notes/Index");
        var ownerHtml = await owner.GetStringAsync("/Notes/Index");
        Assert.Contains("High priority", viewerHtml);
        Assert.DoesNotContain("High priority", ownerHtml);
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

    private static async Task<int> CreateScheduleAsync(HttpClient client, string name)
    {
        var pageHtml = await client.GetStringAsync("/Schedules/Index");
        var token = HtmlHelpers.ExtractAntiforgeryToken(pageHtml);
        var response = await client.PostAsync("/Schedules/Index?handler=Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NewScheduleName"] = name,
            ["NewScheduleColor"] = "Blue",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var listHtml = await client.GetStringAsync("/Schedules/Index");
        var editLinkMatch = Regex.Match(listHtml, "/Schedules/Edit/(\\d+)");
        Assert.True(editLinkMatch.Success, $"Could not find a schedule edit link in:\n{listHtml}");
        return int.Parse(editLinkMatch.Groups[1].Value);
    }

    /// <summary>Pass withUsername: null to save the schedule with nobody checked (unshares everyone).</summary>
    private static async Task ShareScheduleAsync(HttpClient owner, int scheduleId, string? withUsername)
    {
        var editHtml = await owner.GetStringAsync($"/Schedules/Edit/{scheduleId}");
        var token = HtmlHelpers.ExtractAntiforgeryToken(editHtml);
        var nameMatch = Regex.Match(editHtml, "name=\"Input\\.Name\"[^>]*value=\"([^\"]*)\"");
        Assert.True(nameMatch.Success);

        var fields = new Dictionary<string, string>
        {
            ["Input.Name"] = System.Net.WebUtility.HtmlDecode(nameMatch.Groups[1].Value),
            ["Input.Color"] = "Blue",
            ["__RequestVerificationToken"] = token
        };

        if (withUsername is not null)
        {
            var checkboxMatch = Regex.Match(editHtml, $"value=\"([^\"]+)\" id=\"share-[^\"]+\"[^>]*>\\s*<label class=\"form-check-label\" for=\"share-\\1\">{Regex.Escape(withUsername)}</label>");
            Assert.True(checkboxMatch.Success, $"Could not find a share checkbox for '{withUsername}' in:\n{editHtml}");
            fields["Input.ShareWithUserIds"] = checkboxMatch.Groups[1].Value;
        }

        var response = await owner.PostAsync($"/Schedules/Edit/{scheduleId}", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<int> CreateToDoNoteWithScheduleAsync(HttpClient client, string title, int scheduleId)
    {
        var createPageHtml = await client.GetStringAsync("/Notes/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NoteType"] = "ToDo",
            ["Input.Title"] = title,
            ["Input.ScheduleId"] = scheduleId.ToString(),
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var listHtml = await client.GetStringAsync("/Notes/Index");
        var idMatches = Regex.Matches(listHtml, "id=\"note-(\\d+)\"");
        Assert.NotEmpty(idMatches);
        return idMatches.Select(m => int.Parse(m.Groups[1].Value)).Max();
    }
}
