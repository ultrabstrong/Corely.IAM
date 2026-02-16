using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class SignInModel(
    IAuthenticationService authenticationService,
    IUserContextProvider userContextProvider,
    IAuthCookieManager authCookieManager,
    IOptions<SecurityOptions> securityOptions
) : PageModel
{
    private readonly int _authTokenTtlSeconds = securityOptions.Value.AuthTokenTtlSeconds;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Request.Cookies.ContainsKey(AuthenticationConstants.AUTH_TOKEN_COOKIE))
        {
            return Redirect(AppRoutes.Dashboard);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required.";
            return Page();
        }

        var deviceId = authCookieManager.GetOrCreateDeviceId(HttpContext);
        var result = await authenticationService.SignInAsync(
            new SignInRequest(Username, Password, deviceId)
        );

        if (result.ResultCode != SignInResultCode.Success)
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        authCookieManager.SetAuthCookies(
            Response.Cookies,
            result.AuthToken!,
            result.AuthTokenId!.Value,
            Request.IsHttps,
            _authTokenTtlSeconds
        );

        var userContext = userContextProvider.GetUserContext();
        if (userContext == null || userContext.AvailableAccounts.Count == 0)
        {
            return Redirect(AppRoutes.Dashboard);
        }

        if (userContext.AvailableAccounts.Count == 1)
        {
            // Auto-select the only account
            var switchResult = await authenticationService.SwitchAccountAsync(
                new SwitchAccountRequest(userContext.AvailableAccounts[0].Id)
            );

            if (switchResult.ResultCode == SignInResultCode.Success)
            {
                authCookieManager.SetAuthCookies(
                    Response.Cookies,
                    switchResult.AuthToken!,
                    switchResult.AuthTokenId!.Value,
                    Request.IsHttps,
                    _authTokenTtlSeconds
                );
                return Redirect(AppRoutes.Dashboard);
            }

            // Auto-switch failed — fall through to account selection
            return Redirect(AppRoutes.SelectAccount);
        }

        // Multiple accounts — let user choose
        return Redirect(AppRoutes.SelectAccount);
    }
}
