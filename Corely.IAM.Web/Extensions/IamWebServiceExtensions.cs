using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.Extensions;

public static class IamWebServiceExtensions
{
    public static IServiceCollection AddIAMWeb(this IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddHttpContextAccessor();

        services.AddSingleton<IAuthCookieManager, AuthCookieManager>();
        services.AddSingleton<IUserContextClaimsBuilder, UserContextClaimsBuilder>();

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

    public static IServiceCollection AddIAMWebBlazor(this IServiceCollection services)
    {
        services.AddScoped<IBlazorUserContextAccessor, BlazorUserContextAccessor>();
        services.AddScoped<AuthenticationStateProvider, IamAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();

        return services;
    }
}
