using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class RegisterModel(
    IRegistrationService registrationService,
    IAuthenticationService authenticationService,
    IAuthCookieManager authCookieManager,
    IOptions<SecurityOptions> securityOptions
) : PageModel
{
    private readonly int _authTokenTtlSeconds = securityOptions.Value.AuthTokenTtlSeconds;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Request.Cookies.ContainsKey(AuthenticationConstants.AUTH_TOKEN_COOKIE))
        {
            return Redirect(AppRoutes.Dashboard);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        var registerResult = await registrationService.RegisterUserAsync(
            new RegisterUserRequest(Username, Email, Password)
        );

        if (registerResult.ResultCode != RegisterUserResultCode.Success)
        {
            ErrorMessage = registerResult.Message ?? "Registration failed.";
            return Page();
        }

        // Auto sign-in after registration
        var deviceId = authCookieManager.GetOrCreateDeviceId(HttpContext);
        var signInResult = await authenticationService.SignInAsync(
            new SignInRequest(Username, Password, deviceId)
        );

        if (signInResult.ResultCode == SignInResultCode.Success)
        {
            authCookieManager.SetAuthCookies(
                Response.Cookies,
                signInResult.AuthToken!,
                signInResult.AuthTokenId!.Value,
                Request.IsHttps,
                _authTokenTtlSeconds
            );
            return Redirect(AppRoutes.Dashboard);
        }

        // Registration succeeded but auto sign-in failed — send to sign-in page
        return Redirect(AppRoutes.SignIn);
    }
}
