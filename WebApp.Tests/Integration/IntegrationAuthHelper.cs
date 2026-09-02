namespace WebApp.Tests.Integration;

public static class IntegrationAuthHelper
{
    public static async Task RegisterAndLoginAsync(HttpClient client, FamilyHubFactory factory, string email, string password)
    {
        var registerPageHtml = await client.GetStringAsync("/Identity/Account/Register");
        var registerToken = HtmlHelpers.ExtractAntiforgeryToken(registerPageHtml);

        await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password,
            ["__RequestVerificationToken"] = registerToken
        }));

        var sentEmail = factory.EmailSender.SentEmails.Single(e => e.To == email);
        var confirmationLink = HtmlHelpers.ExtractFirstLink(sentEmail.HtmlMessage);
        await client.GetAsync(confirmationLink);

        var loginPageHtml = await client.GetStringAsync("/Identity/Account/Login");
        var loginToken = HtmlHelpers.ExtractAntiforgeryToken(loginPageHtml);

        await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = loginToken
        }));
    }
}
