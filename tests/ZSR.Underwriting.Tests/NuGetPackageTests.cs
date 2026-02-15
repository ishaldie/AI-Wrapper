using MudBlazor;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Serilog;
using FluentValidation;
using Polly;

namespace ZSR.Underwriting.Tests;

/// <summary>
/// Verifies that required NuGet packages are installed and key types are resolvable.
/// </summary>
public class NuGetPackageTests
{
    [Fact]
    public void MudBlazor_Package_Is_Installed()
    {
        var type = typeof(MudButton);
        Assert.NotNull(type);
        Assert.Equal("MudBlazor", type.Namespace);
    }

    [Fact]
    public void EfCore_Package_Is_Installed()
    {
        var type = typeof(DbContext);
        Assert.NotNull(type);
        Assert.Equal("Microsoft.EntityFrameworkCore", type.Namespace);
    }

    [Fact]
    public void Serilog_Package_Is_Installed()
    {
        var type = typeof(Log);
        Assert.NotNull(type);
        Assert.Equal("Serilog", type.Namespace);
    }

    [Fact]
    public void FluentValidation_Package_Is_Installed()
    {
        var type = typeof(AbstractValidator<>);
        Assert.NotNull(type);
        Assert.Equal("FluentValidation", type.Namespace);
    }

    [Fact]
    public void Polly_Package_Is_Installed()
    {
        var type = typeof(ResiliencePipeline);
        Assert.NotNull(type);
        Assert.Equal("Polly", type.Namespace);
    }

    [Fact]
    public void AspNetIdentity_Package_Is_Installed()
    {
        var type = typeof(IdentityUser);
        Assert.NotNull(type);
        Assert.Equal("Microsoft.AspNetCore.Identity", type.Namespace);
    }

    [Fact]
    public void AspNetIdentityEfCore_Package_Is_Installed()
    {
        var type = typeof(IdentityDbContext<>);
        Assert.NotNull(type);
        Assert.Equal("Microsoft.AspNetCore.Identity.EntityFrameworkCore", type.Namespace);
    }
}
