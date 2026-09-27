using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class PasswordToggleTests : FunctionalTestBase
{
    [Theory]
    [InlineData("/signin")]
    [InlineData("/register")]
    public async Task PasswordPagesReferenceTheToggleScript(string route)
    {
        using var response = await Client.GetAsync(route);
        response.EnsureSuccessStatusCode();

        Assert.Contains(
            "_content/Corely.IAM.Web/js/password-toggle.js",
            await response.Content.ReadAsStringAsync()
        );
    }

    [Fact]
    public async Task TheScriptIsServedFromTheStaticWebAsset()
    {
        using var response = await Client.GetAsync(
            "/_content/Corely.IAM.Web/js/password-toggle.js"
        );

        response.EnsureSuccessStatusCode();
        Assert.Contains("input[type=\"password\"]", await response.Content.ReadAsStringAsync());
    }
}
