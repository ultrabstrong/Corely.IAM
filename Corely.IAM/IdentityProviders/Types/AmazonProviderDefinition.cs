using System.Net.Http.Headers;
using System.Text.Json;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Types;

internal sealed class AmazonProviderDefinition : OAuth2ProviderDefinition
{
    public const string PROFILE_URL = "https://api.amazon.com/user/profile";

    public override IdentityProviderType Type => IdentityProviderType.Amazon;
    public override string DisplayName => "Amazon";
    public override EmailVerificationEvidence EmailEvidence =>
        EmailVerificationEvidence.AccountPolicy;
    public override string Issuer => "https://www.amazon.com";
    public override string AuthorizationEndpoint => "https://www.amazon.com/ap/oa";
    public override string TokenEndpoint => "https://api.amazon.com/auth/o2/token";
    public override IReadOnlyList<string> Scopes { get; } = ["profile"];

    public override async Task<ExternalIdentity?> ReadIdentityAsync(
        string accessToken,
        ResolvedIdentityProvider provider,
        HttpClient http,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, PROFILE_URL);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var profile = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken
        );
        if (
            !profile.RootElement.TryGetProperty("user_id", out var userId)
            || string.IsNullOrWhiteSpace(userId.GetString())
        )
            return null;

        var email = profile.RootElement.TryGetProperty("email", out var value)
            ? value.GetString()
            : null;
        return new ExternalIdentity(
            Type,
            Issuer,
            userId.GetString()!,
            email,
            !string.IsNullOrWhiteSpace(email)
        );
    }
}
