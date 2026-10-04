using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class HidingTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task HidingAFolder_HidesItsNotes_AndShowingBringsThemBack()
    {
        var (owner, _) = await CreateAccountAsync();
        var folderId = await SharingTestHelpers.CreateFolderAsync(owner, $"Work {Guid.NewGuid():N}");
        var title = $"Report {Guid.NewGuid():N}";
        await SharingTestHelpers.CreateToDoNoteInFolderAsync(owner, title, folderId);

        await ToggleHiddenAsync(owner, "/Folders/Index", folderId);
        Assert.DoesNotContain(title, await owner.GetStringAsync("/Notes/Index"));
        Assert.DoesNotContain(title, await owner.GetStringAsync($"/Notes/Index?folderId={folderId}"));

        await ToggleHiddenAsync(owner, "/Folders/Index", folderId);
        Assert.Contains(title, await owner.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task HidingAParentFolder_HidesSubfolderNotes()
    {
        var (owner, _) = await CreateAccountAsync();
        var parentId = await SharingTestHelpers.CreateFolderAsync(owner, $"Home {Guid.NewGuid():N}");
        var childId = await SharingTestHelpers.CreateFolderAsync(owner, $"Kitchen {Guid.NewGuid():N}", parentId: parentId);
        var title = $"Fix tap {Guid.NewGuid():N}";
        await SharingTestHelpers.CreateToDoNoteInFolderAsync(owner, title, childId);

        await ToggleHiddenAsync(owner, "/Folders/Index", parentId);

        Assert.DoesNotContain(title, await owner.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task OwnerHidingASharedFolder_HidesItsNotesForTheViewerToo()
    {
        var (owner, _) = await CreateAccountAsync();
        var (viewer, viewerUsername) = await CreateAccountAsync();
        await SharingTestHelpers.ConnectAsync(owner, viewer, viewerUsername);

        var folderId = await SharingTestHelpers.CreateFolderAsync(owner, $"Family {Guid.NewGuid():N}", shareWithUsername: viewerUsername);
        var title = $"Buy milk {Guid.NewGuid():N}";
        await SharingTestHelpers.CreateToDoNoteInFolderAsync(owner, title, folderId);
        Assert.Contains(title, await viewer.GetStringAsync("/Notes/Index"));

        await ToggleHiddenAsync(owner, "/Folders/Index", folderId);

        Assert.DoesNotContain(title, await viewer.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task HidingASchedule_HidesItsNotes_AndItDisappearsFromTheFilter()
    {
        var (owner, _) = await CreateAccountAsync();
        var scheduleName = $"Gym {Guid.NewGuid():N}";
        var scheduleId = await CreateScheduleAsync(owner, scheduleName);
        var title = $"Leg day {Guid.NewGuid():N}";
        await SharingTestHelpers.CreateToDoNoteAsync(owner, title, new() { ["Input.ScheduleId"] = scheduleId.ToString() });
        Assert.Contains(title, await owner.GetStringAsync("/Notes/Index"));

        await ToggleHiddenAsync(owner, "/Schedules/Index", scheduleId);

        var notesHtml = await owner.GetStringAsync("/Notes/Index");
        Assert.DoesNotContain(title, notesHtml);
        Assert.DoesNotContain(scheduleName, notesHtml);
        Assert.Contains(scheduleName, await owner.GetStringAsync("/Schedules/Index"));
    }

    [Fact]
    public async Task HidingANoteType_HidesItsNotes_AndRemovesItAsAnOption()
    {
        var (owner, _) = await CreateAccountAsync();
        var title = $"Call mum {Guid.NewGuid():N}";
        await SharingTestHelpers.CreateToDoNoteAsync(owner, title, []);
        Assert.Contains(title, await owner.GetStringAsync("/Notes/Index"));

        var response = await SaveHiddenNoteTypesAsync(owner, "ToDo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var notesHtml = await owner.GetStringAsync("/Notes/Index");
        Assert.DoesNotContain(title, notesHtml);
        Assert.DoesNotContain("<option value=\"ToDo\"", notesHtml);
        Assert.DoesNotContain(title, await owner.GetStringAsync("/Notes/Index?noteType=ToDo"));

        var createHtml = await owner.GetStringAsync("/Notes/Create");
        Assert.DoesNotContain("id=\"type-todo\"", createHtml);
        Assert.Contains("id=\"type-laundry\"", createHtml);

        await SaveHiddenNoteTypesAsync(owner);
        Assert.Contains(title, await owner.GetStringAsync("/Notes/Index"));
    }

    [Fact]
    public async Task HidingEveryNoteType_IsRejected()
    {
        var (owner, _) = await CreateAccountAsync();

        var response = await SaveHiddenNoteTypesAsync(owner, "ToDo", "Laundry", "WorkShift", "Fasting");

        Assert.Contains("Keep at least one note type visible.", await response.Content.ReadAsStringAsync());
    }

    private async Task<(HttpClient Client, string Username)> CreateAccountAsync()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, "Sup3r$ecretPass!");
        await client.GetStringAsync("/Settings/Index");
        return (client, email[..email.IndexOf('@')]);
    }

    private static async Task ToggleHiddenAsync(HttpClient client, string listPage, int id)
    {
        var html = await client.GetStringAsync(listPage);
        var response = await client.PostAsync($"{listPage}?handler=ToggleHidden&id={id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(html)
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<int> CreateScheduleAsync(HttpClient client, string name)
    {
        var html = await client.GetStringAsync("/Schedules/Index");
        var response = await client.PostAsync("/Schedules/Index?handler=Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NewScheduleName"] = name,
            ["NewScheduleColor"] = "Blue",
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(html)
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var listHtml = await client.GetStringAsync("/Schedules/Index");
        return Regex.Matches(listHtml, "/Schedules/Edit/(\\d+)").Select(m => int.Parse(m.Groups[1].Value)).Max();
    }

    private static async Task<HttpResponseMessage> SaveHiddenNoteTypesAsync(HttpClient client, params string[] types)
    {
        var html = await client.GetStringAsync("/Settings/Index");
        var usernameMatch = Regex.Match(html, "name=\"Input\\.Username\"[^>]*value=\"([^\"]*)\"");
        Assert.True(usernameMatch.Success);

        var fields = new List<KeyValuePair<string, string>>
        {
            new("Input.Username", WebUtility.HtmlDecode(usernameMatch.Groups[1].Value)),
            new("Input.AccentColor", "Blue"),
            new("__RequestVerificationToken", HtmlHelpers.ExtractAntiforgeryToken(html))
        };
        fields.AddRange(types.Select(t => new KeyValuePair<string, string>("Input.HiddenNoteTypes", t)));

        return await client.PostAsync("/Settings/Index", new FormUrlEncodedContent(fields));
    }
}
