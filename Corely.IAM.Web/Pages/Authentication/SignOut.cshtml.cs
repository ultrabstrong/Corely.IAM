using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.Web.Pages.Authentication;

public class SignOutModel(
    IAuthenticationService authenticationService,
    IAuthCookieManager authCookieManager
) : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect(AppRoutes.Dashboard);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var tokenId = Request.Cookies[AuthenticationConstants.AUTH_TOKEN_ID_COOKIE];
        if (!string.IsNullOrWhiteSpace(tokenId))
        {
            await authenticationService.SignOutAsync(new SignOutRequest(tokenId));
        }

        authCookieManager.DeleteAuthCookies(Response.Cookies);
        authCookieManager.DeleteDeviceIdCookie(Response.Cookies);

        return Redirect(AppRoutes.SignIn);
    }
}
