using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.Web.Pages.Authentication;

public class ConfirmEmailModel(IEmailVerificationService emailVerificationService) : PageModel
{
    public const string INVALID_LINK_MESSAGE = "This link is not valid";

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public string? ErrorMessage { get; private set; }

    public string? ConfirmedEmail { get; private set; }

    public bool Rejected { get; private set; }

    public bool Removed { get; private set; }

    public bool Asking => ErrorMessage is null && ConfirmedEmail is null && !Rejected;

    // Opening the link changes nothing, so a mail scanner that opens it first confirms nothing and
    // another site cannot act through it. The reader chooses on the page, which posts.
    public IActionResult OnGet()
    {
        NoReferrer();
        if (string.IsNullOrWhiteSpace(Token))
            ErrorMessage = INVALID_LINK_MESSAGE;
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        NoReferrer();
        var result = await emailVerificationService.VerifyEmailAsync(
            new VerifyEmailRequest(Token ?? string.Empty)
        );
        if (result.ResultCode == VerifyEmailResultCode.Success)
            ConfirmedEmail = result.Email;
        else
            ErrorMessage = result.Message;
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync()
    {
        NoReferrer();
        var result = await emailVerificationService.RejectEmailVerificationAsync(
            new VerifyEmailRequest(Token ?? string.Empty)
        );
        if (result.ResultCode == RejectEmailVerificationResultCode.Success)
        {
            Removed = result.Removed;
            Rejected = true;
        }
        else
            ErrorMessage = result.Message;
        return Page();
    }

    private void NoReferrer() => Response.Headers["Referrer-Policy"] = "no-referrer";
}
