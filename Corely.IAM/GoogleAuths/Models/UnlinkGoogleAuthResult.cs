namespace Corely.IAM.GoogleAuths.Models;

public enum UnlinkGoogleAuthResultCode
{
    Success,
    NotLinkedError,
    LastAuthMethodError,
    UnauthorizedError,
}

public record UnlinkGoogleAuthResult(UnlinkGoogleAuthResultCode ResultCode, string Message);
