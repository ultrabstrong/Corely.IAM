using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class RegisterWithGoogleModel(
    IRegistrationService registrationService,
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
        if (TempData["GoogleIdToken"] == null)
        {
            return Redirect(AppRoutes.SignIn);
        }
        TempData.Keep("GoogleIdToken");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var googleIdToken = TempData["GoogleIdToken"]?.ToString();
        if (string.IsNullOrWhiteSpace(googleIdToken))
        {
            return Redirect(AppRoutes.SignIn);
        }

        var registerResult = await registrationService.RegisterUserWithGoogleAsync(
            new RegisterUserWithGoogleRequest(googleIdToken)
        );

        if (registerResult.ResultCode != RegisterUserWithGoogleResultCode.Success)
        {
            ErrorMessage = registerResult.ResultCode switch
            {
                RegisterUserWithGoogleResultCode.InvalidGoogleTokenError =>
                    "Google authentication expired. Please try again.",
                RegisterUserWithGoogleResultCode.GoogleAccountInUseError =>
                    "This Google account is already linked to another user.",
                RegisterUserWithGoogleResultCode.UserExistsError =>
                    "An account with this email already exists. Sign in with your password and link Google from your profile.",
                _ => $"Registration failed: {registerResult.ResultCode}",
            };
            return Page();
        }

        var deviceId = authCookieManager.GetOrCreateDeviceId(HttpContext);
        var signInResult = await authenticationService.SignInWithGoogleAsync(
            new SignInWithGoogleRequest(googleIdToken, deviceId)
        );

        if (signInResult.ResultCode == SignInResultCode.MfaRequiredChallenge)
        {
            TempData["MfaChallengeToken"] = signInResult.MfaChallengeToken;
            return Redirect(AppRoutes.VerifyMfa);
        }

        if (signInResult.ResultCode != SignInResultCode.Success)
        {
            ErrorMessage = "Account created but sign-in failed. Please sign in manually.";
            return Page();
        }

        authCookieManager.SetAuthCookies(
            Response.Cookies,
            signInResult.AuthToken!,
            signInResult.AuthTokenId!.Value,
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
