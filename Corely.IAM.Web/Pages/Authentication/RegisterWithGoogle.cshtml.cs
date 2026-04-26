using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class RegisterWithGoogleModel(
    IRegistrationService registrationService,
    IAuthenticationService authenticationService,
    IAuthCookieManager authCookieManager,
    IPostAuthenticationFlowService postAuthenticationFlowService,
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

        return await postAuthenticationFlowService.CompleteSignInAsync(
            HttpContext,
            signInResult,
            _authTokenTtlSeconds
        );
    }
}
