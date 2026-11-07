using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Models;

internal record ExternalIdentity(
    IdentityProviderType Type,
    string Issuer,
    string Subject,
    string? Email,
    bool EmailVerified,
    DateTime? AuthTimeUtc = null
)
{
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
}
