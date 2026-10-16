namespace Corely.IAM.BasicAuths.Models;

internal record VerifyBasicAuthResult(
    VerifyBasicAuthResultCode ResultCode,
    string Message,
    bool IsValid
);

internal enum VerifyBasicAuthResultCode
{
    Success,
    UserNotFoundError,
    UnauthorizedError,
}
