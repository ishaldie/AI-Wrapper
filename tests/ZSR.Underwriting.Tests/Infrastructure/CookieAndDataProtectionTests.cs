using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class CookieAndDataProtectionTests
{
    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddLogging();

        // Apply the same cookie config as Program.cs
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.SlidingExpiration = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.HttpOnly = true;
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Cookie_SecurePolicy_Is_Always()
    {
        using var sp = BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
    }

    [Fact]
    public void Cookie_SameSite_Is_Strict()
    {
        using var sp = BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.Equal(SameSiteMode.Strict, options.Cookie.SameSite);
    }

    [Fact]
    public void Cookie_HttpOnly_Is_True()
    {
        using var sp = BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.True(options.Cookie.HttpOnly);
    }

    [Fact]
    public void Session_ExpireTimeSpan_Is_24Hours()
    {
        using var sp = BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.Equal(TimeSpan.FromHours(24), options.ExpireTimeSpan);
    }

    [Fact]
    public void SlidingExpiration_Is_Enabled()
    {
        using var sp = BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.True(options.SlidingExpiration);
    }
}
