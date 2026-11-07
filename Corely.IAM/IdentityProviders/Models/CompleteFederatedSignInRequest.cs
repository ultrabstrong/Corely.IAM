using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Models;

public sealed class CompleteFederatedSignInRequest(
    FederatedSignInCorrelation correlation,
    string redirectUri,
    string deviceId,
    string? code,
    string? state,
    string? issuer,
    string? error,
    Guid? accountId = null
)
{
    public IdentityProviderType Type => Correlation.Type;
    public FederatedSignInPurpose Purpose => Correlation.Purpose;
    public string RedirectUri { get; } = redirectUri;
    public string DeviceId { get; } = deviceId;
    public string? Error { get; } = error;
    public Guid? AccountId { get; } = accountId;

    internal FederatedSignInCorrelation Correlation { get; } = correlation;
    internal string? Code { get; } = code;
    internal string? State { get; } = state;
    internal string? Issuer { get; } = issuer;
}
