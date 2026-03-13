namespace Corely.IAM.TotpAuths.Models;

public enum ConfirmTotpResultCode
{
    Success,
    NotFoundError,
    AlreadyEnabledError,
    InvalidCodeError,
    UnauthorizedError,
}

public record ConfirmTotpResult(ConfirmTotpResultCode ResultCode, string Message);
