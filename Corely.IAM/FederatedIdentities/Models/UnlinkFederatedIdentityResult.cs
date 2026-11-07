namespace Corely.IAM.FederatedIdentities.Models;

public enum UnlinkFederatedIdentityResultCode
{
    Success,
    NotLinkedError,
    LastAuthMethodError,
    UnauthorizedError,
}

public record UnlinkFederatedIdentityResult(
    UnlinkFederatedIdentityResultCode ResultCode,
    string Message
);
