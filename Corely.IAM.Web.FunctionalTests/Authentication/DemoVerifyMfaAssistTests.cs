using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class DemoVerifyMfaAssistTests : FunctionalTestBase
{
    internal const string PLATFORM_OWNER_SECRET = "JBSWY3DPEHPK3PXP";

    protected override IReadOnlyDictionary<string, string> Settings =>
        new Dictionary<string, string>
        {
            ["DemoFeatures:PlatformOwnerTotpSecret"] = PLATFORM_OWNER_SECRET,
        };

    [Fact]
    public async Task TheCodePageOffersPlatformOwnersCurrentCodeInDevelopment()
    {
        var html = await GetVerifyMfaPageAsync();
        var code = await GenerateTotpCodeAsync(PLATFORM_OWNER_SECRET);

        Assert.Contains("id=\"demo-fill-code\"", html);
        Assert.Contains($"data-code=\"{code}\"", html);
    }
}
