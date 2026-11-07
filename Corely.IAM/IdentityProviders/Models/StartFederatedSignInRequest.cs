using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Models;

public record StartFederatedSignInRequest(
    IdentityProviderType Type,
    FederatedSignInPurpose Purpose,
    string RedirectUri
);
