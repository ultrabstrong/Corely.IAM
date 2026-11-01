namespace Corely.IAM.Security.Models;

internal enum ReauthenticationResultCode
{
    Success,
    BasicAuthNotFoundError,
    InvalidCurrentPasswordError,
    UserLockedError,
    TotpRequiredError,
    InvalidTotpCodeError,
    GoogleSignInRequiredError,
    InvalidGoogleSignInError,
    NoReauthenticationMethodError,
}

internal record ReauthenticationResult(ReauthenticationResultCode ResultCode, string Message)
{
    public static ReauthenticationResult Succeeded { get; } =
        new(ReauthenticationResultCode.Success, string.Empty);
}
