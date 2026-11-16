using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SignInResult = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.Pages.Authentication;

public class VerifyEmailModel(
    IEmailVerificationService emailVerificationService,
    IAuthenticationService authenticationService,
    IPostAuthenticationFlowService postAuthenticationFlowService,
    TimeProvider timeProvider
) : PageModel
{
    public const string CHALLENGE_TEMP_DATA_KEY = "EmailVerificationChallengeToken";
    public const string EXPIRED_MESSAGE = "Your sign in has expired. Sign in again to continue.";

    [BindProperty]
    public string ChallengeToken { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public EmailVerificationStatus? Status { get; private set; }

    public string? Message { get; private set; }

    public string? ErrorMessage { get; private set; }

    public int ResendWaitSeconds =>
        Status?.NextResendUtc is { } next
            ? Math.Max(0, (int)Math.Ceiling((next - Now()).TotalSeconds))
            : 0;

    public static IActionResult RedirectFor(PageModel page, SignInResult result, string? returnUrl)
    {
        page.TempData[CHALLENGE_TEMP_DATA_KEY] = result.EmailVerificationChallengeToken;
        page.TempData[SignInModel.RETURN_URL_TEMP_DATA_KEY] = returnUrl;
        return new RedirectResult(AppRoutes.VerifyEmail);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (
            TempData[CHALLENGE_TEMP_DATA_KEY] is not string token
            || string.IsNullOrWhiteSpace(token)
        )
            return Redirect(AppRoutes.SignIn);

        ChallengeToken = token;
        ReturnUrl = (TempData[SignInModel.RETURN_URL_TEMP_DATA_KEY] as string).AsLocalUrl();
        return await ShowAsync();
    }

    public async Task<IActionResult> OnPostResendAsync()
    {
        var result = await emailVerificationService.ResendEmailVerificationAsync(ChallengeToken);
        if (result.ResultCode == RequestEmailVerificationResultCode.ChallengeNotFoundError)
            return Expired();

        if (result.ResultCode != RequestEmailVerificationResultCode.Success)
            ErrorMessage = result.Message;
        else if (result.Delivery == EmailVerificationDeliveryStatus.Failed)
            ErrorMessage = "The email could not be sent. Try again in a moment.";
        else
            Message = "A new link is on its way.";

        return await ShowAsync();
    }

    public async Task<IActionResult> OnPostContinueAsync()
    {
        var result = await authenticationService.CompleteEmailVerificationAsync(
            new CompleteEmailVerificationRequest(ChallengeToken)
        );

        if (result.ResultCode == SignInResultCode.Success)
            return await postAuthenticationFlowService.CompleteSignInAsync(
                HttpContext,
                result,
                ReturnUrl.AsLocalUrl()
            );

        if (result.ResultCode == SignInResultCode.EmailVerificationPendingError)
        {
            ErrorMessage = "Your email is not confirmed yet. Open the link in the email first.";
            return await ShowAsync();
        }

        return Expired();
    }

    public async Task<IActionResult> OnPostStatusAsync()
    {
        var result = await emailVerificationService.GetEmailVerificationStatusAsync(ChallengeToken);
        return new JsonResult(
            new
            {
                state = result.Status?.State.ToString() ?? "SignInExpired",
                resendWaitSeconds = result.Status?.NextResendUtc is { } next
                    ? Math.Max(0, (int)Math.Ceiling((next - Now()).TotalSeconds))
                    : 0,
            }
        );
    }

    private async Task<IActionResult> ShowAsync()
    {
        var result = await emailVerificationService.GetEmailVerificationStatusAsync(ChallengeToken);
        if (result.Status is null)
            return Expired();

        Status = result.Status;
        return Page();
    }

    private IActionResult Expired()
    {
        TempData["ErrorMessage"] = EXPIRED_MESSAGE;
        return Redirect(AppRoutes.SignIn);
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
}
