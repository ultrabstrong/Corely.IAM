using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.Web.FunctionalTests.Demos;

/// <summary>
/// Keeps the demo apps from rotting unnoticed: each must start, let someone sign up through the
/// library's pages, and route none of the library's admin pages. What the demos do once signed in
/// is Blazor over a circuit and is not reachable from this tier.
/// </summary>
public abstract class DemoAppTestsBase<TNotesContext> : IAsyncLifetime
    where TNotesContext : DbContext
{
    private DemoAppFactory<TNotesContext> _factory = null!;
    private TestClient _client = null!;

    protected abstract string AppName { get; }

    public ValueTask InitializeAsync()
    {
        _factory = new DemoAppFactory<TNotesContext>();
        _factory.InitializeIamDatabase();
        _client = new TestClient(_factory.CreateTestClient());
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SignUp_LandsSignedInOnTheAppsHomePage()
    {
        await SignUpAsync();

        using var home = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
    }

    [Theory]
    [InlineData(AppRoutes.Users)]
    [InlineData(AppRoutes.Groups)]
    [InlineData(AppRoutes.Roles)]
    [InlineData(AppRoutes.Permissions)]
    public async Task TheLibrarysAdminPagesAreNotRouted(string route)
    {
        await SignUpAsync();

        using var response = await _client.GetAsync(route);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SignInPagesUseTheAppsOwnLayout()
    {
        using var response = await _client.GetAsync(AppRoutes.SignIn);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(AppName, html);
        Assert.DoesNotContain("Corely IAM", html);
        Assert.Contains("_content/Corely.IAM.Web/js/form-busy.js", html);
    }

    [Fact]
    public async Task TheAppSendsItsOwnContentSecurityPolicy()
    {
        using var response = await _client.GetAsync(AppRoutes.SignIn);

        var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", policy);
    }

    private async Task SignUpAsync()
    {
        using var response = await _client.PostFormAsync(
            AppRoutes.Register,
            new Dictionary<string, string>
            {
                ["Username"] = "demo.user",
                ["Email"] = "demo.user@example.com",
                ["Password"] = SeedData.OwnerPassword,
                ["ConfirmPassword"] = SeedData.OwnerPassword,
            }
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AppRoutes.Dashboard, response.Headers.Location!.OriginalString);
    }
}
