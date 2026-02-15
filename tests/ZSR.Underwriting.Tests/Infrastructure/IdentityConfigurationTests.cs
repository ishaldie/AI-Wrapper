using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class IdentityConfigurationTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void AppDbContext_Extends_IdentityDbContext()
    {
        using var context = CreateInMemoryContext();
        Assert.IsAssignableFrom<IdentityDbContext<ApplicationUser>>(context);
    }

    [Fact]
    public void AppDbContext_Has_Users_Table()
    {
        using var context = CreateInMemoryContext();
        // IdentityDbContext exposes Users DbSet
        var users = context.Users;
        Assert.NotNull(users);
    }

    [Fact]
    public void AppDbContext_Has_Roles_Table()
    {
        using var context = CreateInMemoryContext();
        var roles = context.Roles;
        Assert.NotNull(roles);
    }

    [Fact]
    public void AppDbContext_Still_Has_Deals_Table()
    {
        using var context = CreateInMemoryContext();
        var deals = context.Deals;
        Assert.NotNull(deals);
    }

    [Fact]
    public async Task Can_Add_ApplicationUser_To_Context()
    {
        using var context = CreateInMemoryContext();
        context.Database.EnsureCreated();

        var user = new ApplicationUser
        {
            UserName = "test@zsr.com",
            Email = "test@zsr.com",
            FullName = "Test User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var saved = await context.Users.FirstAsync();
        Assert.Equal("Test User", saved.FullName);
        Assert.Equal("test@zsr.com", saved.Email);
    }
}
