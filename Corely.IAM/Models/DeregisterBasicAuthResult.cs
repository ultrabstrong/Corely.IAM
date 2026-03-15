namespace Corely.IAM.Models;

public enum DeregisterBasicAuthResultCode
{
    Success,
    NotFoundError,
    LastAuthMethodError,
    UnauthorizedError,
}

public record DeregisterBasicAuthResult(DeregisterBasicAuthResultCode ResultCode, string Message);
