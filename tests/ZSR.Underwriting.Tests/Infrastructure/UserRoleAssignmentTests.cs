using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class UserRoleAssignmentTests
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

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return sp;
    }

    [Fact]
    public async Task Can_Create_User_And_Assign_Analyst_Role()
    {
        using var sp = CreateServiceProvider();

        // Seed roles first
        await SeedData.SeedAsync(sp);

        using var scope = sp.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "analyst@zsr.com",
            Email = "analyst@zsr.com",
            FullName = "Test Analyst"
        };

        var createResult = await userManager.CreateAsync(user, "Analyst123!");
        Assert.True(createResult.Succeeded);

        var roleResult = await userManager.AddToRoleAsync(user, "Analyst");
        Assert.True(roleResult.Succeeded);

        var roles = await userManager.GetRolesAsync(user);
        Assert.Contains("Analyst", roles);
    }

    [Fact]
    public async Task New_User_Has_No_Roles_By_Default()
    {
        using var sp = CreateServiceProvider();
        await SeedData.SeedAsync(sp);

        using var scope = sp.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "newuser@zsr.com",
            Email = "newuser@zsr.com",
            FullName = "New User"
        };

        await userManager.CreateAsync(user, "NewUser123!");
        var roles = await userManager.GetRolesAsync(user);
        Assert.Empty(roles);
    }

    [Fact]
    public async Task Can_Check_If_User_Is_In_Role()
    {
        // SeedData only creates the admin user when ADMIN_SEED_PASSWORD is set
        Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", "TestAdmin123!");
        try
        {
            using var sp = CreateServiceProvider();
            await SeedData.SeedAsync(sp);

            using var scope = sp.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var admin = await userManager.FindByEmailAsync("admin@zsr.com");
            Assert.NotNull(admin);
            Assert.True(await userManager.IsInRoleAsync(admin, "Admin"));
            Assert.False(await userManager.IsInRoleAsync(admin, "Analyst"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null);
        }
    }

    [Fact]
    public async Task Password_Must_Meet_Requirements()
    {
        using var sp = CreateServiceProvider();

        using var scope = sp.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "weak@zsr.com",
            Email = "weak@zsr.com",
            FullName = "Weak Pass"
        };

        // Too short / no uppercase / no digit
        var result = await userManager.CreateAsync(user, "abc");
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }
}
