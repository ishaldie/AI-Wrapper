using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class UserManagementServiceTests
{
    private ServiceProvider CreateServiceProvider()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddLogging();
        services.AddScoped<IUserManagementService, UserManagementService>();

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return sp;
    }

    [Fact]
    public async Task GetAllUsersAsync_Returns_Empty_When_No_Users()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        var users = await svc.GetAllUsersAsync();
        Assert.Empty(users);
    }

    [Fact]
    public async Task GetAllUsersAsync_Returns_Users_With_Roles()
    {
        using var sp = CreateServiceProvider();
        Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", "Test123!");
        try { await SeedData.SeedAsync(sp); }
        finally { Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null); }

        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        var users = await svc.GetAllUsersAsync();
        Assert.Single(users);
        Assert.Equal("admin@zsr.com", users[0].Email);
        Assert.Contains("Admin", users[0].Roles);
    }

    [Fact]
    public async Task AssignRoleAsync_Adds_Role_To_User()
    {
        using var sp = CreateServiceProvider();
        Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", "Test123!");
        try { await SeedData.SeedAsync(sp); }
        finally { Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null); }

        // Create a test user
        using (var scope = sp.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "analyst@zsr.com",
                Email = "analyst@zsr.com",
                FullName = "Test Analyst"
            };
            await um.CreateAsync(user, "Analyst123!");
        }

        using (var scope = sp.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await um.FindByEmailAsync("analyst@zsr.com");

            var result = await svc.AssignRoleAsync(user!.Id, "Admin");
            Assert.True(result);

            var roles = await um.GetRolesAsync(user);
            Assert.Contains("Admin", roles);
        }
    }

    [Fact]
    public async Task RemoveRoleAsync_Removes_Role_From_User()
    {
        using var sp = CreateServiceProvider();
        Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", "Test123!");
        try { await SeedData.SeedAsync(sp); }
        finally { Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null); }

        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await um.FindByEmailAsync("admin@zsr.com");

        var result = await svc.RemoveRoleAsync(admin!.Id, "Admin");
        Assert.True(result);

        var roles = await um.GetRolesAsync(admin);
        Assert.DoesNotContain("Admin", roles);
    }

    [Fact]
    public async Task AssignRoleAsync_Returns_False_For_Invalid_User()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        var result = await svc.AssignRoleAsync("nonexistent-id", "Admin");
        Assert.False(result);
    }
}
