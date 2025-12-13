namespace Corely.IAM.Models;

public record DeregisterAccountResult(DeregisterAccountResultCode ResultCode, string Message);

public enum DeregisterAccountResultCode
{
    Success,
    AccountNotFoundError,
    UnauthorizedError,
}
