using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.Web.Pages.Authentication;

public class ConfirmEmailModel(IEmailVerificationService emailVerificationService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public VerifyEmailResult? Result { get; private set; }

    public bool Confirmed => Result?.ResultCode == VerifyEmailResultCode.Success;

    // Confirms on GET, from a link another site (an inbox) opens. The token is the authority, not
    // the cookie: it confirms only the address it was sent to and signs nobody in.
    public async Task<IActionResult> OnGetAsync()
    {
        // The address carries the token, so no link on the page may pass it on as a referrer.
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Result = string.IsNullOrWhiteSpace(Token)
            ? new VerifyEmailResult(
                VerifyEmailResultCode.EmailVerificationNotFoundError,
                "This link is not valid"
            )
            : await emailVerificationService.VerifyEmailAsync(new VerifyEmailRequest(Token));
        return Page();
    }
}
