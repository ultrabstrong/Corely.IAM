using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.WebApp;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class TokenRenewalTests : FunctionalTestBase
{
    protected override int AuthTokenTtlSeconds => 300;
    protected override int AuthSessionTtlSeconds => 3600;

    [Fact]
    public async Task IdlePastTokenTtl_StillServesProtectedPage()
    {
        await SignInSuccessfullyAsync();
        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);

        using var response = await Client.GetAsync(WebAppRoutes.Home);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task IdlePastTokenTtl_RotatesJti()
    {
        await SignInSuccessfullyAsync();
        var before = TestJwt.GetJti(CurrentAuthToken!);

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(WebAppRoutes.Home);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotEqual(before, TestJwt.GetJti(CurrentAuthToken!));
    }

    [Fact]
    public async Task IdlePastTokenTtl_KeepsTheSession()
    {
        await SignInSuccessfullyAsync();
        var before = await CurrentAuthTokenRowAsync();

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(WebAppRoutes.Home);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = await CurrentAuthTokenRowAsync();
        Assert.NotEqual(before.Id, after.Id);
        Assert.Equal(before.SessionId, after.SessionId);
        Assert.Equal(before.SessionStartedUtc, after.SessionStartedUtc);
    }

    [Fact]
    public async Task Renewal_RotatesTheRefreshCookie()
    {
        await SignInSuccessfullyAsync();
        var before = CurrentRefreshToken;
        Assert.NotNull(before);

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(WebAppRoutes.Home);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotNull(CurrentRefreshToken);
        Assert.NotEqual(before, CurrentRefreshToken);
    }

    [Fact]
    public async Task Renewal_RevokesThePreviousToken()
    {
        await SignInSuccessfullyAsync();
        var previousTokenId = Guid.Parse(CurrentAuthTokenId!);

        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using var response = await Client.GetAsync(WebAppRoutes.Home);
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
        using var response = await Client.GetAsync(WebAppRoutes.Home);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotEqual(previousTokenId, CurrentAuthTokenId);
    }

    [Fact]
    public async Task RequestWithinTokenTtl_DoesNotRotateTheToken()
    {
        await SignInSuccessfullyAsync();
        var before = TestJwt.GetJti(CurrentAuthToken!);

        Clock.AdvanceSeconds(AuthTokenTtlSeconds / 2);
        using var response = await Client.GetAsync(WebAppRoutes.Home);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(before, TestJwt.GetJti(CurrentAuthToken!));
    }

    [Fact]
    public async Task Renewal_ClampsExpiryToTheSessionBound()
    {
        await SignInSuccessfullyAsync();
        var sessionExpiresUtc = (await CurrentAuthTokenRowAsync()).SessionStartedUtc.AddSeconds(
            AuthSessionTtlSeconds
        );

        Clock.AdvanceSeconds(AuthSessionTtlSeconds - (AuthTokenTtlSeconds / 2));
        using var response = await Client.GetAsync(WebAppRoutes.Home);
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
        using var response = await Client.GetAsync(WebAppRoutes.Home);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task ConcurrentRenewal_RefusesTheLoserWithoutSigningOut()
    {
        await SignInSuccessfullyAsync();
        var stale = Client.Cookies.Cookies.ToDictionary();
        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);

        using var winner = await Client.GetAsync(WebAppRoutes.Home);
        using var staleClient = CreateClientWith(stale);
        using var loser = await staleClient.GetAsync(WebAppRoutes.Home);

        Assert.Equal(HttpStatusCode.OK, winner.StatusCode);
        Assert.DoesNotContain(staleClient.LastSetCookies, c => c.IsDeletion);
        using var next = await Client.GetAsync(WebAppRoutes.Home);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
    }

    [Fact]
    public async Task ReusedRefreshCookie_EndsTheSessionForEveryHolder()
    {
        await SignInSuccessfullyAsync();
        var stolen = Client.Cookies.Cookies.ToDictionary();
        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        using (var renewed = await Client.GetAsync(WebAppRoutes.Home))
            Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);

        Clock.AdvanceSeconds(60);
        using var attackerClient = CreateClientWith(stolen);
        using var attacker = await attackerClient.GetAsync(WebAppRoutes.Home);
        using var owner = await Client.GetAsync(WebAppRoutes.Home);

        Assert.Equal(HttpStatusCode.Redirect, attacker.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, owner.StatusCode);
        Assert.False(HasAuthCookies);
    }
}
