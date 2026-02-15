using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class IdentityServiceRegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IdentityServiceRegistrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void UserManager_Is_Registered()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
        Assert.NotNull(userManager);
    }

    [Fact]
    public void RoleManager_Is_Registered()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();
        Assert.NotNull(roleManager);
    }

    [Fact]
    public void SignInManager_Is_Registered()
    {
        using var scope = _factory.Services.CreateScope();
        var signInManager = scope.ServiceProvider.GetService<SignInManager<ApplicationUser>>();
        Assert.NotNull(signInManager);
    }
}
