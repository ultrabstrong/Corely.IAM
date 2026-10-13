using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class DemoQuickSignInTests : FunctionalTestBase
{
    [Fact]
    public async Task TheSignInPageOffersQuickSignInInDevelopment()
    {
        using var response = await Client.GetAsync(AppRoutes.SignIn);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("id=\"demo-quick-sign-in\"", html);
        Assert.Contains("value=\"platform-owner\"", html);
    }

    [Fact]
    public async Task TheCodePageHasNoCodeButtonWithoutASavedSecret()
    {
        var html = await GetVerifyMfaPageAsync();

        Assert.Contains("id=\"code\"", html);
        Assert.DoesNotContain("demo-fill-code", html);
    }
}
