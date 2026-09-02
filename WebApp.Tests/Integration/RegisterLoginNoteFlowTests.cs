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

        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

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
