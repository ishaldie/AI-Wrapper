using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class SeedDataTests
{
    private async Task<ServiceProvider> CreateServiceProviderAsync()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddLogging();

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return sp;
    }

    [Fact]
    public async Task SeedAsync_Creates_Admin_Role()
    {
        using var sp = await CreateServiceProviderAsync();
        await SeedData.SeedAsync(sp);

        using var scope = sp.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True(await roleManager.RoleExistsAsync("Admin"));
    }

    [Fact]
    public async Task SeedAsync_Creates_Analyst_Role()
    {
        using var sp = await CreateServiceProviderAsync();
        await SeedData.SeedAsync(sp);

        using var scope = sp.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True(await roleManager.RoleExistsAsync("Analyst"));
    }

    [Fact]
    public async Task SeedAsync_Creates_Default_Admin_User()
    {
        using var sp = await CreateServiceProviderAsync();
        await SeedData.SeedAsync(sp);

        using var scope = sp.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@zsr.com");
        Assert.NotNull(admin);
        Assert.Equal("Admin", admin.FullName);
    }

    [Fact]
    public async Task SeedAsync_Assigns_Admin_Role_To_Default_User()
    {
        using var sp = await CreateServiceProviderAsync();
        await SeedData.SeedAsync(sp);

        using var scope = sp.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@zsr.com");
        Assert.NotNull(admin);
        var roles = await userManager.GetRolesAsync(admin);
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public async Task SeedAsync_Is_Idempotent()
    {
        using var sp = await CreateServiceProviderAsync();
        await SeedData.SeedAsync(sp);
        await SeedData.SeedAsync(sp); // Run twice

        using var scope = sp.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Should still have exactly 2 roles and 1 admin user
        Assert.True(await roleManager.RoleExistsAsync("Admin"));
        Assert.True(await roleManager.RoleExistsAsync("Analyst"));
        var admin = await userManager.FindByEmailAsync("admin@zsr.com");
        Assert.NotNull(admin);
    }
}
