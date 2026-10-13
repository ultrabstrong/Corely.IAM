using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class SecurityHeadersTests : FunctionalTestBase
{
    [Theory]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("Cross-Origin-Opener-Policy", "same-origin")]
    public async Task TheHostsHeadersReachTheLibrarysSignInPage(string name, string value)
    {
        using var response = await Client.GetAsync(AppRoutes.SignIn);

        Assert.Equal(value, Assert.Single(response.Headers.GetValues(name)));
    }

    [Fact]
    public async Task PagesAreNotStored()
    {
        using var response = await Client.GetAsync(AppRoutes.SignIn);

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task StaticFilesKeepTheStaticFileHandlersCacheHeader()
    {
        using var response = await Client.GetAsync("/_content/Corely.IAM.Web/js/form-busy.js");

        response.EnsureSuccessStatusCode();
        Assert.Single(response.Headers.GetValues("Cache-Control"));
        Assert.False(response.Headers.CacheControl?.NoStore);
    }
}
