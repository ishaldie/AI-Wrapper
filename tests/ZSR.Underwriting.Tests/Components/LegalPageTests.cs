using System.Net;

namespace ZSR.Underwriting.Tests.Components;

[Collection(WebAppCollection.Name)]
public class LegalPageTests
{
    private readonly HttpClient _client;

    public LegalPageTests(WebAppFixture fixture)
    {
        _client = fixture.Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task Terms_Page_Is_Accessible_Without_Auth()
    {
        var response = await _client.GetAsync("/terms");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Terms_Page_Contains_TOS_Title()
    {
        var response = await _client.GetAsync("/terms");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Terms of Service", content);
    }

    [Fact]
    public async Task Terms_Page_Contains_AI_Disclaimer()
    {
        var response = await _client.GetAsync("/terms");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("AI Technology", content);
    }

    [Fact]
    public async Task Terms_Page_Contains_Not_Investment_Advice()
    {
        var response = await _client.GetAsync("/terms");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Not Investment Advice", content);
    }

    [Fact]
    public async Task Privacy_Page_Is_Accessible_Without_Auth()
    {
        var response = await _client.GetAsync("/privacy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Privacy_Page_Contains_Privacy_Title()
    {
        var response = await _client.GetAsync("/privacy");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Privacy Policy", content);
    }

    [Fact]
    public async Task Privacy_Page_Contains_Third_Party_Services()
    {
        var response = await _client.GetAsync("/privacy");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Third-Party Services", content);
    }

    [Fact]
    public async Task Privacy_Page_Contains_Data_Collection_Section()
    {
        var response = await _client.GetAsync("/privacy");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Information We Collect", content);
    }
}
