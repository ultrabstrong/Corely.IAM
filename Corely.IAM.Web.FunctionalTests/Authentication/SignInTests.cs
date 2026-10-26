using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.Web.Security;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

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
            c.Name == AuthCookieManager.AUTH_TOKEN_COOKIE
        );

        Assert.True(authCookie.HttpOnly);
        Assert.True(authCookie.Secure);
        Assert.Equal("lax", authCookie.SameSite, ignoreCase: true);
        Assert.Equal(
            "lax",
            Client
                .LastSetCookies.Last(c => c.Name == AuthCookieManager.AUTH_TOKEN_ID_COOKIE)
                .SameSite,
            ignoreCase: true
        );
        var refreshCookie = Client.LastSetCookies.Last(c =>
            c.Name == AuthCookieManager.REFRESH_TOKEN_COOKIE
        );
        Assert.True(refreshCookie.HttpOnly);
        Assert.True(refreshCookie.Secure);
        Assert.Equal("lax", refreshCookie.SameSite, ignoreCase: true);
    }

    [Fact]
    public async Task SignIn_CookieExpiryReflectsTheIdleTimeoutNotTokenTtl()
    {
        using var response = await SignInAsync();
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var authCookie = Client.LastSetCookies.Last(c =>
            c.Name == AuthCookieManager.AUTH_TOKEN_COOKIE
        );

        Assert.NotNull(authCookie.Expires);
        var expectedExpiry = Clock.GetUtcNow().AddSeconds(AuthSessionIdleTimeoutSeconds);
        Assert.True(
            (authCookie.Expires!.Value - expectedExpiry).Duration() < TimeSpan.FromMinutes(1),
            $"Cookie expiry {authCookie.Expires} should track the idle timeout ({expectedExpiry})."
        );
    }

    [Fact]
    public async Task SignIn_SetsARefreshCookie()
    {
        await SignInSuccessfullyAsync();

        Assert.NotNull(CurrentRefreshToken);
    }

    [Fact]
    public async Task SignIn_SetsDeviceIdCookie()
    {
        await SignInSuccessfullyAsync();

        Assert.True(Client.Cookies.Contains(AuthCookieManager.DEVICE_ID_COOKIE));
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
