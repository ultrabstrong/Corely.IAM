using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

/// <summary>
/// F4 - revocation takes effect on the next request. Revoking out of band stands in for a
/// sign-out-everywhere triggered elsewhere: another device, an admin action, a password reset.
/// </summary>
public class RevocationTests : FunctionalTestBase
{
    [Fact]
    public async Task BeforeRevocation_ProtectedPageIsServed()
    {
        await SignInSuccessfullyAsync();

        using var response = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AfterRevocation_RedirectsToSignIn()
    {
        await SignInSuccessfullyAsync();
        using (var before = await Client.GetAsync(AppRoutes.Dashboard))
            Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        await RevokeCurrentTokenOutOfBandAsync();

        using var after = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.Redirect, after.StatusCode);
        Assert.Contains(AppRoutes.SignIn, after.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AfterRevocation_ReturnUrlIsPreserved()
    {
        await SignInSuccessfullyAsync();
        await RevokeCurrentTokenOutOfBandAsync();

        using var response = await Client.GetAsync(AppRoutes.Profile);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.Contains("ReturnUrl", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AfterRevocation_AuthCookiesAreCleared()
    {
        await SignInSuccessfullyAsync();
        await RevokeCurrentTokenOutOfBandAsync();

        using var response = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task RevokingOneSession_DoesNotAffectASibling()
    {
        await SignInSuccessfullyAsync();

        // A second client is a second device: separate cookie jar, separate token row.
        using var sibling = new TestClient(Factory.CreateTestClient());
        using var siblingSignIn = await sibling.PostFormAsync(
            AppRoutes.SignIn,
            new Dictionary<string, string>
            {
                ["Username"] = SeedData.OwnerUsername,
                ["Password"] = SeedData.OwnerPassword,
            }
        );
        Assert.Equal(HttpStatusCode.Redirect, siblingSignIn.StatusCode);

        await RevokeCurrentTokenOutOfBandAsync();

        using var revoked = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.Redirect, revoked.StatusCode);

        using var stillValid = await sibling.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.OK, stillValid.StatusCode);
    }
}
