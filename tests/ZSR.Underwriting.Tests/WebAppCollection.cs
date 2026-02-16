using Microsoft.AspNetCore.Mvc.Testing;

namespace ZSR.Underwriting.Tests;

/// <summary>
/// Shared WebApplicationFactory so all integration test classes reuse one host.
/// Classes opt-in via [Collection(WebAppCollection.Name)].
/// </summary>
public class WebAppFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; } = new();

    public Task InitializeAsync()
    {
        // Force the host to start once, up-front
        _ = Factory.Server;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class WebAppCollection : ICollectionFixture<WebAppFixture>
{
    public const string Name = "WebApp";
}
