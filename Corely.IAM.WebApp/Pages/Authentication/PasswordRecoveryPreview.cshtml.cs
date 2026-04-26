using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.WebApp.Pages.Authentication;

public class PasswordRecoveryPreviewModel(IOptions<DemoFeaturesOptions> demoFeaturesOptions)
    : PageModel
{
    private readonly DemoFeaturesOptions _demoFeaturesOptions = demoFeaturesOptions.Value;

    public string? Email { get; private set; }

    public string? RecoveryToken { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string ResetPasswordUrl =>
        $"{WebAppRoutes.ResetPassword}?token={Uri.EscapeDataString(RecoveryToken ?? string.Empty)}";

    public IActionResult OnGet()
    {
        if (!_demoFeaturesOptions.EnablePasswordRecoveryPreview)
        {
            return NotFound();
        }

        Email = TempData.Peek("RecoveryPreviewEmail") as string;
        RecoveryToken = TempData.Peek("RecoveryPreviewToken") as string;

        if (string.IsNullOrWhiteSpace(RecoveryToken))
        {
            ErrorMessage =
                "No recovery preview is available yet. Request a password recovery first.";
        }

        return Page();
    }
}
