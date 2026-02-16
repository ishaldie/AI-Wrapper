using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ZSR.Underwriting.Tests.Components;

[Collection(WebAppCollection.Name)]
public class ExternalAuthTests
{
    private readonly HttpClient _client;

    public ExternalAuthTests(WebAppFixture fixture)
    {
        _client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("Microsoft")]
    public async Task ExternalLogin_ValidProvider_Returns_Challenge_Redirect(string provider)
    {
        var response = await _client.GetAsync($"/api/auth/external-login?provider={provider}");

        // Challenge triggers a redirect to the OAuth provider
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.NotEmpty(location);
    }

    [Fact]
    public async Task ExternalLogin_InvalidProvider_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/auth/external-login?provider=InvalidProvider");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExternalCallback_Without_Cookie_Redirects_To_Login_With_Error()
    {
        // Calling callback directly without an external auth cookie should fail gracefully
        var response = await _client.GetAsync("/api/auth/external-callback");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.Contains("/login?error=ExternalLoginFailed", location);
    }
}
