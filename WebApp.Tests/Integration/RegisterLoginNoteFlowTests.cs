using System.Net;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class RegisterLoginNoteFlowTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task Register_confirm_login_create_note_and_see_it_listed()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";

        var registerPageHtml = await client.GetStringAsync("/Identity/Account/Register");
        var registerToken = HtmlHelpers.ExtractAntiforgeryToken(registerPageHtml);

        var registerResponse = await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password,
            ["__RequestVerificationToken"] = registerToken
        }));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var sentEmail = factory.EmailSender.SentEmails.Single(e => e.To == email);
        var confirmationLink = HtmlHelpers.ExtractFirstLink(sentEmail.HtmlMessage);

        var confirmResponse = await client.GetAsync(confirmationLink);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var loginPageHtml = await client.GetStringAsync("/Identity/Account/Login");
        var loginToken = HtmlHelpers.ExtractAntiforgeryToken(loginPageHtml);

        var loginResponse = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = loginToken
        }));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var createNotePageHtml = await client.GetStringAsync("/Notes/Create");
        var createNoteToken = HtmlHelpers.ExtractAntiforgeryToken(createNotePageHtml);
        var noteTitle = $"Integration test note {Guid.NewGuid():N}";

        var createNoteResponse = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NoteType"] = "ToDo",
            ["Input.Title"] = noteTitle,
            ["__RequestVerificationToken"] = createNoteToken
        }));
        Assert.Equal(HttpStatusCode.OK, createNoteResponse.StatusCode);

        var notesIndexHtml = await client.GetStringAsync("/Notes");
        Assert.Contains(noteTitle, notesIndexHtml);
    }
}
