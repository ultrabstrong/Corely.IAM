using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.UnitTests.Services;

public class OperationScopesTests
{
    private const string TOKEN = "token-1";

    private readonly Mock<IAuthenticationService> _authenticationService = new();
    private readonly DefaultHttpContext _httpContext = new();
    private readonly TestNavigationManager _navigation = new();
    private readonly List<ScopeProbe> _probes = [];
    private readonly OperationScopes _scopes;

    public OperationScopesTests()
    {
        SetCookie(TOKEN);
        _authenticationService
            .Setup(s => s.AuthenticateWithTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(UserAuthTokenValidationResultCode.Success);

        var services = new ServiceCollection();
        // Signing in resolves the probe, so every scope that signs in has one to dispose.
        services.AddScoped(sp =>
        {
            sp.GetRequiredService<ScopeProbe>();
            return _authenticationService.Object;
        });
        services.AddScoped(_ =>
        {
            var probe = new ScopeProbe();
            _probes.Add(probe);
            return probe;
        });
        var provider = services.BuildServiceProvider();

        _scopes = new OperationScopes(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new HttpContextAccessor { HttpContext = _httpContext },
            _navigation
        );
    }

    private void SetCookie(string token) =>
        _httpContext.Request.Headers.Cookie = $"{AuthCookieManager.AUTH_TOKEN_COOKIE}={token}";

    [Fact]
    public async Task RunAsync_GivesEachOperationItsOwnScope_ForTwoCalls()
    {
        var first = await _scopes.RunAsync(s =>
            Task.FromResult(s.GetRequiredService<ScopeProbe>())
        );
        var second = await _scopes.RunAsync(s =>
            Task.FromResult(s.GetRequiredService<ScopeProbe>())
        );

        Assert.NotNull(first);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task RunAsync_SignsTheScopeInWithTheToken_BeforeTheWork()
    {
        var signedInFirst = false;
        _authenticationService
            .Setup(s => s.AuthenticateWithTokenAsync(TOKEN))
            .ReturnsAsync(() =>
            {
                signedInFirst = true;
                return UserAuthTokenValidationResultCode.Success;
            });

        var ran = await _scopes.RunAsync(_ =>
        {
            Assert.True(signedInFirst);
            return Task.CompletedTask;
        });

        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_ReadsTheCookieOnce_ForTheInstance()
    {
        await _scopes.RunAsync(_ => Task.CompletedTask);
        SetCookie("token-2");

        await _scopes.RunAsync(_ => Task.CompletedTask);

        _authenticationService.Verify(s => s.AuthenticateWithTokenAsync(TOKEN), Times.Exactly(2));
        _authenticationService.Verify(s => s.AuthenticateWithTokenAsync("token-2"), Times.Never);
    }

    [Fact]
    public async Task RunAsync_PrefersTheRenewedToken_WhenTheMiddlewareRenewedInThisRequest()
    {
        _httpContext.Items[AuthCookieManager.RENEWED_AUTH_TOKEN_ITEM] = "renewed";

        await _scopes.RunAsync(_ => Task.CompletedTask);

        _authenticationService.Verify(s => s.AuthenticateWithTokenAsync("renewed"), Times.Once);
    }

    [Fact]
    public async Task RunAsync_SkipsTheWorkAndReloads_ForAFailedSignIn()
    {
        _authenticationService
            .Setup(s => s.AuthenticateWithTokenAsync(TOKEN))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);
        var ran = false;

        var result = await _scopes.RunAsync(_ =>
        {
            ran = true;
            return Task.FromResult("done");
        });

        Assert.Null(result);
        Assert.False(ran);
        var (uri, forceLoad) = Assert.Single(_navigation.Navigations);
        Assert.Equal(_navigation.Uri, uri);
        Assert.True(forceLoad);
    }

    [Fact]
    public async Task RunAsync_RunsNothingAndStays_ForATabWithNoSession()
    {
        _httpContext.Request.Headers.Cookie = string.Empty;

        var ran = await _scopes.RunAsync(_ => Task.CompletedTask);

        Assert.False(_scopes.HasSession);
        Assert.False(ran);
        Assert.Empty(_navigation.Navigations);
        _authenticationService.Verify(
            s => s.AuthenticateWithTokenAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RunAsync_DisposesTheScope_AfterSuccess()
    {
        await _scopes.RunAsync(s => Task.FromResult(s.GetRequiredService<ScopeProbe>()));

        Assert.True(Assert.Single(_probes).Disposed);
    }

    [Fact]
    public async Task RunAsync_DisposesTheScope_AfterAFailedSignIn()
    {
        _authenticationService
            .Setup(s => s.AuthenticateWithTokenAsync(TOKEN))
            .ReturnsAsync(UserAuthTokenValidationResultCode.TokenValidationFailed);

        await _scopes.RunAsync(_ => Task.CompletedTask);

        Assert.True(Assert.Single(_probes).Disposed);
    }

    [Fact]
    public async Task RunAsync_DisposesTheScopeAndRethrows_ForThrowingWork()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _scopes.RunAsync(s =>
            {
                s.GetRequiredService<ScopeProbe>();
                throw new InvalidOperationException();
            })
        );

        Assert.True(Assert.Single(_probes).Disposed);
    }

    private sealed class ScopeProbe : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public List<(string Uri, bool ForceLoad)> Navigations { get; } = [];

        public TestNavigationManager() => Initialize("https://app.test/", "https://app.test/users");

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            Navigations.Add((uri, options.ForceLoad));
    }
}
