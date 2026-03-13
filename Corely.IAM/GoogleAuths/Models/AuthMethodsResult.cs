namespace Corely.IAM.GoogleAuths.Models;

public enum AuthMethodsResultCode
{
    Success,
    UnauthorizedError,
}

public record AuthMethodsResult(
    AuthMethodsResultCode ResultCode,
    string Message,
    bool HasBasicAuth,
    bool HasGoogleAuth,
    string? GoogleEmail
);
