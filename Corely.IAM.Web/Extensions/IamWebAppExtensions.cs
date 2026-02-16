using Corely.IAM.Web.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Corely.IAM.Web.Extensions;

public static class IamWebAppExtensions
{
    public static IApplicationBuilder UseIAMWebAuthentication(this IApplicationBuilder app)
    {
        // Order matters — each middleware builds on the previous:
        app.UseMiddleware<CorrelationIdMiddleware>(); // 1. Assigns correlation ID for request tracing
        app.UseMiddleware<SecurityHeadersMiddleware>(); // 2. Adds security headers (CSP, X-Frame-Options, etc.)
        app.UseMiddleware<AuthenticationTokenMiddleware>(); // 3. Validates JWT cookie → sets UserContext + ClaimsPrincipal
        app.UseAuthentication(); // 4. ASP.NET Core authentication middleware
        app.UseAuthorization(); // 5. ASP.NET Core authorization middleware

        return app;
    }
}
