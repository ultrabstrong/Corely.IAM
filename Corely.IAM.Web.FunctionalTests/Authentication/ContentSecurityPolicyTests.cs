using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class ContentSecurityPolicyTests : FunctionalTestBase
{
    [Fact]
    public async Task TheHostsPolicyReachesTheLibrarysSignInPage()
    {
        using var response = await Client.GetAsync(AppRoutes.SignIn);

        var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", policy);
        Assert.Contains("frame-ancestors 'none'", policy);
    }
}
