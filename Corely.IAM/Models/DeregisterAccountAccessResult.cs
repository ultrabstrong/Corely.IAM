namespace Corely.IAM.Models;

public record DeregisterAccountAccessResult(
    DeregisterAccountAccessResultCode ResultCode,
    string Message
);

public enum DeregisterAccountAccessResultCode
{
    Success,
    AccountNotFoundError,
    UnauthorizedError,
    PlatformAccountError,
}
