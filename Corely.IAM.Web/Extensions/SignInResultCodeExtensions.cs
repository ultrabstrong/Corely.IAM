using Corely.IAM.Models;

namespace Corely.IAM.Web.Extensions;

internal static class SignInResultCodeExtensions
{
    extension(SignInResultCode resultCode)
    {
        public string SwitchFailureMessage() =>
            resultCode == SignInResultCode.TwoFactorRequiredError
                ? "Turn on two factor authentication on your profile to enter this account as a platform member."
                : "Failed to switch account.";
    }
}
