namespace Corely.IAM.BasicAuths.Models;

public enum UpdateBasicAuthResultCode
{
    Success,
    BasicAuthNotFoundError,
    UnauthorizedError,
}

internal record UpdateBasicAuthResult(UpdateBasicAuthResultCode ResultCode, string? Message);
