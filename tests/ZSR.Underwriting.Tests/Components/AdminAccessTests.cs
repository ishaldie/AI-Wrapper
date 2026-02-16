using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ZSR.Underwriting.Tests.Components;

[Collection(WebAppCollection.Name)]
public class AdminAccessTests
{
    private readonly HttpClient _client;

    public AdminAccessTests(WebAppFixture fixture)
    {
        _client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Admin_UserManagement_Redirects_When_Unauthenticated()
    {
        var response = await _client.GetAsync("/admin/users");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString() ?? "");
    }
}
