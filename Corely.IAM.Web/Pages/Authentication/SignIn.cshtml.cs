using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class SignInModel(
    IAuthenticationService authenticationService,
    IAuthCookieManager authCookieManager,
    IPostAuthenticationFlowService postAuthenticationFlowService,
    IOptions<SecurityOptions> securityOptions,
    IOptions<IAMWebOptions> iamWebOptions
) : PageModel
{
    public const string RETURN_URL_TEMP_DATA_KEY = "ReturnUrl";

    public string? GoogleClientId { get; } = securityOptions.Value.GoogleClientId;
    public string? ForgotPasswordPath { get; } = iamWebOptions.Value.ForgotPasswordPath;

    public string? GoogleCallbackUrl { get; set; }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        ReturnUrl = ReturnUrl.AsLocalUrl();

        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(ReturnUrl ?? iamWebOptions.Value.HomePath);
        }

        if (!string.IsNullOrWhiteSpace(GoogleClientId))
        {
            var request = HttpContext.Request;
            GoogleCallbackUrl = $"{request.Scheme}://{request.Host}{AppRoutes.GoogleCallback}";
            if (ReturnUrl != null)
            {
                authCookieManager.SetReturnUrlCookie(HttpContext, ReturnUrl);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = ReturnUrl.AsLocalUrl();

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required.";
            return Page();
        }

        var deviceId = authCookieManager.GetOrCreateDeviceId(HttpContext);
        var result = await authenticationService.SignInAsync(
            new SignInRequest(Username, Password, deviceId)
        );

        if (result.ResultCode == SignInResultCode.MfaRequiredChallenge)
        {
            TempData["MfaChallengeToken"] = result.MfaChallengeToken;
            TempData[RETURN_URL_TEMP_DATA_KEY] = ReturnUrl;
            return Redirect(AppRoutes.VerifyMfa);
        }

        if (result.ResultCode != SignInResultCode.Success)
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        return await postAuthenticationFlowService.CompleteSignInAsync(
            HttpContext,
            result,
            ReturnUrl
        );
    }
}
