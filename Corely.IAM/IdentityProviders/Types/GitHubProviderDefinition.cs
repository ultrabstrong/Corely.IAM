using System.Net.Http.Headers;
using System.Text.Json;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Types;

internal sealed class GitHubProviderDefinition : OAuth2ProviderDefinition
{
    public const string API_VERSION = "2022-11-28";
    public const string USER_AGENT = "Corely.IAM";
    public const string USER_URL = "https://api.github.com/user";
    public const string EMAILS_URL = "https://api.github.com/user/emails";

    public override IdentityProviderType Type => IdentityProviderType.GitHub;
    public override string DisplayName => "GitHub";
    public override string Issuer => "https://github.com";
    public override string AuthorizationEndpoint => "https://github.com/login/oauth/authorize";
    public override string TokenEndpoint => "https://github.com/login/oauth/access_token";
    public override IReadOnlyList<string> Scopes { get; } = [];

    public override async Task<ExternalIdentity?> ReadIdentityAsync(
        string accessToken,
        ResolvedIdentityProvider provider,
        HttpClient http,
        CancellationToken cancellationToken
    )
    {
        using var user = await GetJsonAsync(http, USER_URL, accessToken, cancellationToken);
        if (
            user is null
            || !user.RootElement.TryGetProperty("id", out var id)
            || id.ValueKind != JsonValueKind.Number
        )
            return null;

        using var emails = await GetJsonAsync(http, EMAILS_URL, accessToken, cancellationToken);
        var email =
            emails?.RootElement.ValueKind == JsonValueKind.Array
                ? emails
                    .RootElement.EnumerateArray()
                    .Where(e =>
                        e.TryGetProperty("primary", out var primary)
                        && primary.ValueKind == JsonValueKind.True
                        && e.TryGetProperty("verified", out var verified)
                        && verified.ValueKind == JsonValueKind.True
                    )
                    .Select(e =>
                        e.TryGetProperty("email", out var value) ? value.GetString() : null
                    )
                    .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e))
                : null;

        return new ExternalIdentity(Type, Issuer, id.GetRawText(), email, email is not null);
    }

    private static async Task<JsonDocument?> GetJsonAsync(
        HttpClient http,
        string url,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd(USER_AGENT);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", API_VERSION);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken
        );
    }
}
