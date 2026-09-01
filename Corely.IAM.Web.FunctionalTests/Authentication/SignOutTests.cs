using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class SignOutTests : FunctionalTestBase
{
    [Fact]
    public async Task SignOut_RedirectsToSignIn()
    {
        await SignInSuccessfullyAsync();

        using var response = await Client.GetAsync(AppRoutes.SignOut);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SignOut_ClearsAuthCookies()
    {
        await SignInSuccessfullyAsync();

        using var response = await Client.GetAsync(AppRoutes.SignOut);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        Assert.False(HasAuthCookies);
        Assert.Null(CurrentAuthTokenId);
    }

    [Fact]
    public async Task SignOut_RevokesTheTokenRow()
    {
        await SignInSuccessfullyAsync();
        var tokenId = Guid.Parse(CurrentAuthTokenId!);

        using var response = await Client.GetAsync(AppRoutes.SignOut);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var row = await GetAuthTokenRowAsync(tokenId);
        Assert.NotNull(row);
        Assert.NotNull(row!.RevokedUtc);
    }

    [Fact]
    public async Task AfterSignOut_ProtectedPageRedirects()
    {
        await SignInSuccessfullyAsync();
        using (var signOut = await Client.GetAsync(AppRoutes.SignOut))
            Assert.Equal(HttpStatusCode.Redirect, signOut.StatusCode);

        using var response = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task SignOut_WithoutASession_StillRedirectsCleanly()
    {
        using var response = await Client.GetAsync(AppRoutes.SignOut);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }
}
