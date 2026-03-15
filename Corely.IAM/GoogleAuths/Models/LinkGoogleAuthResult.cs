namespace Corely.IAM.GoogleAuths.Models;

public enum LinkGoogleAuthResultCode
{
    Success,
    InvalidGoogleTokenError,
    AlreadyLinkedError,
    GoogleAccountInUseError,
    UnauthorizedError,
}

public record LinkGoogleAuthResult(LinkGoogleAuthResultCode ResultCode, string Message);
