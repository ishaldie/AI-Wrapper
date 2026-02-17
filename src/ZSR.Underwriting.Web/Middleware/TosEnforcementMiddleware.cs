using Microsoft.AspNetCore.Identity;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Web.Middleware;

public class TosEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/accept-terms",
        "/terms",
        "/privacy",
        "/logout",
        "/login",
        "/register",
        "/verify-code",
        "/not-found",
        "/Error"
    };

    private static readonly string[] ExemptPrefixes =
    [
        "/api/auth/",
        "/_blazor",
        "/_framework"
    ];

    public TosEnforcementMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip exempt paths
        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        // Skip static assets
        if (path.Contains('.') && !path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Only check authenticated users
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.User);

        if (user is not null)
        {
            var requiredVersion = _configuration["Application:TosVersion"] ?? "1.0";
            if (user.TosVersion != requiredVersion)
            {
                context.Response.Redirect("/accept-terms");
                return;
            }
        }

        await _next(context);
    }

    private static bool IsExemptPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return true;

        if (ExemptPaths.Contains(path))
            return true;

        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
