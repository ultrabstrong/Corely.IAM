using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Corely.IAM.Web.Extensions;

public static class IamWebServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIAMWeb(Action<IAMWebOptions>? configure = null)
        {
            services.AddRazorPages();
            services.AddHttpContextAccessor();
            services.Configure(configure ?? (_ => { }));

            services.AddSingleton<IAuthCookieManager, AuthCookieManager>();
            services.AddSingleton<IUserContextClaimsBuilder, UserContextClaimsBuilder>();
            services.AddScoped<IPostAuthenticationFlowService, PostAuthenticationFlowService>();
            services.TryAddScoped<IHumanVerifier, NoHumanVerifier>();

            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/signin";
                    options.LogoutPath = "/signout";
                    options.Events.OnRedirectToLogin = context =>
                        context.HttpContext.IsIAMApiRequest()
                            ? context.Response.BearerChallengeAsync(
                                StatusCodes.Status401Unauthorized
                            )
                            : RedirectAsync(context);
                    options.Events.OnRedirectToAccessDenied = context =>
                        context.HttpContext.IsIAMApiRequest()
                            ? context.Response.BearerChallengeAsync(StatusCodes.Status403Forbidden)
                            : RedirectAsync(context);
                });

            services.AddAuthorization();

            return services;
        }

        public IServiceCollection AddIAMWebBlazor<TLinks>()
            where TLinks : class, IIamWebLinks
        {
            services.AddSingleton<IIamWebLinks, TLinks>();
            return services.AddIAMWebBlazor();
        }

        public IServiceCollection AddIAMWebBlazor()
        {
            services.AddScoped<IBlazorUserContextAccessor, BlazorUserContextAccessor>();
            services.AddScoped<AuthenticationStateProvider, IamAuthenticationStateProvider>();
            services.AddScoped<IAccountDisplayState, AccountDisplayState>();
            services.AddCascadingAuthenticationState();

            return services;
        }
    }

    private static Task RedirectAsync(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
