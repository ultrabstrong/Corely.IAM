using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class SessionCapTests : FunctionalTestBase
{
    protected override int AuthTokenTtlSeconds => 30;
    protected override int AuthSessionTtlSeconds => 300;

    [Fact]
    public async Task JustBeforeSessionExpiry_StillRenews()
    {
        await SignInSuccessfullyAsync();
        Clock.AdvanceSeconds(AuthSessionTtlSeconds - 20);

        using var response = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AfterSessionExpiry_RedirectsToSignIn()
    {
        await SignInSuccessfullyAsync();
        Clock.AdvanceSeconds(AuthSessionTtlSeconds + 20);

        using var response = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AfterSessionExpiry_ClearsAuthCookies()
    {
        await SignInSuccessfullyAsync();
        Clock.AdvanceSeconds(AuthSessionTtlSeconds + 20);

        using var response = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task RepeatedRenewals_DoNotExtendTheSessionPastItsOrigin()
    {
        await SignInSuccessfullyAsync();
        var sessionStartedAt = TestJwt.GetSessionStartedAt(CurrentAuthToken!);

        for (var i = 0; i < 5; i++)
        {
            Clock.AdvanceSeconds(AuthTokenTtlSeconds + 5);
            using var response = await Client.GetAsync(AppRoutes.Dashboard);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(sessionStartedAt, TestJwt.GetSessionStartedAt(CurrentAuthToken!));
        }

        Clock.AdvanceSeconds(AuthSessionTtlSeconds);
        using var expired = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.Redirect, expired.StatusCode);
    }
}
