using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.Web.Security;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

/// <summary>
/// F5 / F6 - the sign-in seam. Cookie attributes are asserted from the raw Set-Cookie headers,
/// which is why no browser is needed: whether the app *sets* Secure and HttpOnly is observable
/// here, and whether a browser *enforces* them is not this suite's concern.
/// </summary>
public class SignInTests : FunctionalTestBase
{
    [Fact]
    public async Task SignIn_WithValidCredentials_RedirectsAndSetsAuthCookies()
    {
        using var response = await SignInAsync();

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(CurrentAuthToken);
        Assert.NotNull(CurrentAuthTokenId);
    }

    [Fact]
    public async Task SignIn_SetsAuthCookiesWithExpectedSecurityAttributes()
    {
        using var response = await SignInAsync();
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var authCookie = Client.LastSetCookies.Last(c =>
            c.Name == AuthenticationConstants.AUTH_TOKEN_COOKIE
        );

        Assert.True(authCookie.HttpOnly);
        Assert.True(authCookie.Secure);
        Assert.Equal("strict", authCookie.SameSite, ignoreCase: true);
    }

    [Fact]
    public async Task SignIn_CookieExpiryReflectsSessionTtlNotTokenTtl()
    {
        using var response = await SignInAsync();
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var authCookie = Client.LastSetCookies.Last(c =>
            c.Name == AuthenticationConstants.AUTH_TOKEN_COOKIE
        );

        Assert.NotNull(authCookie.Expires);
        var expectedExpiry = Clock.GetUtcNow().AddSeconds(AuthSessionTtlSeconds);
        Assert.True(
            (authCookie.Expires!.Value - expectedExpiry).Duration() < TimeSpan.FromMinutes(1),
            $"Cookie expiry {authCookie.Expires} should track the session TTL ({expectedExpiry})."
        );
    }

    [Fact]
    public async Task SignIn_IssuedTokenCarriesSessionStartedAtClaim()
    {
        await SignInSuccessfullyAsync();

        Assert.NotNull(TestJwt.GetSessionStartedAt(CurrentAuthToken!));
    }

    [Fact]
    public async Task SignIn_SetsDeviceIdCookie()
    {
        await SignInSuccessfullyAsync();

        Assert.True(Client.Cookies.Contains(AuthenticationConstants.DEVICE_ID_COOKIE));
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_SetsNoAuthCookies()
    {
        using var response = await SignInAsync(password: "Wrong!Pass123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task SignIn_WithUnknownUser_BehavesTheSameAsWrongPassword()
    {
        using var unknownUser = await SignInAsync(username: "nosuchuser");
        var unknownUserBody = await unknownUser.Content.ReadAsStringAsync();

        // Fresh client so the failed attempt above does not affect lockout state.
        await DisposeAsync();
        await InitializeAsync();

        using var wrongPassword = await SignInAsync(password: "Wrong!Pass123");
        var wrongPasswordBody = await wrongPassword.Content.ReadAsStringAsync();

        Assert.Equal(unknownUser.StatusCode, wrongPassword.StatusCode);
        Assert.Equal(ExtractErrorMessage(unknownUserBody), ExtractErrorMessage(wrongPasswordBody));
    }

    [Fact]
    public async Task SignIn_WithEmptyCredentials_IsRejected()
    {
        using var response = await SignInAsync(username: "", password: "");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(HasAuthCookies);
    }

    private static string ExtractErrorMessage(string html) =>
        html.Contains("Invalid username or password", StringComparison.OrdinalIgnoreCase)
            ? "invalid-credentials"
            : "other";
}
