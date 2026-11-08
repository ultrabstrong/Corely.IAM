using System.Net;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Corely.IAM.WebApp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.FunctionalTests.Blazor;

// A Blazor tab is one DI scope that lives as long as the circuit. These tests open such a scope
// on the real host and run its operations, as a component on screen for a while would.
public class OperationScopeTests : FunctionalTestBase
{
    private const string PAGE = "http://localhost/accounts";

    protected override int AuthTokenTtlSeconds => 300;
    protected override int AuthSessionTtlSeconds => 3600;

    private sealed class Tab : IDisposable
    {
        private readonly IServiceScope _scope;

        public Tab(IServiceProvider root, string token)
        {
            _scope = root.CreateScope();
            var httpContext = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
            httpContext.Request.Headers.Cookie = $"{AuthCookieManager.AUTH_TOKEN_COOKIE}={token}";
            _scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                httpContext;
            var navigation = _scope.ServiceProvider.GetRequiredService<NavigationManager>();
            ((IHostEnvironmentNavigationManager)navigation).Initialize("http://localhost/", PAGE);
            Operations = _scope.ServiceProvider.GetRequiredService<IOperationScopes>();
        }

        public IOperationScopes Operations { get; }

        public void Dispose() => _scope.Dispose();
    }

    private Tab OpenTab() => new(Factory.Services, CurrentAuthToken!);

    private Task<string?> ReadAccountNameAsync(Tab tab) =>
        tab.Operations.RunAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IRetrievalService>()
                .GetAccountAsync(AccountId, hydrate: false);
            Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
            return result.Item!.AccountName;
        });

    private Task RenameAccountElsewhereAsync(string name) =>
        Factory.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .AuthenticateWithTokenAsync(CurrentAuthToken!);
            Assert.Equal(Users.Models.UserAuthTokenValidationResultCode.Success, signIn);
            var result = await services
                .GetRequiredService<IModificationService>()
                .ModifyAccountAsync(new UpdateAccountRequest(AccountId, name));
            Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        });

    [Fact]
    public async Task RunAsync_ReadsWhatAnotherScopeChanged_ForALaterOperationInTheSameTab()
    {
        await SignInSuccessfullyAsync();
        using var tab = OpenTab();
        Assert.Equal(SeedData.AccountName, await ReadAccountNameAsync(tab));

        await RenameAccountElsewhereAsync("Renamed Elsewhere");

        Assert.Equal("Renamed Elsewhere", await ReadAccountNameAsync(tab));
    }

    [Fact]
    public async Task RunAsync_ReloadsTheCurrentPage_ForTheFirstOperationAfterTheTokenExpires()
    {
        await SignInSuccessfullyAsync();
        using var tab = OpenTab();
        Assert.True(await tab.Operations.RunAsync(_ => Task.CompletedTask));
        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);
        var ran = false;

        // Outside a circuit the host's navigation manager redirects by throwing.
        var redirect = await Assert.ThrowsAsync<NavigationException>(() =>
            tab.Operations.RunAsync(_ =>
            {
                ran = true;
                return Task.CompletedTask;
            })
        );

        Assert.False(ran);
        Assert.Equal(PAGE, redirect.Location);
    }

    [Fact]
    public async Task Reload_RenewsTheSession_ForATabWhoseTokenExpired()
    {
        await SignInSuccessfullyAsync();
        var expired = CurrentAuthToken;
        Clock.AdvanceSeconds(AuthTokenTtlSeconds + 10);

        using var reload = await Client.GetAsync(WebAppRoutes.Home);

        Assert.Equal(HttpStatusCode.OK, reload.StatusCode);
        Assert.NotEqual(expired, CurrentAuthToken);
        using var tab = OpenTab();
        Assert.True(await tab.Operations.RunAsync(_ => Task.CompletedTask));
    }
}
