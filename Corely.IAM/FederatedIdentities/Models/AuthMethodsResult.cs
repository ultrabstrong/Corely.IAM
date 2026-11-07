namespace Corely.IAM.FederatedIdentities.Models;

public enum AuthMethodsResultCode
{
    Success,
    UnauthorizedError,
}

public record AuthMethodsResult(
    AuthMethodsResultCode ResultCode,
    string Message,
    bool HasBasicAuth,
    IReadOnlyList<LinkedIdentity> LinkedIdentities
);
