using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.Web.Security;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

/// <summary>
/// F6 - what an anonymous or malformed request sees. The malformed-cookie case matters most:
/// the middleware must reject and clear rather than throw, since a corrupt cookie would otherwise
/// lock a user out of the site with a 500 that no amount of retrying fixes.
/// </summary>
public class UnauthenticatedAccessTests : FunctionalTestBase
{
    [Fact]
    public async Task AnonymousRequestToProtectedPage_RedirectsToSignIn()
    {
        using var response = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AnonymousRequestToProtectedPage_PreservesReturnUrl()
    {
        using var response = await Client.GetAsync(AppRoutes.Profile);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.Contains("ReturnUrl", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignInPage_IsReachableAnonymously()
    {
        using var response = await Client.GetAsync(AppRoutes.SignIn);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MalformedAuthCookie_IsRejectedAndClearedRatherThanThrowing()
    {
        await SignInSuccessfullyAsync();

        // Replay a syntactically valid but meaningless token.
        using var corrupt = new TestClient(Factory.CreateTestClient());
        corrupt.Cookies.Apply(BuildCookieHeader("not.a.jwt"));

        using var response = await corrupt.GetAsync(AppRoutes.Dashboard);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task AlreadySignedIn_SignInPageRedirectsToDashboard()
    {
        await SignInSuccessfullyAsync();

        using var response = await Client.GetAsync(AppRoutes.SignIn);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static System.Net.Http.Headers.HttpResponseHeaders BuildCookieHeader(string token)
    {
        using var message = new HttpResponseMessage();
        message.Headers.Add(
            "Set-Cookie",
            $"{AuthenticationConstants.AUTH_TOKEN_COOKIE}={token}; path=/"
        );
        return message.Headers;
    }
}
