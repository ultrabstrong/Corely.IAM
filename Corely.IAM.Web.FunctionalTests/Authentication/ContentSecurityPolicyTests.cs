using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

/// <summary>
/// Corely.IAM.Web sets no Content-Security-Policy, so the reference host has to. This holds that it
/// does, on the pages the library serves.
/// </summary>
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
