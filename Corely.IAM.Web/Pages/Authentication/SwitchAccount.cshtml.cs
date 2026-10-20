using Corely.IAM.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Services;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class SwitchAccountModel(
    IAuthenticationService authenticationService,
    IAuthCookieManager authCookieManager,
    ISecurityPolicyProvider securityPolicyProvider,
    IOptions<IAMWebOptions> iamWebOptions
) : PageModel
{
    public async Task<IActionResult> OnGetAsync(Guid? accountId = null, string? returnUrl = null)
    {
        if (accountId == null)
        {
            return Redirect(iamWebOptions.Value.HomePath);
        }

        return await SwitchAndRedirectAsync(accountId.Value, returnUrl);
    }

    public async Task<IActionResult> OnPostAsync(Guid accountId, string? returnUrl)
    {
        return await SwitchAndRedirectAsync(accountId, returnUrl);
    }

    private async Task<IActionResult> SwitchAndRedirectAsync(Guid accountId, string? returnUrl)
    {
        var result = await authenticationService.SwitchAccountAsync(
            new SwitchAccountRequest(accountId)
        );

        if (result.ResultCode != SignInResultCode.Success)
        {
            return Redirect(AppRoutes.SelectAccount);
        }

        authCookieManager.SetAuthCookies(
            Response.Cookies,
            result.AuthToken!,
            result.AuthTokenId!.Value,
            Request.IsHttps,
            (await securityPolicyProvider.GetPolicyAsync()).AuthSessionTtlSeconds
        );

        return Redirect(returnUrl.AsLocalUrl() ?? iamWebOptions.Value.HomePath);
    }
}
