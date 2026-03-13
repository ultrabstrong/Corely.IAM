namespace Corely.IAM.TotpAuths.Models;

public enum RegenerateTotpRecoveryCodesResultCode
{
    Success,
    NotFoundError,
    NotEnabledError,
    UnauthorizedError,
}

public record RegenerateTotpRecoveryCodesResult(
    RegenerateTotpRecoveryCodesResultCode ResultCode,
    string Message,
    string[]? RecoveryCodes
);
