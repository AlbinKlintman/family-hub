using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

public static class SharingTestHelpers
{
    /// <summary>Sends a connection request from requester and accepts it as recipient.</summary>
    public static async Task ConnectAsync(HttpClient requester, HttpClient recipient, string recipientUsername)
    {
        var requestPageHtml = await requester.GetStringAsync("/Family/Index");
        await requester.PostAsync("/Family/Index?handler=SendRequest", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NewRequestUsername"] = recipientUsername,
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(requestPageHtml)
        }));

        var recipientHtml = await recipient.GetStringAsync("/Family/Index");
        var actionMatch = Regex.Match(recipientHtml, "action=\"([^\"]*handler=Accept[^\"]*)\"");
        Assert.True(actionMatch.Success);
        var idMatch = Regex.Match(actionMatch.Groups[1].Value, "id=(\\d+)");
        Assert.True(idMatch.Success);

        await recipient.PostAsync($"/Family/Index?handler=Accept&id={idMatch.Groups[1].Value}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(recipientHtml)
        }));
    }

    /// <summary>The UserId value of the "Share with" checkbox labelled with this username (_ShareCheckboxList markup).</summary>
    public static string FindShareCheckboxUserId(string html, string username)
    {
        var match = Regex.Match(html, $"value=\"([^\"]+)\" id=\"share-[^\"]+\"[^>]*>\\s*<label class=\"form-check-label\" for=\"share-\\1\">{Regex.Escape(username)}</label>");
        Assert.True(match.Success, $"Could not find a share checkbox for '{username}' in:\n{html}");
        return match.Groups[1].Value;
    }

    public static async Task<int> CreateFolderAsync(HttpClient client, string name, string? shareWithUsername = null, int? parentId = null)
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
            fields["NewFolderShareWithUserIds"] = FindShareCheckboxUserId(pageHtml, shareWithUsername);
        }

        var response = await client.PostAsync("/Folders/Index?handler=Create", new FormUrlEncodedContent(fields));
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var listHtml = await client.GetStringAsync("/Folders/Index");
        return Regex.Matches(listHtml, "/Folders/Edit/(\\d+)").Select(m => int.Parse(m.Groups[1].Value)).Max();
    }

    public static Task CreateToDoNoteInFolderAsync(HttpClient client, string title, int folderId) =>
        CreateToDoNoteAsync(client, title, new() { ["Input.FolderId"] = folderId.ToString() });

    public static async Task CreateToDoNoteAsync(HttpClient client, string title, Dictionary<string, string> extraFields)
    {
        var createPageHtml = await client.GetStringAsync("/Notes/Create");
        var fields = new Dictionary<string, string>(extraFields)
        {
            ["Input.NoteType"] = "ToDo",
            ["Input.Title"] = title,
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml)
        };
        var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(fields));
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
