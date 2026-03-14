namespace Corely.IAM.BasicAuths.Models;

public enum DeleteBasicAuthResultCode
{
    Success,
    NotFoundError,
    LastAuthMethodError,
    UnauthorizedError,
}

internal record DeleteBasicAuthResult(DeleteBasicAuthResultCode ResultCode, string Message);
