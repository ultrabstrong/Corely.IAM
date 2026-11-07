using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Types;

internal sealed class FacebookProviderDefinition : OAuth2ProviderDefinition
{
    public const string GRAPH_API_VERSION = "v27.0";
    public const string GRAPH_URL = "https://graph.facebook.com/" + GRAPH_API_VERSION;

    public override IdentityProviderType Type => IdentityProviderType.Facebook;
    public override string DisplayName => "Facebook";
    public override EmailVerificationEvidence EmailEvidence =>
        EmailVerificationEvidence.AccountPolicy;
    public override bool UsesPkce => false;
    public override string Issuer => "https://www.facebook.com";
    public override string AuthorizationEndpoint =>
        $"https://www.facebook.com/{GRAPH_API_VERSION}/dialog/oauth";
    public override string TokenEndpoint => $"{GRAPH_URL}/oauth/access_token";
    public override IReadOnlyList<string> Scopes { get; } = ["email", "public_profile"];

    public override HttpRequestMessage TokenRequest(
        ResolvedIdentityProvider provider,
        string code,
        string redirectUri,
        string? codeVerifier
    ) =>
        new(
            HttpMethod.Get,
            $"{TokenEndpoint}?client_id={Uri.EscapeDataString(provider.Settings.ClientId)}"
                + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
                + $"&client_secret={Uri.EscapeDataString(provider.ClientSecret ?? string.Empty)}"
                + $"&code={Uri.EscapeDataString(code)}"
        );

    public override async Task<ExternalIdentity?> ReadIdentityAsync(
        string accessToken,
        ResolvedIdentityProvider provider,
        HttpClient http,
        CancellationToken cancellationToken
    )
    {
        var secret = provider.ClientSecret ?? string.Empty;
        var appToken = $"{provider.Settings.ClientId}|{secret}";
        using var debug = await GetJsonAsync(
            http,
            $"{GRAPH_URL}/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appToken)}",
            null,
            cancellationToken
        );
        if (
            debug is null
            || !debug.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("is_valid", out var isValid)
            || isValid.ValueKind != JsonValueKind.True
            || !data.TryGetProperty("app_id", out var appId)
            || appId.GetString() != provider.Settings.ClientId
            || !data.TryGetProperty("user_id", out var userId)
        )
            return null;

        using var me = await GetJsonAsync(
            http,
            $"{GRAPH_URL}/me?fields=id,email&appsecret_proof={AppSecretProof(accessToken, secret)}",
            accessToken,
            cancellationToken
        );
        if (
            me is null
            || !me.RootElement.TryGetProperty("id", out var id)
            || id.GetString() != userId.GetString()
        )
            return null;

        var email = me.RootElement.TryGetProperty("email", out var value)
            ? value.GetString()
            : null;
        return new ExternalIdentity(
            Type,
            Issuer,
            id.GetString()!,
            email,
            !string.IsNullOrWhiteSpace(email)
        );
    }

    internal static string AppSecretProof(string accessToken, string appSecret) =>
        Convert
            .ToHexString(
                HMACSHA256.HashData(
                    Encoding.UTF8.GetBytes(appSecret),
                    Encoding.UTF8.GetBytes(accessToken)
                )
            )
            .ToLowerInvariant();

    private static async Task<JsonDocument?> GetJsonAsync(
        HttpClient http,
        string url,
        string? accessToken,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken
        );
    }
}
