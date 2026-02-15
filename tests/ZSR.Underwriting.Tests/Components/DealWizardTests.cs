using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class DealWizardTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public DealWizardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void DealWizard_RendersRequiredInputsText()
    {
        var cut = _ctx.Render<DealWizard>();

        var markup = cut.Markup;
        Assert.Contains("Required Inputs", markup);
    }

    [Fact]
    public void DealWizard_ShowsThreeSteps()
    {
        var cut = _ctx.Render<DealWizard>();

        var steps = cut.FindComponents<MudStep>();
        Assert.Equal(3, steps.Count);
    }

    [Fact]
    public void DealWizard_HasPropertyNameField()
    {
        var cut = _ctx.Render<DealWizard>();

        var markup = cut.Markup;
        Assert.Contains("Property Name", markup);
    }
}
