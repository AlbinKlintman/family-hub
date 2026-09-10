using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public partial class FamilyTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task SendRequest_AppearsAsIncomingForRecipientAndOutgoingForRequester()
    {
        var (client1, username1) = await CreateAccountWithUsernameAsync();
        var (client2, username2) = await CreateAccountWithUsernameAsync();

        await SendRequestAsync(client1, username2);

        var requesterHtml = await client1.GetStringAsync("/Family/Index");
        Assert.Contains("Requests you've sent", requesterHtml);
        Assert.Contains(username2, requesterHtml);

        var recipientHtml = await client2.GetStringAsync("/Family/Index");
        Assert.Contains("Requests you've received", recipientHtml);
        Assert.Contains(username1, recipientHtml);
    }

    [Fact]
    public async Task Accept_MovesConnectionIntoBothAccountsConnectionList()
    {
        var (client1, username1) = await CreateAccountWithUsernameAsync();
        var (client2, username2) = await CreateAccountWithUsernameAsync();

        await SendRequestAsync(client1, username2);

        var recipientHtml = await client2.GetStringAsync("/Family/Index");
        var id = ExtractFirstHandlerRouteId(recipientHtml, "Accept");
        var token = HtmlHelpers.ExtractAntiforgeryToken(recipientHtml);

        var acceptResponse = await client2.PostAsync($"/Family/Index?handler=Accept&id={id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var requesterHtml = await client1.GetStringAsync("/Family/Index");
        Assert.Contains("Your connections", requesterHtml);
        Assert.Contains(username2, requesterHtml);
        Assert.DoesNotContain("Requests you've sent", requesterHtml);

        var updatedRecipientHtml = await client2.GetStringAsync("/Family/Index");
        Assert.Contains(username1, updatedRecipientHtml);
        Assert.DoesNotContain("Requests you've received", updatedRecipientHtml);
    }

    [Fact]
    public async Task Decline_RemovesThePendingRequestEntirely()
    {
        var (client1, _) = await CreateAccountWithUsernameAsync();
        var (client2, username2) = await CreateAccountWithUsernameAsync();

        await SendRequestAsync(client1, username2);

        var recipientHtml = await client2.GetStringAsync("/Family/Index");
        var id = ExtractFirstHandlerRouteId(recipientHtml, "Decline");
        var token = HtmlHelpers.ExtractAntiforgeryToken(recipientHtml);

        await client2.PostAsync($"/Family/Index?handler=Decline&id={id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        var updatedRecipientHtml = await client2.GetStringAsync("/Family/Index");
        Assert.DoesNotContain("Requests you've received", updatedRecipientHtml);

        var requesterHtml = await client1.GetStringAsync("/Family/Index");
        Assert.DoesNotContain("Requests you've sent", requesterHtml);
    }

    [Fact]
    public async Task Labels_AreIndependentPerDirection()
    {
        var (client1, _) = await CreateAccountWithUsernameAsync();
        var (client2, username2) = await CreateAccountWithUsernameAsync();

        await SendRequestAsync(client1, username2);

        var recipientHtml = await client2.GetStringAsync("/Family/Index");
        var acceptId = ExtractFirstHandlerRouteId(recipientHtml, "Accept");
        var acceptToken = HtmlHelpers.ExtractAntiforgeryToken(recipientHtml);
        await client2.PostAsync($"/Family/Index?handler=Accept&id={acceptId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = acceptToken
        }));

        var requesterHtml = await client1.GetStringAsync("/Family/Index");
        var connectionId = ExtractFirstHandlerRouteId(requesterHtml, "SetLabel");
        var labelToken = HtmlHelpers.ExtractAntiforgeryToken(requesterHtml);
        await client1.PostAsync($"/Family/Index?handler=SetLabel&id={connectionId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["LabelInput"] = "Wife",
            ["__RequestVerificationToken"] = labelToken
        }));

        var requesterAfterLabel = await client1.GetStringAsync("/Family/Index");
        Assert.Contains("value=\"Wife\"", requesterAfterLabel);

        // The placeholder text itself always says "e.g. Wife" regardless of any actual value,
        // so check the label wasn't actually saved as a value rather than searching for the word.
        var recipientAfterLabel = await client2.GetStringAsync("/Family/Index");
        Assert.DoesNotContain("value=\"Wife\"", recipientAfterLabel);
    }

    [Fact]
    public async Task SendRequest_ToSelf_IsRejected()
    {
        var (client, username) = await CreateAccountWithUsernameAsync();

        var response = await SendRequestAsync(client, username);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("send a connection request to yourself", body);
    }

    [Fact]
    public async Task SendRequest_ToUnknownUsername_ShowsError()
    {
        var (client, _) = await CreateAccountWithUsernameAsync();

        var response = await SendRequestAsync(client, $"nosuchuser{Guid.NewGuid():N}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("No user found", body);
    }

    [Fact]
    public async Task SendRequest_Duplicate_IsRejected()
    {
        var (client1, _) = await CreateAccountWithUsernameAsync();
        var (_, username2) = await CreateAccountWithUsernameAsync();

        await SendRequestAsync(client1, username2);
        var response = await SendRequestAsync(client1, username2);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("already connected", body);
    }

    private async Task<(HttpClient Client, string Username)> CreateAccountWithUsernameAsync()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        // Visiting Settings once lazily creates the profile, defaulting the username to the email's local part.
        await client.GetStringAsync("/Settings/Index");
        return (client, email[..email.IndexOf('@')]);
    }

    private static async Task<HttpResponseMessage> SendRequestAsync(HttpClient client, string targetUsername)
    {
        var pageHtml = await client.GetStringAsync("/Family/Index");
        var token = HtmlHelpers.ExtractAntiforgeryToken(pageHtml);

        return await client.PostAsync("/Family/Index?handler=SendRequest", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NewRequestUsername"] = targetUsername,
            ["__RequestVerificationToken"] = token
        }));
    }

    private static int ExtractFirstHandlerRouteId(string html, string handler)
    {
        var actionMatch = Regex.Match(html, $"action=\"([^\"]*handler={handler}[^\"]*)\"");
        Assert.True(actionMatch.Success, $"Could not find an action for handler '{handler}' in:\n{html}");

        var idMatch = Regex.Match(actionMatch.Groups[1].Value, "id=(\\d+)");
        Assert.True(idMatch.Success, $"Action '{actionMatch.Groups[1].Value}' had no id.");
        return int.Parse(idMatch.Groups[1].Value);
    }
}
