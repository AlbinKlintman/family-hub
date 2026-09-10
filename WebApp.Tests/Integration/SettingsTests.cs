using System.Net;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class SettingsTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task GetSettings_FirstVisit_DefaultsUsernameFromEmail()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var settingsHtml = await client.GetStringAsync("/Settings/Index");

        var expectedDefault = email[..email.IndexOf('@')];
        Assert.Contains(expectedDefault, settingsHtml);
    }

    [Fact]
    public async Task Save_UpdatesUsernameAvatarAndAccentColor()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var newUsername = $"user{Guid.NewGuid():N}"[..20];

        var settingsPageHtml = await client.GetStringAsync("/Settings/Index");
        var token = HtmlHelpers.ExtractAntiforgeryToken(settingsPageHtml);

        var response = await client.PostAsync("/Settings/Index", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = newUsername,
            ["Input.AvatarUrl"] = "https://example.com/avatar.png",
            ["Input.AccentColor"] = "Purple",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedHtml = await client.GetStringAsync("/Settings/Index");
        Assert.Contains(newUsername, updatedHtml);
        Assert.Contains("https://example.com/avatar.png", updatedHtml);
    }

    [Fact]
    public async Task Save_DuplicateUsername_ShowsValidationError()
    {
        using var client1 = factory.CreateClient();
        var email1 = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client1, factory, email1, password);

        var takenUsername = $"taken{Guid.NewGuid():N}"[..20];
        var page1Html = await client1.GetStringAsync("/Settings/Index");
        var token1 = HtmlHelpers.ExtractAntiforgeryToken(page1Html);
        await client1.PostAsync("/Settings/Index", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = takenUsername,
            ["__RequestVerificationToken"] = token1
        }));

        using var client2 = factory.CreateClient();
        var email2 = $"{Guid.NewGuid():N}@example.com";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client2, factory, email2, password);

        var page2Html = await client2.GetStringAsync("/Settings/Index");
        var token2 = HtmlHelpers.ExtractAntiforgeryToken(page2Html);
        var response = await client2.PostAsync("/Settings/Index", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = takenUsername,
            ["__RequestVerificationToken"] = token2
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("already taken", body);
    }

    [Fact]
    public async Task Save_InvalidAvatarUrl_ShowsValidationError()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var settingsPageHtml = await client.GetStringAsync("/Settings/Index");
        var token = HtmlHelpers.ExtractAntiforgeryToken(settingsPageHtml);

        var response = await client.PostAsync("/Settings/Index", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = $"user{Guid.NewGuid():N}"[..20],
            ["Input.AvatarUrl"] = "not-a-url",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Enter a valid URL", body);
    }

    [Fact]
    public async Task Dashboard_TodaysFastCard_HiddenByDefault_ShownAfterEnabling()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var homeHtml = await client.GetStringAsync("/");
        Assert.DoesNotContain("Today's fast", homeHtml);

        var settingsPageHtml = await client.GetStringAsync("/Settings/Index");
        var token = HtmlHelpers.ExtractAntiforgeryToken(settingsPageHtml);
        await client.PostAsync("/Settings/Index", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = $"user{Guid.NewGuid():N}"[..20],
            ["Input.ShowTodaysFastCard"] = "true",
            ["__RequestVerificationToken"] = token
        }));

        var updatedHomeHtml = await client.GetStringAsync("/");
        Assert.Contains("Today's fast", updatedHomeHtml);
    }
}
