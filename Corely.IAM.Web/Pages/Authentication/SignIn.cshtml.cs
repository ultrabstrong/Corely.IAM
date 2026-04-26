using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
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
    IOptions<SecurityOptions> securityOptions
) : PageModel
{
    private readonly int _authTokenTtlSeconds = securityOptions.Value.AuthTokenTtlSeconds;

    public string? GoogleClientId { get; } = securityOptions.Value.GoogleClientId;

    public string? GoogleCallbackUrl { get; set; }

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

        if (!string.IsNullOrWhiteSpace(GoogleClientId))
        {
            var request = HttpContext.Request;
            GoogleCallbackUrl = $"{request.Scheme}://{request.Host}{AppRoutes.GoogleCallback}";
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

        if (result.ResultCode == SignInResultCode.MfaRequiredChallenge)
        {
            TempData["MfaChallengeToken"] = result.MfaChallengeToken;
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
            _authTokenTtlSeconds
        );
    }
}
