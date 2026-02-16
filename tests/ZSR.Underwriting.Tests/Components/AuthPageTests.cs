using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ZSR.Underwriting.Tests.Components;

public class AuthPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthPageTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
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
        Assert.Contains("Sign In", content);
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
        var response = await _client.GetAsync("/");
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
    public async Task Login_Page_Contains_Password_Field()
    {
        var response = await _client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Password", content);
    }

    [Fact]
    public async Task Login_Page_Contains_Remember_Me()
    {
        var response = await _client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Remember me", content);
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
}
