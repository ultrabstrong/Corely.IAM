using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class GoogleCallbackModel(
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

        if (result.ResultCode == SignInResultCode.GoogleAuthNotLinkedError)
        {
            TempData["GoogleIdToken"] = credential;
            return Redirect(AppRoutes.RegisterWithGoogle);
        }

        if (result.ResultCode != SignInResultCode.Success)
        {
            ErrorMessage = result.ResultCode switch
            {
                SignInResultCode.InvalidGoogleTokenError =>
                    "Google authentication failed. Please try again.",
                _ => $"Sign in failed: {result.ResultCode}",
            };
            return Page();
        }

        return await postAuthenticationFlowService.CompleteSignInAsync(
            HttpContext,
            result,
            _authTokenTtlSeconds
        );
    }
}
