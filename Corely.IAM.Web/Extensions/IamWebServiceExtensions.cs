using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

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

            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/signin";
                    options.LogoutPath = "/signout";
                });

            services.AddAuthorization();

            return services;
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
}
