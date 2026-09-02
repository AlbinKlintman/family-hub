using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class HealthAndAccessTests(FamilyHubFactory factory)
{
    [Fact]
    public async Task Health_endpoint_returns_healthy()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Anonymous_home_page_is_reachable()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_request_to_a_protected_page_redirects_to_login()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Board");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location!.ToString());
    }
}
