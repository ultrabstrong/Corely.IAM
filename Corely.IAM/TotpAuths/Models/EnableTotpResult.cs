namespace Corely.IAM.TotpAuths.Models;

public enum EnableTotpResultCode
{
    Success,
    AlreadyEnabledError,
    UnauthorizedError,
}

public record EnableTotpResult(
    EnableTotpResultCode ResultCode,
    string Message,
    string? Secret,
    string? SetupUri,
    string[]? RecoveryCodes
);
