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

    public const string PROMPT_PARAMETER = "prompt";
    public const string SELECT_ACCOUNT_PROMPT = "select_account";

    public virtual bool UsesPkce => true;

    public virtual bool OffersAccountPicker => false;

    public virtual IReadOnlyDictionary<string, string> AuthorizationParameters(
        FederatedSignInPurpose purpose
    ) =>
        purpose == FederatedSignInPurpose.Link && OffersAccountPicker
            ? new Dictionary<string, string> { [PROMPT_PARAMETER] = SELECT_ACCOUNT_PROMPT }
            : new Dictionary<string, string>();
}
