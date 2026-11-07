using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.Models;

namespace Corely.IAM.IdentityProviders.Models;

public enum CompleteFederatedSignInResultCode
{
    Success,
    MfaRequiredChallenge,
    ProviderDeclinedError,
    InvalidStateError,
    ExpiredError,
    IssuerMismatchError,
    ProviderNotAvailableError,
    InvalidResponseError,
    NotLinkedError,
    EmailNotVerifiedError,
    NoEmailError,
    EmailInUseError,
    IdentityInUseError,
    AlreadyLinkedError,
    NotLinkedToUserError,
    UnauthorizedError,
    UserLockedError,
    SignInError,
}

public record CompleteFederatedSignInResult(
    CompleteFederatedSignInResultCode ResultCode,
    string Message,
    IdentityProviderType Type,
    FederatedSignInPurpose Purpose,
    SignInResult? SignIn = null,
    bool CreatedUser = false
);
