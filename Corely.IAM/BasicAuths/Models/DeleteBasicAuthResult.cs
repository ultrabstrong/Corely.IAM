namespace Corely.IAM.BasicAuths.Models;

internal enum DeleteBasicAuthResultCode
{
    Success,
    NotFoundError,
    LastAuthMethodError,
    UnauthorizedError,
}

internal record DeleteBasicAuthResult(DeleteBasicAuthResultCode ResultCode, string Message);
