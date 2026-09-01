using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class TokenRenewalTests : FunctionalTestBase
{
    protected override int AuthTokenTtlSeconds => 60;
    protected override int AuthSessionTtlSeconds => 3600;

    [Fact]
    public async Task IdlePastTokenTtl_StillServesProtectedPage()
    {
        await SignInSuccessfullyAsync();
        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);

        using var response = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task IdlePastTokenTtl_RotatesJti()
    {
        await SignInSuccessfullyAsync();
        var before = TestJwt.GetJti(CurrentAuthToken!);

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotEqual(before, TestJwt.GetJti(CurrentAuthToken!));
    }

    [Fact]
    public async Task IdlePastTokenTtl_PreservesSessionStartedAt()
    {
        await SignInSuccessfullyAsync();
        var before = TestJwt.GetSessionStartedAt(CurrentAuthToken!);
        Assert.NotNull(before);

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(before, TestJwt.GetSessionStartedAt(CurrentAuthToken!));
    }

    [Fact]
    public async Task Renewal_RevokesThePreviousToken()
    {
        await SignInSuccessfullyAsync();
        var previousTokenId = Guid.Parse(CurrentAuthTokenId!);

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var previousRow = await GetAuthTokenRowAsync(previousTokenId);
        Assert.NotNull(previousRow);
        Assert.NotNull(previousRow!.RevokedUtc);
    }

    [Fact]
    public async Task Renewal_IssuesADifferentTokenId()
    {
        await SignInSuccessfullyAsync();
        var previousTokenId = CurrentAuthTokenId;

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotEqual(previousTokenId, CurrentAuthTokenId);
    }

    [Fact]
    public async Task RequestWithinTokenTtl_DoesNotRotateTheToken()
    {
        await SignInSuccessfullyAsync();
        var before = TestJwt.GetJti(CurrentAuthToken!);

        Clock.AdvanceSeconds(AuthTokenTtlSeconds / 2);
        using var response = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(before, TestJwt.GetJti(CurrentAuthToken!));
    }

    [Fact]
    public async Task Renewal_ClampsExpiryToTheSessionBound()
    {
        await SignInSuccessfullyAsync();
        var sessionStartedAt = TestJwt.GetSessionStartedAt(CurrentAuthToken!)!.Value;
        var sessionExpiresUtc = DateTimeOffset
            .FromUnixTimeSeconds(sessionStartedAt)
            .AddSeconds(AuthSessionTtlSeconds)
            .UtcDateTime;

        Clock.AdvanceSeconds(AuthSessionTtlSeconds - (AuthTokenTtlSeconds / 2));
        using var response = await Client.GetAsync(AppRoutes.Dashboard);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var expiresUtc = TestJwt.GetExpiresUtc(CurrentAuthToken!);
        Assert.True(
            expiresUtc <= sessionExpiresUtc.AddSeconds(1),
            $"Token expiry {expiresUtc:O} must not exceed the session bound {sessionExpiresUtc:O}."
        );
    }

    [Fact]
    public async Task RevokedToken_IsNotRenewed()
    {
        await SignInSuccessfullyAsync();
        await RevokeCurrentTokenOutOfBandAsync();

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(HasAuthCookies);
    }
}
