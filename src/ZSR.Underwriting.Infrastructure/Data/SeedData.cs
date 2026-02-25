using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

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

        // Create default admin user (password from environment variable)
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("ADMIN_SEED_PASSWORD environment variable is not set — skipping admin user seeding");
        }
        else
        {
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

                var createResult = await userManager.CreateAsync(admin, adminPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }

                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // Seed checklist templates (idempotent — only if table is empty)
        if (!await db.ChecklistTemplates.AnyAsync())
        {
            var templates = ChecklistTemplateSeed.GetTemplates();
            db.ChecklistTemplates.AddRange(templates);
            await db.SaveChangesAsync();
        }
    }
}
