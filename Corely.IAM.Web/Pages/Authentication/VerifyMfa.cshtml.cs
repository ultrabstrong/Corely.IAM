using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class VerifyMfaModel(
    IAuthenticationService authenticationService,
    IPostAuthenticationFlowService postAuthenticationFlowService,
    IOptions<SecurityOptions> securityOptions
) : PageModel
{
    private readonly int _authTokenTtlSeconds = securityOptions.Value.AuthTokenTtlSeconds;

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public string MfaChallengeToken { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        var token = TempData["MfaChallengeToken"] as string;
        if (string.IsNullOrWhiteSpace(token))
        {
            return Redirect(AppRoutes.SignIn);
        }

        MfaChallengeToken = token;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(MfaChallengeToken))
        {
            return Redirect(AppRoutes.SignIn);
        }

        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Please enter a code.";
            return Page();
        }

        var result = await authenticationService.VerifyMfaAsync(
            new VerifyMfaRequest(MfaChallengeToken, Code.Trim())
        );

        if (result.ResultCode == SignInResultCode.MfaChallengeExpiredError)
        {
            TempData["ErrorMessage"] = "Your MFA session has expired. Please sign in again.";
            return Redirect(AppRoutes.SignIn);
        }

        if (result.ResultCode != SignInResultCode.Success)
        {
            ErrorMessage =
                result.ResultCode == SignInResultCode.InvalidMfaCodeError
                    ? "Invalid code. Please try again."
                    : result.Message ?? "Verification failed.";
            return Page();
        }

        return await postAuthenticationFlowService.CompleteSignInAsync(
            HttpContext,
            result,
            _authTokenTtlSeconds
        );
    }
}
