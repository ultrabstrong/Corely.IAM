using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.Web.Security;
using Corely.IAM.WebApp;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class UnauthenticatedAccessTests : FunctionalTestBase
{
    [Fact]
    public async Task AnonymousRequestToProtectedPage_RedirectsToSignIn()
    {
        using var response = await Client.GetAsync(WebAppRoutes.Home);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AnonymousRequestToProtectedPage_PreservesReturnUrl()
    {
        using var response = await Client.GetAsync(WebAppRoutes.Profile);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.Contains("ReturnUrl", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnonymousRequestToAuditPage_RedirectsToSignIn()
    {
        using var response = await Client.GetAsync(WebAppRoutes.Audit(AccountId));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AnonymousRequestToPlatformSettings_RedirectsToSignIn()
    {
        using var response = await Client.GetAsync(WebAppRoutes.PlatformSettings);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
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

        using var corrupt = new TestClient(Factory.CreateTestClient());
        corrupt.Cookies.Apply(BuildCookieHeader("not.a.jwt"));

        using var response = await corrupt.GetAsync(WebAppRoutes.Home);

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
        message.Headers.Add("Set-Cookie", $"{AuthCookieManager.AUTH_TOKEN_COOKIE}={token}; path=/");
        return message.Headers;
    }
}
