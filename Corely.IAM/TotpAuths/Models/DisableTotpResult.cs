namespace Corely.IAM.TotpAuths.Models;

public enum DisableTotpResultCode
{
    Success,
    NotFoundError,
    InvalidCodeError,
    UnauthorizedError,
}

public record DisableTotpResult(DisableTotpResultCode ResultCode, string Message);
