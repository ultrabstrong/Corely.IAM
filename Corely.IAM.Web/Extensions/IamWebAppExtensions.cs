using Corely.IAM.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Extensions;

public static class IamWebAppExtensions
{
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseIAMWebAuthentication()
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<BearerTokenMiddleware>();
            app.UseMiddleware<AuthenticationTokenMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }

        public IApplicationBuilder UseIAMApiResponses(string pathPrefix) =>
            app.UseWhen(
                context => context.Request.Path.StartsWithSegments(pathPrefix),
                api =>
                {
                    api.UseExceptionHandler(handler =>
                        handler.Run(context =>
                            context.Response.WriteApiFailureAsync(
                                context.Features.Get<IExceptionHandlerFeature>()?.Error
                                    is BadHttpRequestException badRequest
                                    ? badRequest.StatusCode
                                    : StatusCodes.Status500InternalServerError
                            )
                        )
                    );
                    api.UseStatusCodePages(pages =>
                        pages.Run(context =>
                            context.Response.WriteApiFailureAsync(context.Response.StatusCode)
                        )
                    );
                }
            );
    }
}
