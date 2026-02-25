using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Tests;

/// <summary>
/// Custom WebApplicationFactory that swaps the DB to InMemory for test isolation.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntegrationTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core registrations to avoid dual-provider conflict.
            // EF Core 10 throws if both Sqlite and InMemory provider services coexist.
            // AddDbContext registers internal provider services beyond just DbContextOptions,
            // so we must remove everything EF-related and re-add cleanly.
            var efDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(AppDbContext)
                         || (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false))
                .ToList();
            foreach (var d in efDescriptors)
                services.Remove(d);

            // Re-add with InMemory database
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}

/// <summary>
/// Shared WebApplicationFactory so all integration test classes reuse one host.
/// Classes opt-in via [Collection(WebAppCollection.Name)].
/// </summary>
public class WebAppFixture : IAsyncLifetime
{
    public TestWebApplicationFactory Factory { get; } = new();
    public string? StartupError { get; private set; }

    public Task InitializeAsync()
    {
        try
        {
            // Force the host to build and the test server to start
            // CreateClient() triggers both host build AND TestServer start
            using var client = Factory.CreateClient();
        }
        catch (Exception ex)
        {
            StartupError = $"{ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
                StartupError += $"\nInner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
        }
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
