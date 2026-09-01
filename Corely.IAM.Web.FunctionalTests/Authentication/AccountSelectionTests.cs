using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class AccountSelectionTests : FunctionalTestBase
{
    [Fact]
    public async Task SingleAccountUser_GoesStraightToDashboard()
    {
        using var response = await SignInAsync();

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain(AppRoutes.SelectAccount, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task MultiAccountUser_IsRoutedToSelection()
    {
        await CreateAdditionalAccountAsync(SeedData.SecondAccountName);

        using var response = await SignInAsync();

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SelectAccount, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SelectingAnAccount_IssuesAScopedTokenAndReachesDashboard()
    {
        var secondAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        using (var signIn = await SignInAsync())
            Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);

        using var response = await Client.PostFormAsync(
            AppRoutes.SelectAccount,
            new Dictionary<string, string> { ["accountId"] = secondAccountId.ToString() }
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AppRoutes.Dashboard, response.Headers.Location!.OriginalString);
        Assert.True(HasAuthCookies);
    }

    [Fact]
    public async Task SwitchingAccounts_RotatesTheToken()
    {
        var secondAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        using (var signIn = await SignInAsync())
            Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);

        using (
            var first = await Client.PostFormAsync(
                AppRoutes.SelectAccount,
                new Dictionary<string, string> { ["accountId"] = AccountId.ToString() }
            )
        )
            Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);

        var beforeSwitch = TestJwt.GetJti(CurrentAuthToken!);

        using var second = await Client.PostFormAsync(
            AppRoutes.SelectAccount,
            new Dictionary<string, string> { ["accountId"] = secondAccountId.ToString() }
        );
        Assert.Equal(HttpStatusCode.Redirect, second.StatusCode);

        Assert.NotEqual(beforeSwitch, TestJwt.GetJti(CurrentAuthToken!));
    }

    [Fact]
    public async Task SwitchingToAnAccountTheUserDoesNotBelongTo_IsRefused()
    {
        await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        using (var signIn = await SignInAsync())
            Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);

        using var response = await Client.PostFormAsync(
            AppRoutes.SelectAccount,
            new Dictionary<string, string> { ["accountId"] = Guid.NewGuid().ToString() }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SelectAccountPage_IsNotReachableAnonymously()
    {
        using var response = await Client.GetAsync(AppRoutes.SelectAccount);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }
}
