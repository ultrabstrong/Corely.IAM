using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.WebApp.Pages.Authentication;

public class ResetPasswordModel(IPasswordRecoveryService passwordRecoveryService) : PageModel
{
    private readonly IPasswordRecoveryService _passwordRecoveryService = passwordRecoveryService;

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public string? InfoMessage { get; private set; }

    public bool ResetSucceeded { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return Page();
        }

        var result = await _passwordRecoveryService.ValidatePasswordRecoveryTokenAsync(
            new ValidatePasswordRecoveryTokenRequest(Token)
        );

        InfoMessage =
            result.ResultCode == ValidatePasswordRecoveryTokenResultCode.Success
                ? "Recovery token is valid. Choose a new password."
                : null;
        ErrorMessage =
            result.ResultCode == ValidatePasswordRecoveryTokenResultCode.Success
                ? null
                : result.Message;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = "Recovery token is required.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Password is required.";
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        var result = await _passwordRecoveryService.ResetPasswordWithRecoveryAsync(
            new ResetPasswordWithRecoveryRequest(Token, Password)
        );

        if (result.ResultCode != ResetPasswordWithRecoveryResultCode.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        ResetSucceeded = true;
        InfoMessage = "Password reset complete. You can now sign in with the new password.";
        return Page();
    }
}
