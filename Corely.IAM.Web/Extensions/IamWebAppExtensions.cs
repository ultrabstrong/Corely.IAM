using Corely.IAM.Web.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Corely.IAM.Web.Extensions;

public static class IamWebAppExtensions
{
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseIAMWebAuthentication()
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<AuthenticationTokenMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
