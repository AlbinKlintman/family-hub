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
}
