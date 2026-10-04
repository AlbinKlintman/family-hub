using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class HiddenPagesTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task HidingJobApplications_RemovesItEverywhere_AndShowingItKeepsTheData()
    {
        var client = await CreateAccountAsync();
        var roleName = $"Backend Dev {Guid.NewGuid():N}";
        var createHtml = await client.GetStringAsync("/Applications/Create");
        await client.PostAsync("/Applications/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.RoleName"] = roleName,
            ["__RequestVerificationToken"] = HtmlHelpers.ExtractAntiforgeryToken(createHtml)
        }));
        Assert.Contains(roleName, await client.GetStringAsync("/Board/Index"));

        await SaveHiddenSectionsAsync(client, "JobApplications");

        var homeHtml = await client.GetStringAsync("/");
        Assert.DoesNotContain("href=\"/Board\"", homeHtml);
        Assert.DoesNotContain("id=\"jobSearchChart\"", await client.GetStringAsync("/Statistics/Index"));
        Assert.Contains("id=\"weightChart\"", await client.GetStringAsync("/Statistics/Index"));

        var boardResponse = await client.GetAsync("/Board/Index");
        Assert.Equal("/", boardResponse.RequestMessage!.RequestUri!.AbsolutePath);
        var editResponse = await client.GetAsync("/Applications/Create");
        Assert.Equal("/", editResponse.RequestMessage!.RequestUri!.AbsolutePath);

        await SaveHiddenSectionsAsync(client);

        Assert.Contains(roleName, await client.GetStringAsync("/Board/Index"));
        Assert.Contains("href=\"/Board\"", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task HidingTraining_HidesItsStatisticsCharts()
    {
        var client = await CreateAccountAsync();

        await SaveHiddenSectionsAsync(client, "Training");

        var statsHtml = await client.GetStringAsync("/Statistics/Index");
        Assert.DoesNotContain("id=\"weightChart\"", statsHtml);
        Assert.DoesNotContain("id=\"workoutChart\"", statsHtml);
        Assert.Contains("id=\"jobSearchChart\"", statsHtml);
    }

    [Fact]
    public async Task HomeAndSettings_StayReachable_WithEveryPageHidden()
    {
        var client = await CreateAccountAsync();

        await SaveHiddenSectionsAsync(client, "Calendar", "Notes", "JobApplications", "Media", "Training", "Statistics", "Family");

        var homeResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
        var settingsResponse = await client.GetAsync("/Settings/Index");
        Assert.Equal("/Settings/Index", settingsResponse.RequestMessage!.RequestUri!.AbsolutePath);
        Assert.Equal("/", (await client.GetAsync("/Family/Index")).RequestMessage!.RequestUri!.AbsolutePath);
    }

    private async Task<HttpClient> CreateAccountAsync()
    {
        var client = factory.CreateClient();
        await IntegrationAuthHelper.RegisterAndLoginAsync(client, factory, $"{Guid.NewGuid():N}@example.com", "Sup3r$ecretPass!");
        await client.GetStringAsync("/Settings/Index");
        return client;
    }

    private static async Task SaveHiddenSectionsAsync(HttpClient client, params string[] sections)
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
        fields.AddRange(sections.Select(s => new KeyValuePair<string, string>("Input.HiddenSections", s)));

        var response = await client.PostAsync("/Settings/Index", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
