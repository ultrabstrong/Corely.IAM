using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.Pages.Authentication;

public class FederatedSignInCallbackModel(
    IFederatedSignInService federatedSignInService,
    IAuthCookieManager authCookieManager,
    IPostAuthenticationFlowService postAuthenticationFlowService
) : PageModel
{
    public const string START_AGAIN_MESSAGE =
        "This sign in could not be matched to one started here. Start again.";

    [BindProperty(SupportsGet = true)]
    public string Provider { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string BackUrl { get; set; } = AppRoutes.SignIn;

    public async Task<IActionResult> OnGetAsync()
    {
        var state = HttpContext
            .RequestServices.GetRequiredService<FederatedSignInCookie>()
            .Take(HttpContext);
        if (
            state is null
            || !Enum.TryParse<IdentityProviderType>(Provider, true, out var type)
            || state.Correlation.Type != type
        )
        {
            ErrorMessage = START_AGAIN_MESSAGE;
            return Page();
        }

        var returnUrl = state.ReturnUrl.AsLocalUrl();
        if (state.Correlation.Purpose != FederatedSignInPurpose.SignIn && returnUrl is not null)
            BackUrl = returnUrl;

        var query = Request.Query;
        var result = await federatedSignInService.CompleteFederatedSignInAsync(
            new CompleteFederatedSignInRequest(
                state.Correlation,
                Request.FederatedCallbackUri(type),
                authCookieManager.GetOrCreateDeviceId(HttpContext),
                query["code"],
                query["state"],
                query["iss"],
                query["error"]
            )
        );

        if (result.ResultCode == CompleteFederatedSignInResultCode.MfaRequiredChallenge)
        {
            TempData["MfaChallengeToken"] = result.SignIn!.MfaChallengeToken;
            TempData[SignInModel.RETURN_URL_TEMP_DATA_KEY] = returnUrl;
            return Redirect(AppRoutes.VerifyMfa);
        }

        if (result.ResultCode != CompleteFederatedSignInResultCode.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        return result.Purpose == FederatedSignInPurpose.SignIn
            ? await postAuthenticationFlowService.CompleteSignInAsync(
                HttpContext,
                result.SignIn!,
                returnUrl
            )
            : Redirect(returnUrl ?? "/");
    }
}
