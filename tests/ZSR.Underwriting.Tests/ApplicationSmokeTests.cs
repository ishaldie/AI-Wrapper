using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Serilog;

namespace ZSR.Underwriting.Tests;

[Collection(WebAppCollection.Name)]
public class ApplicationSmokeTests
{
    private readonly WebAppFixture _fixture;

    public ApplicationSmokeTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Application_Starts_Without_Errors()
    {
        var client = _fixture.Factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Homepage_Returns_Success_With_MudBlazor()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("MudBlazor", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serilog_Is_Configured_As_Logging_Provider()
    {
        // Ensure the host is started (factory is already initialized by the collection fixture)
        _ = _fixture.Factory.Server;
        Assert.NotEqual(Serilog.Core.Logger.None, Log.Logger);
    }

    [Fact]
    public void DI_Container_Resolves_ILogger()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var logger = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILogger<ApplicationSmokeTests>>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void DI_Container_Resolves_MudBlazor_Services()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dialogService = scope.ServiceProvider.GetService<MudBlazor.IDialogService>();
        Assert.NotNull(dialogService);
    }
}
