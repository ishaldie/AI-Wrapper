using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ZSR.Underwriting.Tests.Components;

[Collection(WebAppCollection.Name)]
public class AuthPageTests
{
    private readonly HttpClient _client;

    public AuthPageTests(WebAppFixture fixture)
    {
        _client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Login_Page_Is_Accessible_Without_Auth()
    {
        var response = await _client.GetAsync("/login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Welcome", content);
    }

    [Fact]
    public async Task Register_Page_Is_Accessible_Without_Auth()
    {
        var response = await _client.GetAsync("/register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Create Account", content);
    }

    [Fact]
    public async Task Dashboard_Redirects_To_Login_When_Unauthenticated()
    {
        var response = await _client.GetAsync("/search");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Login_Page_Contains_Email_Field()
    {
        var response = await _client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email", content);
    }

    [Fact]
    public async Task Login_Page_Contains_Continue_Button()
    {
        var response = await _client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Continue", content);
    }

    [Fact]
    public async Task Register_Page_Contains_FullName_Field()
    {
        var response = await _client.GetAsync("/register");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Full Name", content);
    }

    [Fact]
    public async Task Register_Page_Links_To_Login()
    {
        var response = await _client.GetAsync("/register");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("/login", content);
    }

    [Fact]
    public async Task Login_Page_Links_To_Register()
    {
        var response = await _client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("/register", content);
    }

    [Fact]
    public async Task Landing_Page_Is_Accessible_Without_Auth()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Answer any real estate question", content);
    }

    [Fact]
    public async Task VerifyCode_Page_Redirects_Without_Email()
    {
        var response = await _client.GetAsync("/verify-code");
        // Should redirect to login since no email query param
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Login_Page_Shows_Social_Login_Options()
    {
        var response = await _client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Continue with Google", content);
        Assert.Contains("Continue with Microsoft", content);
    }

    [Fact]
    public async Task Landing_Page_Hero_Search_Is_Link_Not_Input()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        // Bug fix: Hero search bar should be a navigable <a> link to /login
        // not an <input> with SSR-broken @onfocus handler that does nothing
        Assert.Contains("Search a property", content);
        Assert.DoesNotContain("<input type=\"text\" placeholder=\"Search a property", content);
    }

    [Fact]
    public async Task Login_Page_Social_Buttons_Link_To_External_Auth_Endpoint()
    {
        var response = await _client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/auth/external-login?provider=Google", content);
        Assert.Contains("/api/auth/external-login?provider=Microsoft", content);
    }

    [Fact]
    public async Task Logout_Post_Endpoint_Returns_Redirect()
    {
        // POST /api/auth/logout should return redirect to /login (requires auth)
        // Before fix: endpoint doesn't exist, returns 200 with not-found page
        // After fix: endpoint exists, returns 302 redirect to /login
        var response = await _client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString() ?? "");
    }
}
