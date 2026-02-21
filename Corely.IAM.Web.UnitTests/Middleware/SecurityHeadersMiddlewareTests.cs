using Corely.IAM.Web.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Corely.IAM.Web.UnitTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnv = new();

    private SecurityHeadersMiddleware CreateMiddleware(bool isDevelopment = false)
    {
        _mockEnv
            .Setup(e => e.EnvironmentName)
            .Returns(isDevelopment ? Environments.Development : Environments.Production);
        return new SecurityHeadersMiddleware(_ => Task.CompletedTask, _mockEnv.Object);
    }

    [Theory]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("Cache-Control", "no-store, no-cache, must-revalidate")]
    [InlineData("Pragma", "no-cache")]
    [InlineData("Cross-Origin-Opener-Policy", "same-origin")]
    [InlineData("Cross-Origin-Resource-Policy", "same-origin")]
    [InlineData("X-Permitted-Cross-Domain-Policies", "none")]
    public async Task InvokeAsync_Always_SetsExpectedHeader(string headerName, string expectedValue)
    {
        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.ContainsKey(headerName));
        Assert.Equal(expectedValue, httpContext.Response.Headers[headerName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_Always_SetsPermissionsPolicy()
    {
        var middleware = CreateMiddleware();
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
    public async Task InvokeAsync_Always_SetsContentSecurityPolicy()
    {
        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.ContainsKey("Content-Security-Policy"));
        var csp = httpContext.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("script-src", csp);
        Assert.Contains("connect-src 'self' wss: ws:", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("form-action 'self'", csp);
        Assert.Contains("base-uri 'self'", csp);
    }

    [Fact]
    public async Task InvokeAsync_InProduction_SetsHsts()
    {
        var middleware = CreateMiddleware(isDevelopment: false);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.ContainsKey("Strict-Transport-Security"));
        var hsts = httpContext.Response.Headers["Strict-Transport-Security"].ToString();
        Assert.Contains("max-age=31536000", hsts);
        Assert.Contains("includeSubDomains", hsts);
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_DoesNotSetHsts()
    {
        var middleware = CreateMiddleware(isDevelopment: true);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.False(httpContext.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSet_DeprecatedXXssProtection()
    {
        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.False(httpContext.Response.Headers.ContainsKey("X-XSS-Protection"));
    }

    [Fact]
    public async Task InvokeAsync_Always_CallsNextDelegate()
    {
        bool nextCalled = false;
        _mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var middleware = new SecurityHeadersMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            _mockEnv.Object
        );
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
    }
}
