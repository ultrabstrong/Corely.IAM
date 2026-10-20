using System.Net;
using System.Reflection;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.Web.Security;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class CrossSiteRequestTests : FunctionalTestBase
{
    public static TheoryData<string, string?> EveryEndpointFromElsewhere()
    {
        var data = new TheoryData<string, string?>();
        foreach (
            var field in typeof(AppRoutes).GetFields(BindingFlags.Public | BindingFlags.Static)
        )
        {
            var path = (string)field.GetValue(null)!;
            data.Add(path, "cross-site");
            data.Add(path, "same-site");
            data.Add(path, null);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryEndpointFromElsewhere))]
    public async Task Get_ChangesNoSession_ForARequestAnotherSiteStarted(
        string path,
        string? fetchSite
    )
    {
        var otherAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();
        var tokenId = CurrentAuthTokenId;
        var target = path == AppRoutes.SwitchAccount ? $"{path}?accountId={otherAccountId}" : path;

        using (var response = await Client.GetAsync(target, fetchSite))
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        Assert.DoesNotContain(
            Client.LastSetCookies,
            c =>
                c.Name
                    is AuthCookieManager.AUTH_TOKEN_COOKIE
                        or AuthCookieManager.AUTH_TOKEN_ID_COOKIE
        );
        Assert.Equal(tokenId, CurrentAuthTokenId);
        Assert.Null((await GetAuthTokenRowAsync(Guid.Parse(tokenId!)))!.RevokedUtc);
    }

    [Fact]
    public async Task SignOut_AsksFirst_ForALinkFromAnotherSite()
    {
        await SignInSuccessfullyAsync();

        using var response = await Client.GetAsync(AppRoutes.SignOut, "cross-site");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<form method=\"post\"", html);
        Assert.True(HasAuthCookies);
    }

    [Fact]
    public async Task SignOut_SignsOut_ForTheConfirmPost()
    {
        await SignInSuccessfullyAsync();
        using var page = await Client.GetAsync(AppRoutes.SignOut, "cross-site");
        var html = await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await Client.PostFormAsync(
            AppRoutes.SignOut,
            [],
            Html.InputValue(html, "__RequestVerificationToken")
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task SwitchAccount_AsksFirst_ForALinkFromAnotherSite()
    {
        var otherAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();

        using var response = await Client.GetAsync(
            $"{AppRoutes.SwitchAccount}?accountId={otherAccountId}&returnUrl=%2Fprofile",
            "cross-site"
        );
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(otherAccountId.ToString(), Html.InputValue(html, "accountId"));
        Assert.Equal("/profile", Html.InputValue(html, "returnUrl"));
    }

    [Fact]
    public async Task SwitchAccount_Switches_ForTheConfirmPost()
    {
        var otherAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();
        var tokenId = CurrentAuthTokenId;
        using var page = await Client.GetAsync(
            $"{AppRoutes.SwitchAccount}?accountId={otherAccountId}&returnUrl=%2Fprofile",
            "cross-site"
        );
        var html = await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await Client.PostFormAsync(
            AppRoutes.SwitchAccount,
            new Dictionary<string, string>
            {
                ["accountId"] = otherAccountId.ToString(),
                ["returnUrl"] = "/profile",
            },
            Html.InputValue(html, "__RequestVerificationToken")
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/profile", response.Headers.Location!.OriginalString);
        Assert.NotEqual(tokenId, CurrentAuthTokenId);
    }
}
