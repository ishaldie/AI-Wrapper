using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Web.Endpoints;

public static class ExternalAuthEndpoints
{
    private static readonly HashSet<string> AllowedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Google", "Microsoft"
    };

    public static void MapExternalAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/external-login", HandleExternalLogin)
            .AllowAnonymous();

        app.MapGet("/api/auth/external-callback", HandleExternalCallback)
            .AllowAnonymous();
    }

    private static IResult HandleExternalLogin(string provider, string? returnUrl = "/search")
    {
        if (!AllowedProviders.Contains(provider))
        {
            return Results.BadRequest($"Invalid provider: {provider}");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/api/auth/external-callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/search")}",
            Items = { ["LoginProvider"] = provider }
        };

        return Results.Challenge(properties, [provider]);
    }

    private static async Task<IResult> HandleExternalCallback(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        string? returnUrl = "/search")
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return Results.Redirect("/login?error=ExternalLoginFailed");
        }

        // Try signing in with existing external login link
        var signInResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            return Results.Redirect(returnUrl ?? "/search");
        }

        // No existing link — check if user with this email exists
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return Results.Redirect("/login?error=NoEmail");
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            // Link external login to existing account
            var linkResult = await userManager.AddLoginAsync(existingUser, info);
            if (!linkResult.Succeeded)
            {
                return Results.Redirect("/login?error=LinkFailed");
            }

            await signInManager.SignInAsync(existingUser, isPersistent: true);
            return Results.Redirect(returnUrl ?? "/search");
        }

        // Brand new user — auto-create with Analyst role
        var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            return Results.Redirect("/login?error=CreateFailed");
        }

        await userManager.AddToRoleAsync(newUser, "Analyst");
        await userManager.AddLoginAsync(newUser, info);
        await signInManager.SignInAsync(newUser, isPersistent: true);

        return Results.Redirect(returnUrl ?? "/search");
    }
}
