namespace ZSR.Underwriting.Web.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // CSP compatible with Blazor Server + MudBlazor + Google Maps:
        // - 'unsafe-inline' for styles (MudBlazor injects inline styles)
        // - 'unsafe-eval' for Blazor's JS interop
        // - ws:/wss: for Blazor SignalR WebSocket connection
        // - maps.googleapis.com / maps.gstatic.com for Google Maps API
        headers["Content-Security-Policy"] = string.Join("; ",
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://maps.googleapis.com",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data: https://maps.gstatic.com https://maps.googleapis.com https://*.ggpht.com https://*.google.com https://*.googleusercontent.com",
            "font-src 'self' data: https://fonts.gstatic.com",
            "connect-src 'self' ws: wss: https://maps.googleapis.com",
            "frame-ancestors 'none'");

        await _next(context);
    }
}
