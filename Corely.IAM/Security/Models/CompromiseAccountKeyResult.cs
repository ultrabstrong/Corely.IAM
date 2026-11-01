namespace Corely.IAM.Security.Models;

public enum CompromiseAccountKeyResultCode
{
    Success,
    UnauthorizedError,
    NotFoundError,
    ValidationError,
    CurrentVersionError,
    AlreadyCompromisedError,
    BasicAuthNotFoundError,
    InvalidCurrentPasswordError,
    UserLockedError,
    TotpRequiredError,
    InvalidTotpCodeError,
    GoogleSignInRequiredError,
    InvalidGoogleSignInError,
    NoReauthenticationMethodError,
}

public record CompromiseAccountKeyResult(CompromiseAccountKeyResultCode ResultCode, string Message);
