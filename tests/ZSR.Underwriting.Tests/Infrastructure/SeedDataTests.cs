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
    public async Task SeedAsync_Creates_Default_Admin_User_When_EnvVar_Set()
    {
        Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", "Test1234!");
        try
        {
            using var sp = await CreateServiceProviderAsync();
            await SeedData.SeedAsync(sp);

            using var scope = sp.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await userManager.FindByEmailAsync("admin@zsr.com");
            Assert.NotNull(admin);
            Assert.Equal("Admin", admin.FullName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null);
        }
    }

    [Fact]
    public async Task SeedAsync_Assigns_Admin_Role_To_Default_User()
    {
        Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", "Test1234!");
        try
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
        finally
        {
            Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null);
        }
    }

    [Fact]
    public async Task SeedAsync_Is_Idempotent()
    {
        Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", "Test1234!");
        try
        {
            using var sp = await CreateServiceProviderAsync();
            await SeedData.SeedAsync(sp);
            await SeedData.SeedAsync(sp); // Run twice

            using var scope = sp.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            Assert.True(await roleManager.RoleExistsAsync("Admin"));
            Assert.True(await roleManager.RoleExistsAsync("Analyst"));
            var admin = await userManager.FindByEmailAsync("admin@zsr.com");
            Assert.NotNull(admin);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null);
        }
    }

    [Fact]
    public async Task SeedAsync_Skips_Admin_When_EnvVar_Missing()
    {
        Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null);
        try
        {
            using var sp = await CreateServiceProviderAsync();
            await SeedData.SeedAsync(sp);

            using var scope = sp.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await userManager.FindByEmailAsync("admin@zsr.com");
            Assert.Null(admin);

            // Roles should still be created even without admin user
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True(await roleManager.RoleExistsAsync("Admin"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ADMIN_SEED_PASSWORD", null);
        }
    }
}
