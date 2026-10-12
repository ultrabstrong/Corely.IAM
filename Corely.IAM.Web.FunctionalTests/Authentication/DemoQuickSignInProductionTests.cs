using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class DemoQuickSignInProductionTests : FunctionalTestBase
{
    protected override string EnvironmentName => Environments.Production;

    [Fact]
    public async Task TheSignInPageHasNoQuickSignInOutsideDevelopment()
    {
        using var response = await Client.GetAsync(AppRoutes.SignIn);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("id=\"username\"", html);
        Assert.DoesNotContain("demo-quick-sign-in", html);
    }
}
