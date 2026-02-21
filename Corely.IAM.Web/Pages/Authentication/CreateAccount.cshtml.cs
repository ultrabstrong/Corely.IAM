using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class CreateAccountModel(
    IRegistrationService registrationService,
    IAuthenticationService authenticationService,
    IUserContextProvider userContextProvider,
    IAuthCookieManager authCookieManager,
    IOptions<SecurityOptions> securityOptions
) : PageModel
{
    private readonly int _authTokenTtlSeconds = securityOptions.Value.AuthTokenTtlSeconds;

    public string AccountName { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        var userContext = userContextProvider.GetUserContext();
        if (userContext == null)
        {
            return Redirect(AppRoutes.SignIn);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string accountName)
    {
        AccountName = accountName;

        if (string.IsNullOrWhiteSpace(accountName))
        {
            ErrorMessage = "Account name is required.";
            return Page();
        }

        var createResult = await registrationService.RegisterAccountAsync(
            new RegisterAccountRequest(accountName)
        );

        if (createResult.ResultCode != RegisterAccountResultCode.Success)
        {
            ErrorMessage = "Failed to create account. Please try again.";
            return Page();
        }

        // Auto-switch into the newly created account
        var switchResult = await authenticationService.SwitchAccountAsync(
            new SwitchAccountRequest(createResult.CreatedAccountId)
        );

        if (switchResult.ResultCode != SignInResultCode.Success)
        {
            // Account was created but switch failed — let them pick it from the list
            return Redirect(AppRoutes.SelectAccount);
        }

        authCookieManager.SetAuthCookies(
            Response.Cookies,
            switchResult.AuthToken!,
            switchResult.AuthTokenId!.Value,
            Request.IsHttps,
            _authTokenTtlSeconds
        );

        return Redirect(AppRoutes.Dashboard);
    }
}
