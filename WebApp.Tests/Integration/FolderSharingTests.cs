using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class FolderSharingTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task NoteInFolderSharedViaEdit_IsVisibleToRecipient()
    {
        var (owner, _) = await CreateAccountAsync();
        var (viewer, viewerUsername) = await CreateAccountAsync();
        await SharingTestHelpers.ConnectAsync(owner, viewer, viewerUsername);

        var folderId = await CreateFolderAsync(owner, $"Family {Guid.NewGuid():N}");
        await EditFolderSharingAsync(owner, folderId, viewerUsername);

        var title = $"Buy milk {Guid.NewGuid():N}";
        await CreateToDoNoteInFolderAsync(owner, title, folderId);

        Assert.Contains(title, await viewer.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task FolderSharedOnCreate_SharesItsNotes_AndSubfolderNotes()
    {
        var (owner, _) = await CreateAccountAsync();
        var (viewer, viewerUsername) = await CreateAccountAsync();
        await SharingTestHelpers.ConnectAsync(owner, viewer, viewerUsername);

        var parentId = await CreateFolderAsync(owner, $"Family {Guid.NewGuid():N}", shareWithUsername: viewerUsername);
        var childId = await CreateFolderAsync(owner, $"Groceries {Guid.NewGuid():N}", parentId: parentId);

        var title = $"Buy eggs {Guid.NewGuid():N}";
        await CreateToDoNoteInFolderAsync(owner, title, childId);

        Assert.Contains(title, await viewer.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task UnsharingTheFolder_RemovesItsNotesFromViewersList()
    {
        var (owner, _) = await CreateAccountAsync();
        var (viewer, viewerUsername) = await CreateAccountAsync();
        await SharingTestHelpers.ConnectAsync(owner, viewer, viewerUsername);

        var folderId = await CreateFolderAsync(owner, $"Family {Guid.NewGuid():N}", shareWithUsername: viewerUsername);
        var title = $"Buy milk {Guid.NewGuid():N}";
        await CreateToDoNoteInFolderAsync(owner, title, folderId);
        Assert.Contains(title, await viewer.GetStringAsync("/Notes/Index"));

        await EditFolderSharingAsync(owner, folderId, withUsername: null);

        Assert.DoesNotContain(title, await viewer.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task UnconnectedFolder_IsNotVisibleToStrangers()
    {
        var (owner, _) = await CreateAccountAsync();
        var (stranger, _) = await CreateAccountAsync();

        var folderId = await CreateFolderAsync(owner, $"Private {Guid.NewGuid():N}");
        var title = $"Secret {Guid.NewGuid():N}";
        await CreateToDoNoteInFolderAsync(owner, title, folderId);

        Assert.DoesNotContain(title, await stranger.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task ScheduleSharedOnCreate_SharesItsNotes()
    {
        var (owner, _) = await CreateAccountAsync();
        var (viewer, viewerUsername) = await CreateAccountAsync();
        await SharingTestHelpers.ConnectAsync(owner, viewer, viewerUsername);

        var createHtml = await owner.GetStringAsync("/Schedules/Index");
        var fields = new Dictionary<string, string>
        {
            ["NewScheduleName"] = $"Family {Guid.NewGuid():N}",
            ["NewScheduleColor"] = "Blue",
            ["NewScheduleShareWithUserIds"] = SharingTestHelpers.FindShareCheckboxUserId(createHtml, viewerUsername),
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(createHtml)
        };
        var response = await owner.PostAsync("/Schedules/Index?handler=Create", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var listHtml = await owner.GetStringAsync("/Schedules/Index");
        Assert.Contains(viewerUsername, listHtml);
        var scheduleId = Regex.Matches(listHtml, "/Schedules/Edit/(\\d+)").Select(m => int.Parse(m.Groups[1].Value)).Max();

        var title = $"Dinner {Guid.NewGuid():N}";
        await CreateToDoNoteAsync(owner, title, new() { ["Input.ScheduleId"] = scheduleId.ToString() });

        Assert.Contains(title, await viewer.GetStringAsync("/Notes/Index"));
    }

    private async Task<(HttpClient Client, string Username)> CreateAccountAsync()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, "Sup3r$ecretPass!");
        await client.GetStringAsync("/Settings/Index");
        return (client, email[..email.IndexOf('@')]);
    }

    private static async Task<int> CreateFolderAsync(HttpClient client, string name, string? shareWithUsername = null, int? parentId = null)
    {
        var pageHtml = await client.GetStringAsync("/Folders/Index");
        var fields = new Dictionary<string, string>
        {
            ["NewFolderName"] = name,
            ["NewFolderColor"] = "Blue",
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(pageHtml)
        };
        if (parentId is not null)
        {
            fields["NewFolderParentId"] = parentId.Value.ToString();
        }
        if (shareWithUsername is not null)
        {
            fields["NewFolderShareWithUserIds"] = SharingTestHelpers.FindShareCheckboxUserId(pageHtml, shareWithUsername);
        }

        var response = await client.PostAsync("/Folders/Index?handler=Create", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var listHtml = await client.GetStringAsync("/Folders/Index");
        return Regex.Matches(listHtml, "/Folders/Edit/(\\d+)").Select(m => int.Parse(m.Groups[1].Value)).Max();
    }

    /// <summary>Pass withUsername: null to save the folder with nobody checked (unshares everyone).</summary>
    private static async Task EditFolderSharingAsync(HttpClient owner, int folderId, string? withUsername)
    {
        var editHtml = await owner.GetStringAsync($"/Folders/Edit/{folderId}");
        var nameMatch = Regex.Match(editHtml, "name=\"Input\\.Name\"[^>]*value=\"([^\"]*)\"");
        Assert.True(nameMatch.Success);

        var fields = new Dictionary<string, string>
        {
            ["Input.Name"] = WebUtility.HtmlDecode(nameMatch.Groups[1].Value),
            ["Input.Color"] = "Blue",
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(editHtml)
        };
        if (withUsername is not null)
        {
            fields["Input.ShareWithUserIds"] = SharingTestHelpers.FindShareCheckboxUserId(editHtml, withUsername);
        }

        var response = await owner.PostAsync($"/Folders/Edit/{folderId}", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task CreateToDoNoteInFolderAsync(HttpClient client, string title, int folderId) =>
        CreateToDoNoteAsync(client, title, new() { ["Input.FolderId"] = folderId.ToString() });

    private static async Task CreateToDoNoteAsync(HttpClient client, string title, Dictionary<string, string> extraFields)
    {
        var createPageHtml = await client.GetStringAsync("/Notes/Create");
        var fields = new Dictionary<string, string>(extraFields)
        {
            ["Input.NoteType"] = "ToDo",
            ["Input.Title"] = title,
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml)
        };
        var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
