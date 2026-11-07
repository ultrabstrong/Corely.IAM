using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Types;

internal abstract class IdentityProviderDefinition
{
    public abstract IdentityProviderType Type { get; }
    public abstract string DisplayName { get; }
    public abstract FederationProtocol Protocol { get; }
    public abstract EmailVerificationEvidence EmailEvidence { get; }
    public abstract IReadOnlyList<string> Scopes { get; }

    public virtual IReadOnlyList<IdentityProviderCredentialKind> CredentialKinds { get; } =
    [IdentityProviderCredentialKind.ClientSecret];

    public virtual bool UsesPkce => true;

    public virtual IReadOnlyDictionary<string, string> AuthorizationParameters(
        FederatedSignInPurpose purpose
    ) => new Dictionary<string, string>();
}
