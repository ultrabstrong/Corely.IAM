namespace Corely.IAM.FederatedIdentities.Models;

internal enum LinkFederatedIdentityResultCode
{
    Success,
    AlreadyLinkedError,
    IdentityInUseError,
    UnauthorizedError,
}

internal record LinkFederatedIdentityResult(
    LinkFederatedIdentityResultCode ResultCode,
    string Message
);
