using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Infrastructure;

[Collection(WebAppCollection.Name)]
public class IdentityServiceRegistrationTests
{
    private readonly WebAppFixture _fixture;

    public IdentityServiceRegistrationTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void UserManager_Is_Registered()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
        Assert.NotNull(userManager);
    }

    [Fact]
    public void RoleManager_Is_Registered()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();
        Assert.NotNull(roleManager);
    }

    [Fact]
    public void SignInManager_Is_Registered()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var signInManager = scope.ServiceProvider.GetService<SignInManager<ApplicationUser>>();
        Assert.NotNull(signInManager);
    }
}
