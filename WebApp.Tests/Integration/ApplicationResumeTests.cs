using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public partial class ApplicationResumeTests(FamilyHubFactory factory)
{
    private static readonly byte[] FakePdfBytes = "%PDF-1.4\n%fake pdf content for testing\n%%EOF"u8.ToArray();

    [Fact]
    public async Task Upload_resume_on_create_can_be_downloaded_back_unchanged()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var roleName = $"Backend Engineer {Guid.NewGuid():N}";

        var createPageHtml = await client.GetStringAsync("/Applications/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(roleName), "Input.RoleName" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(FakePdfBytes), "Input.ResumeFile", "resume.pdf" }
        };

        var createResponse = await client.PostAsync("/Applications/Create", form);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var boardHtml = await client.GetStringAsync("/Board/Index");
        Assert.Contains(roleName, boardHtml);
        Assert.Contains("Resume", boardHtml);

        var id = ExtractCardId(boardHtml, roleName);

        var editPageHtml = await client.GetStringAsync($"/Applications/Edit/{id}");
        Assert.Contains("resume.pdf", editPageHtml);

        var downloadResponse = await client.GetAsync($"/Applications/Edit/{id}?handler=Resume");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(FakePdfBytes, await downloadResponse.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Uploading_a_non_pdf_is_rejected_with_a_validation_error()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, email, password);

        var roleName = $"Rejected Upload {Guid.NewGuid():N}";

        var createPageHtml = await client.GetStringAsync("/Applications/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(roleName), "Input.RoleName" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent("just some plain text, not a pdf"u8.ToArray()), "Input.ResumeFile", "resume.pdf" }
        };

        var createResponse = await client.PostAsync("/Applications/Create", form);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var body = await createResponse.Content.ReadAsStringAsync();
        Assert.Contains("Enter a PDF file up to 10 MB", body);

        var boardHtml = await client.GetStringAsync("/Board/Index");
        Assert.DoesNotContain(roleName, boardHtml);
    }

    [Fact]
    public async Task Resume_download_is_not_accessible_to_another_user()
    {
        using var ownerClient = factory.CreateClient();
        var ownerEmail = $"{Guid.NewGuid():N}@example.com";
        const string password = "Sup3r$ecretPass!";
        await IntegrationAuthHelper.RegisterAndLoginAsync(ownerClient, factory, ownerEmail, password);

        var roleName = $"Private Resume {Guid.NewGuid():N}";
        var createPageHtml = await ownerClient.GetStringAsync("/Applications/Create");
        var token = HtmlHelpers.ExtractAntiforgeryToken(createPageHtml);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(roleName), "Input.RoleName" },
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(FakePdfBytes), "Input.ResumeFile", "resume.pdf" }
        };
        await ownerClient.PostAsync("/Applications/Create", form);

        var boardHtml = await ownerClient.GetStringAsync("/Board/Index");
        var id = ExtractCardId(boardHtml, roleName);

        using var strangerClient = factory.CreateClient();
        var strangerEmail = $"{Guid.NewGuid():N}@example.com";
        await IntegrationAuthHelper.RegisterAndLoginAsync(strangerClient, factory, strangerEmail, password);

        var response = await strangerClient.GetAsync($"/Applications/Edit/{id}?handler=Resume");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string ExtractCardId(string boardHtml, string roleName)
    {
        var match = CardPattern().Matches(boardHtml).FirstOrDefault(m => m.Groups[2].Value == roleName);
        Assert.NotNull(match);
        return match!.Groups[1].Value;
    }

    [GeneratedRegex("data-id=\"(\\d+)\">\\s*<div[^>]*class=\"card-body\"[^>]*>\\s*<h6[^>]*class=\"card-title mb-1\"[^>]*>([^<]*)</h6>")]
    private static partial Regex CardPattern();
}
