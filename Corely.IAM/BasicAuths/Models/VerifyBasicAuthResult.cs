namespace Corely.IAM.BasicAuths.Models;

public record VerifyBasicAuthResult(
    VerifyBasicAuthResultCode ResultCode,
    string Message,
    bool IsValid
);

public enum VerifyBasicAuthResultCode
{
    Success,
    UserNotFoundError,
    UnauthorizedError,
}
