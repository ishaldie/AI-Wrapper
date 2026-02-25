using Microsoft.AspNetCore.Http;
using Xunit;
using ZSR.Underwriting.Web.Middleware;

namespace ZSR.Underwriting.Tests.Services;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Middleware_Adds_XContentTypeOptions_Header()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
    }

    [Fact]
    public async Task Middleware_Adds_XFrameOptions_Header()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
    }

    [Fact]
    public async Task Middleware_Adds_ReferrerPolicy_Header()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public async Task Middleware_Adds_PermissionsPolicy_Header()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var value = context.Response.Headers["Permissions-Policy"].ToString();
        Assert.Contains("camera=()", value);
        Assert.Contains("microphone=()", value);
        Assert.Contains("geolocation=()", value);
    }

    [Fact]
    public async Task Middleware_Adds_ContentSecurityPolicy_Header()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("default-src 'self'", csp);
    }

    [Fact]
    public async Task CSP_Allows_BlazorWebSocket()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("connect-src 'self' ws: wss:", csp);
    }

    [Fact]
    public async Task CSP_Allows_UnsafeInlineStyles_ForMudBlazor()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("style-src 'self' 'unsafe-inline'", csp);
    }

    [Fact]
    public async Task Middleware_Calls_Next_Delegate()
    {
        var nextCalled = false;
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
