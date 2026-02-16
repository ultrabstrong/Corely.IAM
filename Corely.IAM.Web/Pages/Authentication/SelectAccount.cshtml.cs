using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class SelectAccountModel(
    IAuthenticationService authenticationService,
    IUserContextProvider userContextProvider,
    IAuthCookieManager authCookieManager,
    IOptions<SecurityOptions> securityOptions
) : PageModel
{
    private readonly int _authTokenTtlSeconds = securityOptions.Value.AuthTokenTtlSeconds;

    public List<Account> Accounts { get; set; } = [];

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        var userContext = userContextProvider.GetUserContext();
        if (userContext == null)
        {
            return Redirect(AppRoutes.SignIn);
        }

        if (userContext.CurrentAccount != null)
        {
            return Redirect(AppRoutes.Dashboard);
        }

        Accounts = userContext.AvailableAccounts;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid accountId)
    {
        var result = await authenticationService.SwitchAccountAsync(
            new SwitchAccountRequest(accountId)
        );

        if (result.ResultCode != SignInResultCode.Success)
        {
            ErrorMessage = "Failed to switch account.";
            var userContext = userContextProvider.GetUserContext();
            Accounts = userContext?.AvailableAccounts ?? [];
            return Page();
        }

        authCookieManager.SetAuthCookies(
            Response.Cookies,
            result.AuthToken!,
            result.AuthTokenId!.Value,
            Request.IsHttps,
            _authTokenTtlSeconds
        );

        return Redirect(AppRoutes.Dashboard);
    }
}
