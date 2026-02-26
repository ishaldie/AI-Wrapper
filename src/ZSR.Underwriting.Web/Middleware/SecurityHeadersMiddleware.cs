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

        // CSP compatible with Blazor Server + MudBlazor + Leaflet/OpenStreetMap:
        // - 'unsafe-inline' for styles (MudBlazor injects inline styles)
        // - 'unsafe-eval' for Blazor's JS interop
        // - ws:/wss: for Blazor SignalR WebSocket connection
        // - unpkg.com for Leaflet CDN, tile.openstreetmap.org for map tiles
        headers["Content-Security-Policy"] = string.Join("; ",
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://unpkg.com",
            "style-src 'self' 'unsafe-inline' https://unpkg.com",
            "img-src 'self' data: https://*.tile.openstreetmap.org https://unpkg.com",
            "font-src 'self' data:",
            "connect-src 'self' ws: wss:",
            "frame-ancestors 'none'");

        await _next(context);
    }
}
