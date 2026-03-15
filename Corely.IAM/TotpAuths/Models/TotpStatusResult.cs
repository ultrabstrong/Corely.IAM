namespace Corely.IAM.TotpAuths.Models;

public enum TotpStatusResultCode
{
    Success,
    UnauthorizedError,
}

public record TotpStatusResult(
    TotpStatusResultCode ResultCode,
    string Message,
    bool IsEnabled,
    int RemainingRecoveryCodes
);
