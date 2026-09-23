using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class FormBusyStateTests : FunctionalTestBase
{
    [Fact]
    public async Task AuthPagesReferenceTheBusyStateScript()
    {
        var html = await GetPageAsync(AppRoutes.SignIn);

        Assert.Contains("_content/Corely.IAM.Web/js/form-busy.js", html);
    }

    [Fact]
    public async Task TheScriptIsServedFromTheStaticWebAsset()
    {
        using var response = await Client.GetAsync("/_content/Corely.IAM.Web/js/form-busy.js");

        response.EnsureSuccessStatusCode();
        var script = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-busy-spinner", script);
    }

    [Theory]
    [InlineData("/signin")]
    [InlineData("/register")]
    public async Task ThePrimaryActionOptsIntoTheSpinner(string route)
    {
        var html = await GetPageAsync(route);

        Assert.Contains("data-busy-spinner", html);
    }

    private async Task<string> GetPageAsync(string route)
    {
        using var response = await Client.GetAsync(route);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
