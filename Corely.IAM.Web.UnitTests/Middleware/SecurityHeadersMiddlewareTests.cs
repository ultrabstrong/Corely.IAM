using Corely.IAM.Web.Middleware;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.UnitTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Theory]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-XSS-Protection", "1; mode=block")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("Cache-Control", "no-store, no-cache, must-revalidate")]
    public async Task InvokeAsync_Always_SetsExpectedHeader(string headerName, string expectedValue)
    {
        RequestDelegate next = (_) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.ContainsKey(headerName));
        Assert.Equal(expectedValue, httpContext.Response.Headers[headerName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_Always_SetsPermissionsPolicy()
    {
        RequestDelegate next = (_) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.ContainsKey("Permissions-Policy"));
        var value = httpContext.Response.Headers["Permissions-Policy"].ToString();
        Assert.Contains("camera=()", value);
        Assert.Contains("microphone=()", value);
        Assert.Contains("geolocation=()", value);
        Assert.Contains("payment=()", value);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSet_ContentSecurityPolicy()
    {
        RequestDelegate next = (_) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.False(httpContext.Response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task InvokeAsync_Always_CallsNextDelegate()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new SecurityHeadersMiddleware(next);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
    }
}
