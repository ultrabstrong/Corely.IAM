using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Corely.IAM.Web.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CORRELATION_ID_HEADER = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers[CORRELATION_ID_HEADER].FirstOrDefault()
            ?? Guid.CreateVersion7().ToString();

        context.Response.Headers[CORRELATION_ID_HEADER] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
