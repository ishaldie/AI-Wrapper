using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create roles
        string[] roles = ["Admin", "Analyst"];
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{roleName}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        // Create default admin user
        const string adminEmail = "admin@zsr.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Admin",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, "Admin123!");
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
