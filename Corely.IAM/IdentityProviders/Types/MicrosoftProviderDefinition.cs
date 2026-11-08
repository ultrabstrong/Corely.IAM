using System.IdentityModel.Tokens.Jwt;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Types;

internal sealed class MicrosoftProviderDefinition : OpenIdConnectProviderDefinition
{
    public const string TENANT_DEFAULT = "common";
    public const string PERSONAL_ACCOUNTS_TENANT_ID = "9188040d-6c67-4c5b-b112-36a304b66dad";
    public const string TENANT_ID_PLACEHOLDER = "{tenantid}";

    public override IdentityProviderType Type => IdentityProviderType.Microsoft;
    public override string DisplayName => "Microsoft";
    public override bool ForcesFreshSignInForReauthentication => true;
    public override bool OffersAccountPicker => true;

    public override IReadOnlyList<IdentityProviderCredentialKind> CredentialKinds { get; } =
    [IdentityProviderCredentialKind.Certificate, IdentityProviderCredentialKind.ClientSecret];

    public override string DiscoveryUrl(IdentityProviderSettings settings) =>
        $"https://login.microsoftonline.com/{TenantOf(settings)}/v2.0/.well-known/openid-configuration";

    public override IReadOnlyDictionary<string, string> AuthorizationParameters(
        FederatedSignInPurpose purpose
    ) =>
        purpose == FederatedSignInPurpose.Reauthenticate
            ? new Dictionary<string, string> { [PROMPT_PARAMETER] = "login", ["max_age"] = "0" }
            : base.AuthorizationParameters(purpose);

    public override bool IsValidIssuer(
        string issuer,
        JwtSecurityToken token,
        string metadataIssuer
    ) =>
        TenantIdOf(token) is { } tenantId
        && issuer == metadataIssuer.Replace(TENANT_ID_PLACEHOLDER, tenantId);

    public override bool IsValidSigningKeyIssuer(string? keyIssuer, JwtSecurityToken token) =>
        keyIssuer is null
        || (
            TenantIdOf(token) is { } tenantId
            && keyIssuer.Replace(TENANT_ID_PLACEHOLDER, tenantId) == token.Issuer
        );

    public override bool IsEmailVerified(JwtSecurityToken token) =>
        TenantIdOf(token) == PERSONAL_ACCOUNTS_TENANT_ID
        || token.Claims.FirstOrDefault(c => c.Type == "xms_edov")?.Value is "true" or "True" or "1";

    private static string TenantOf(IdentityProviderSettings settings) =>
        string.IsNullOrWhiteSpace(settings.Tenant) ? TENANT_DEFAULT : settings.Tenant;

    private static string? TenantIdOf(JwtSecurityToken token) =>
        token.Claims.FirstOrDefault(c => c.Type == "tid")?.Value is { } tid
        && Guid.TryParse(tid, out var tenantId)
            ? tenantId.ToString()
            : null;
}
