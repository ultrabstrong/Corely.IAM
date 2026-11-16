using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.Web;
using Corely.IAM.WebApp.EmailVerifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.WebApp.Pages.Authentication;

public class EmailVerificationPreviewModel(
    EmailVerificationPreviewStore store,
    IWebHostEnvironment environment
) : PageModel
{
    public EmailVerificationDelivery? Delivery { get; } = store.Last;

    public string ConfirmUrl =>
        $"{AppRoutes.ConfirmEmail}?token={Uri.EscapeDataString(Delivery?.Token ?? string.Empty)}";

    public IActionResult OnGet() => environment.IsDevelopment() ? Page() : NotFound();
}
