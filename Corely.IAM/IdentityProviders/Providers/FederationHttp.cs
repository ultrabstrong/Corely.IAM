namespace Corely.IAM.IdentityProviders.Providers;

internal sealed class FederationHttp(HttpClient client)
{
    public const int TIMEOUT_SECONDS = 30;

    public HttpClient Client { get; } = client;

    public static FederationHttp Create() =>
        new(new HttpClient { Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) });
}
