using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.WebApp;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class AccountSwitchSessionTests : FunctionalTestBase
{
    [Fact]
    public async Task SwitchingAccount_KeepsTheSession()
    {
        var secondAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();
        var before = await CurrentAuthTokenRowAsync();

        using var response = await SwitchToAsync(secondAccountId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var after = await CurrentAuthTokenRowAsync();
        Assert.NotEqual(before.Id, after.Id);
        Assert.Equal(secondAccountId, after.AccountId);
        Assert.Equal(before.SessionId, after.SessionId);
        Assert.Equal(before.SessionStartedUtc, after.SessionStartedUtc);
    }

    [Fact]
    public async Task SwitchingAccount_RotatesTheRefreshCookie()
    {
        var secondAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();
        var before = CurrentRefreshToken;

        using var response = await SwitchToAsync(secondAccountId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        Assert.NotNull(CurrentRefreshToken);
        Assert.NotEqual(before, CurrentRefreshToken);
    }

    [Fact]
    public async Task CookiesCopiedBeforeASwitch_EndTheSessionWhenReplayed()
    {
        var secondAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();
        var copied = Client.Cookies.Cookies.ToDictionary();
        using (var switched = await SwitchToAsync(secondAccountId))
            Assert.Equal(HttpStatusCode.Redirect, switched.StatusCode);
        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 60);

        using var copyClient = CreateClientWith(copied);
        using var replay = await copyClient.GetAsync(WebAppRoutes.Home);
        using var owner = await Client.GetAsync(WebAppRoutes.Home);

        Assert.Equal(HttpStatusCode.Redirect, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, owner.StatusCode);
        Assert.False(HasAuthCookies);
    }

    private Task<HttpResponseMessage> SwitchToAsync(Guid accountId) =>
        Client.GetAsync($"{AppRoutes.SwitchAccount}?accountId={accountId}", "same-origin");
}
