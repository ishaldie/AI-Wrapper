using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Serilog;

namespace ZSR.Underwriting.Tests;

public class ApplicationSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApplicationSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Application_Starts_Without_Errors()
    {
        var client = _factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Homepage_Returns_Success_With_MudBlazor()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        // MudBlazor CSS should be linked in the page
        Assert.Contains("MudBlazor", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serilog_Is_Configured_As_Logging_Provider()
    {
        // Serilog's static Log.Logger should not be the default silent logger
        // after our configuration runs
        Assert.NotEqual(Serilog.Core.Logger.None, Log.Logger);
    }

    [Fact]
    public void DI_Container_Resolves_ILogger()
    {
        using var scope = _factory.Services.CreateScope();
        var logger = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILogger<ApplicationSmokeTests>>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void DI_Container_Resolves_MudBlazor_Services()
    {
        using var scope = _factory.Services.CreateScope();
        var dialogService = scope.ServiceProvider.GetService<MudBlazor.IDialogService>();
        Assert.NotNull(dialogService);
    }
}
