using Corely.IAM.Web.Middleware;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.UnitTests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private const string CORRELATION_ID_HEADER = "X-Correlation-ID";

    [Fact]
    public async Task InvokeAsync_NoIncomingHeader_GeneratesNewCorrelationId()
    {
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new CorrelationIdMiddleware(next);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
        Assert.True(httpContext.Response.Headers.ContainsKey(CORRELATION_ID_HEADER));
        var correlationId = httpContext.Response.Headers[CORRELATION_ID_HEADER].ToString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_ExistingHeader_UsesSameCorrelationId()
    {
        var expectedId = "my-custom-correlation-id";
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new CorrelationIdMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[CORRELATION_ID_HEADER] = expectedId;

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
        var responseCorrelationId = httpContext.Response.Headers[CORRELATION_ID_HEADER].ToString();
        Assert.Equal(expectedId, responseCorrelationId);
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
        var middleware = new CorrelationIdMiddleware(next);
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
    }
}
