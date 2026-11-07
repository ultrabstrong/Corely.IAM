using System.Text;
using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Models;

public sealed record FederatedSignInCorrelation(
    IdentityProviderType Type,
    FederatedSignInPurpose Purpose,
    string State,
    string CodeVerifier,
    string? Nonce,
    string Issuer,
    DateTime StartedUtc
)
{
    public const int MAX_AGE_SECONDS = 600;

    internal bool HasExpired(DateTime utcNow) => StartedUtc.AddSeconds(MAX_AGE_SECONDS) <= utcNow;

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Type = {Type}, Purpose = {Purpose}");
        return true;
    }
}
