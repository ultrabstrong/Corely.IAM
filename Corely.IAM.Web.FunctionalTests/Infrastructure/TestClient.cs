using System.Text.RegularExpressions;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

/// <summary>
/// Thin HTTP client with an explicit cookie jar and antiforgery handling, so tests read as a
/// sequence of requests rather than as plumbing.
/// </summary>
public sealed partial class TestClient(HttpClient client) : IDisposable
{
    private const string ANTIFORGERY_FIELD = "__RequestVerificationToken";

    public CookieJar Cookies { get; } = new();

    /// <summary>Set-Cookie headers from the most recent response, parsed.</summary>
    public IReadOnlyList<SentCookie> LastSetCookies { get; private set; } = [];

    public async Task<HttpResponseMessage> GetAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendAsync(request);
    }

    /// <summary>
    /// Posts a form, fetching an antiforgery token from the target page unless one is supplied.
    /// Pass <paramref name="antiforgeryToken"/> for pages whose GET consumes single-use state -
    /// the MFA challenge reads its token from TempData, so re-fetching the page would destroy it.
    /// </summary>
    public async Task<HttpResponseMessage> PostFormAsync(
        string path,
        Dictionary<string, string> fields,
        string? antiforgeryToken = null
    )
    {
        var token = antiforgeryToken ?? await GetAntiforgeryTokenAsync(path);
        if (token is not null)
            fields[ANTIFORGERY_FIELD] = token;

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(fields),
        };
        return await SendAsync(request);
    }

    /// <summary>
    /// Posts without an antiforgery token, for tests that assert the guard actually fires.
    /// </summary>
    public async Task<HttpResponseMessage> PostFormWithoutAntiforgeryAsync(
        string path,
        Dictionary<string, string> fields
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(fields),
        };
        return await SendAsync(request);
    }

    public async Task<string?> GetAntiforgeryTokenAsync(string path)
    {
        using var response = await GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        var match = AntiforgeryRegex().Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var cookieHeader = Cookies.ToHeaderValue();
        if (cookieHeader is not null)
            request.Headers.Add("Cookie", cookieHeader);

        var response = await client.SendAsync(request);
        LastSetCookies = Cookies.Apply(response.Headers);
        return response;
    }

    public void Dispose() => client.Dispose();

    [GeneratedRegex(
        """name="__RequestVerificationToken"[^>]*value="([^"]+)""",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex AntiforgeryRegex();
}
