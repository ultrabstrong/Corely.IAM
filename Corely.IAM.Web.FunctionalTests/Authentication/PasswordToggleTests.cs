using System.Text.RegularExpressions;
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

        var reference = Regex.Match(
            await response.Content.ReadAsStringAsync(),
            @"src=""/?(_content/Corely\.IAM\.Web/js/password-toggle(\.\w+)?\.js)"""
        );
        Assert.True(reference.Success);

        using var script = await Client.GetAsync("/" + reference.Groups[1].Value);
        script.EnsureSuccessStatusCode();
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
