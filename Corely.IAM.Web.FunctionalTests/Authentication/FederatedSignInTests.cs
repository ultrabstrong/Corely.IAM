using System.Net;
using System.Web;
using Corely.IAM.DataAccess;
using Corely.IAM.IdentityProviders.Providers;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.Web.Pages.Authentication;
using Corely.IAM.Web.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class FederatedSignInTests : FunctionalTestBase
{
    private const string START = "/signin/start/google";
    private const string CALLBACK = "/signin/callback/google";

    private readonly StubGoogle _google = new();

    protected override IReadOnlyDictionary<string, string> Settings =>
        new Dictionary<string, string>
        {
            ["SecurityOptions:IdentityProviders:Google:ClientId"] = StubGoogle.CLIENT_ID,
            ["SecurityOptions:IdentityProviders:Google:ClientSecret"] = "functional-secret",
        };

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<FederationHttp>();
        services.AddSingleton(new FederationHttp(new HttpClient(_google)));
    }

    private async Task<(string State, string Nonce, Uri Authorize)> StartAsync(string path = START)
    {
        _google.UtcNow = Clock.GetUtcNow().UtcDateTime;
        using var response = await Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        var query = HttpUtility.ParseQueryString(location.Query);
        return (query["state"]!, query["nonce"]!, location);
    }

    private async Task<HttpResponseMessage> CallbackAsync(
        string state,
        string code = "the-code",
        string? fetchSite = "cross-site"
    ) =>
        await Client.GetAsync(
            $"{CALLBACK}?code={code}&state={Uri.EscapeDataString(state)}",
            fetchSite
        );

    [Fact]
    public async Task SignInPage_OffersTheEnabledProvider()
    {
        using var response = await Client.GetAsync(AppRoutes.SignIn);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("href=\"/signin/start/google\"", html);
        Assert.Contains("Sign in with Google", html);
        Assert.Contains("By continuing, you agree to our", html);
    }

    [Fact]
    public async Task RegisterPage_ShowsTheSignUpTerms()
    {
        using var response = await Client.GetAsync(AppRoutes.Register);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("By signing up, you agree to our", html);
        Assert.Contains($"href=\"{AppRoutes.Terms}\"", html);
        Assert.Contains($"href=\"{AppRoutes.Privacy}\"", html);
    }

    [Fact]
    public async Task Start_RedirectsToTheProvider_WithAProtectedCorrelationCookie()
    {
        var (state, nonce, authorize) = await StartAsync();

        Assert.Equal(
            "https://accounts.google.com/authorize",
            authorize.GetLeftPart(UriPartial.Path)
        );
        Assert.False(string.IsNullOrWhiteSpace(state));
        Assert.False(string.IsNullOrWhiteSpace(nonce));
        var query = HttpUtility.ParseQueryString(authorize.Query);
        Assert.Equal($"https://localhost{CALLBACK}", query["redirect_uri"]);
        Assert.Equal("S256", query["code_challenge_method"]);

        var cookie = Assert.Single(
            Client.LastSetCookies,
            c => c.Name == FederatedSignInCookie.NAME
        );
        Assert.True(cookie.HttpOnly);
        Assert.True(cookie.Secure);
        Assert.Equal("lax", cookie.SameSite?.ToLowerInvariant());
        Assert.DoesNotContain(state, cookie.Value);
    }

    [Fact]
    public async Task Start_OnlyAnswersAGet()
    {
        using var response = await Client.PostFormWithoutAntiforgeryAsync(START, []);

        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Empty(Client.LastSetCookies.Where(c => c.Name == FederatedSignInCookie.NAME));
    }

    [Fact]
    public async Task Callback_SignsUpAndSignsIn_ThroughTheWholeFlow()
    {
        var (state, nonce, _) = await StartAsync();
        _google.NextIdToken = _google.IdToken("google-subject", "new.person@gmail.com", nonce);

        using var response = await CallbackAsync(state);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(HasAuthCookies);
        Assert.False(Client.Cookies.Contains(FederatedSignInCookie.NAME));
        var linked = await Factory.WithScopeAsync(services =>
            services
                .GetRequiredService<IamDbContext>()
                .FederatedIdentities.AnyAsync(f => f.Subject == "google-subject")
        );
        Assert.True(linked);
    }

    [Fact]
    public async Task Callback_ChangesNothing_WithoutTheCorrelationCookie()
    {
        using var response = await CallbackAsync("forged-state");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("could not be matched", html);
        Assert.False(HasAuthCookies);
        Assert.Equal(0, _google.TokenRequests);
    }

    [Fact]
    public async Task Callback_ChangesNothing_ForAMismatchedState()
    {
        var (_, nonce, _) = await StartAsync();
        _google.NextIdToken = _google.IdToken("google-subject", "new.person@gmail.com", nonce);

        using var response = await CallbackAsync("another-state");

        Assert.False(HasAuthCookies);
        Assert.Equal(0, _google.TokenRequests);
    }

    [Fact]
    public async Task Callback_UsesItsStateOnce()
    {
        var (state, nonce, _) = await StartAsync();
        _google.NextIdToken = _google.IdToken("google-subject", "new.person@gmail.com", nonce);
        using (await CallbackAsync(state)) { }
        Client.Cookies.Restore(
            Client
                .Cookies.Cookies.Where(c => c.Key == AuthCookieManager.DEVICE_ID_COOKIE)
                .ToDictionary(c => c.Key, c => c.Value)
        );

        using var replay = await CallbackAsync(state);

        Assert.False(HasAuthCookies);
        Assert.Equal(1, _google.TokenRequests);
    }

    [Fact]
    public async Task Callback_RefusesACookieMadeForAnotherProvider()
    {
        var (state, _, _) = await StartAsync();

        using var response = await Client.GetAsync(
            $"/signin/callback/github?code=c&state={Uri.EscapeDataString(state)}",
            "cross-site"
        );

        Assert.False(HasAuthCookies);
        Assert.Contains(
            FederatedSignInCallbackModel.START_AGAIN_MESSAGE,
            HttpUtility.HtmlDecode(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
            )
        );
    }

    [Fact]
    public async Task Callback_ChangesNothing_WhenThePersonDeclines()
    {
        var (state, _, _) = await StartAsync();

        using var response = await Client.GetAsync(
            $"{CALLBACK}?error=access_denied&state={Uri.EscapeDataString(state)}",
            "cross-site"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(HasAuthCookies);
        Assert.Equal(0, _google.TokenRequests);
    }

    [Fact]
    public async Task Callback_ReturnsOnlyToALocalPath()
    {
        var (state, nonce, _) = await StartAsync(
            $"{START}?returnUrl={Uri.EscapeDataString("https://evil.example/steal")}"
        );
        _google.NextIdToken = _google.IdToken("google-subject", "new.person@gmail.com", nonce);

        using var response = await CallbackAsync(state);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(
            response.Headers.Location!.IsAbsoluteUri
                && response.Headers.Location.Host == "evil.example"
        );
        Assert.DoesNotContain("evil.example", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Callback_NeverSignsIntoAnExistingUser_ByEmail()
    {
        var (state, nonce, _) = await StartAsync();
        _google.NextIdToken = _google.IdToken("attacker-subject", SeedData.OwnerEmail, nonce);

        using var response = await CallbackAsync(state);

        Assert.False(HasAuthCookies);
        Assert.Contains(
            "An account already uses this email",
            HttpUtility.HtmlDecode(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
            )
        );
    }

    [Fact]
    public async Task Start_RefusesLinking_WhenSignedOut()
    {
        using var response = await Client.GetAsync($"{START}?purpose=link");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(Client.LastSetCookies.Where(c => c.Name == FederatedSignInCookie.NAME));
    }
}
