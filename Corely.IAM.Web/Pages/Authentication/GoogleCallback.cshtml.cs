using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class GoogleCallbackModel(
    IAuthenticationService authenticationService,
    IUserContextProvider userContextProvider,
    IAuthCookieManager authCookieManager,
    IOptions<SecurityOptions> securityOptions
) : PageModel
{
    private readonly int _authTokenTtlSeconds = securityOptions.Value.AuthTokenTtlSeconds;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        return Redirect(AppRoutes.SignIn);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var credential = Request.Form["credential"].ToString();
        if (string.IsNullOrWhiteSpace(credential))
        {
            ErrorMessage = "No credential received from Google.";
            return Page();
        }

        var deviceId = authCookieManager.GetOrCreateDeviceId(HttpContext);
        var result = await authenticationService.SignInWithGoogleAsync(
            new SignInWithGoogleRequest(credential, deviceId)
        );

        if (result.ResultCode == SignInResultCode.MfaRequiredChallenge)
        {
            TempData["MfaChallengeToken"] = result.MfaChallengeToken;
            return Redirect(AppRoutes.VerifyMfa);
        }

        if (result.ResultCode != SignInResultCode.Success)
        {
            ErrorMessage = result.ResultCode switch
            {
                SignInResultCode.InvalidGoogleTokenError =>
                    "Google authentication failed. Please try again.",
                SignInResultCode.GoogleAuthNotLinkedError =>
                    "No account is linked to this Google account. Sign in with your username and password, then link your Google account from the Profile page.",
                _ => $"Sign in failed: {result.ResultCode}",
            };
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

            return Redirect(AppRoutes.SelectAccount);
        }

        return Redirect(AppRoutes.SelectAccount);
    }
}
