using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class DemoQuickSignInProductionTests : FunctionalTestBase
{
    protected override string EnvironmentName => Environments.Production;

    protected override IReadOnlyDictionary<string, string> Settings =>
        new Dictionary<string, string>
        {
            ["DemoFeatures:PlatformOwnerTotpSecret"] =
                DemoVerifyMfaAssistTests.PLATFORM_OWNER_SECRET,
        };

    [Fact]
    public async Task TheCodePageHasNoCodeButtonOutsideDevelopment()
    {
        var html = await GetVerifyMfaPageAsync();

        Assert.Contains("id=\"code\"", html);
        Assert.DoesNotContain("demo-fill-code", html);
    }

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
