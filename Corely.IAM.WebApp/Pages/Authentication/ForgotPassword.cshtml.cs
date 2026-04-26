using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.WebApp.Pages.Authentication;

public class ForgotPasswordModel(
    IPasswordRecoveryService passwordRecoveryService,
    IOptions<DemoFeaturesOptions> demoFeaturesOptions
) : PageModel
{
    private readonly IPasswordRecoveryService _passwordRecoveryService = passwordRecoveryService;
    private readonly DemoFeaturesOptions _demoFeaturesOptions = demoFeaturesOptions.Value;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? InfoMessage { get; set; }

    public bool IsPreviewEnabled => _demoFeaturesOptions.EnablePasswordRecoveryPreview;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Email is required.";
            return Page();
        }

        var result = await _passwordRecoveryService.RequestPasswordRecoveryAsync(
            new RequestPasswordRecoveryRequest(Email.Trim())
        );

        if (result.ResultCode == RequestPasswordRecoveryResultCode.ValidationError)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        if (result.ResultCode == RequestPasswordRecoveryResultCode.UserNotFoundError)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        if (IsPreviewEnabled && !string.IsNullOrWhiteSpace(result.RecoveryToken))
        {
            TempData["RecoveryPreviewEmail"] = Email.Trim();
            TempData["RecoveryPreviewToken"] = result.RecoveryToken;
            return Redirect(WebAppRoutes.PasswordRecoveryPreview);
        }

        InfoMessage = "Password recovery requested successfully.";
        return Page();
    }
}
