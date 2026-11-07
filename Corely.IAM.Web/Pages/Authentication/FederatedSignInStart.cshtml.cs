using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.Pages.Authentication;

public class FederatedSignInStartModel(IFederatedSignInService federatedSignInService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Provider { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Purpose { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ReturnUrl = ReturnUrl.AsLocalUrl();
        if (!Enum.TryParse<IdentityProviderType>(Provider, true, out var type))
            return NotFound();

        var purpose = Enum.TryParse<FederatedSignInPurpose>(Purpose, true, out var parsed)
            ? parsed
            : FederatedSignInPurpose.SignIn;

        var result = await federatedSignInService.StartFederatedSignInAsync(
            new StartFederatedSignInRequest(type, purpose, Request.FederatedCallbackUri(type))
        );
        if (result.ResultCode != StartFederatedSignInResultCode.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        HttpContext
            .RequestServices.GetRequiredService<FederatedSignInCookie>()
            .Set(HttpContext, new FederatedSignInState(result.Correlation!, ReturnUrl));
        return Redirect(result.AuthorizationUrl!);
    }
}
