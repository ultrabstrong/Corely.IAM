using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class RegisterModel(
    IRegistrationService registrationService,
    IAuthenticationService authenticationService,
    IAuthCookieManager authCookieManager,
    IOptions<SecurityOptions> securityOptions,
    IHumanVerifier humanVerifier,
    IOptions<IAMWebOptions> iamWebOptions
) : PageModel
{
    public const string HUMAN_CHECK_ACTION = "register";

    public string? GoogleClientId { get; } = securityOptions.Value.GoogleClientId;

    public string? GoogleCallbackUrl { get; set; }

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
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(iamWebOptions.Value.HomePath);
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
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        var humanCheck = await humanVerifier.VerifyAsync(
            HttpContext,
            HUMAN_CHECK_ACTION,
            HttpContext.RequestAborted
        );
        if (!humanCheck.Succeeded)
        {
            ErrorMessage = humanCheck.RefusalMessage;
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
                signInResult.RefreshToken!,
                signInResult.RefreshExpiresUtc!.Value,
                Request.IsHttps
            );
            return Redirect(iamWebOptions.Value.HomePath);
        }

        return Redirect(AppRoutes.SignIn);
    }
}
