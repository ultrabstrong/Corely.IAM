using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SignInResultModel = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.UnitTests.Services;

public class PostAuthenticationFlowServiceTests
{
    private const int AUTH_SESSION_TTL_SECONDS = 604800;

    private readonly Mock<IAuthenticationService> _mockAuthenticationService = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly Mock<IAuthCookieManager> _mockAuthCookieManager = new();
    private readonly PostAuthenticationFlowService _service;

    public PostAuthenticationFlowServiceTests()
    {
        _service = new PostAuthenticationFlowService(
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object,
            _mockAuthCookieManager.Object,
            Options.Create(new IAMWebOptions())
        );
    }

    [Fact]
    public async Task CompleteSignInAsync_NoAccounts_RedirectsToDashboard()
    {
        var httpContext = new DefaultHttpContext();
        var signInResult = new SignInResultModel(
            SignInResultCode.Success,
            null,
            "auth-token",
            Guid.CreateVersion7(),
            RefreshToken: "refresh-token",
            RefreshExpiresUtc: DateTime.UtcNow.AddDays(1)
        );
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: []));

        var result = await _service.CompleteSignInAsync(httpContext, signInResult, null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
        _mockAuthenticationService.Verify(
            x => x.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()),
            Times.Never
        );
        _mockAuthCookieManager.Verify(
            x =>
                x.SetAuthCookies(
                    httpContext.Response.Cookies,
                    signInResult.AuthToken!,
                    signInResult.AuthTokenId!.Value,
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    false
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CompleteSignInAsync_MultipleAccounts_RedirectsToSelectAccount()
    {
        var httpContext = new DefaultHttpContext();
        var signInResult = new SignInResultModel(
            SignInResultCode.Success,
            null,
            "auth-token",
            Guid.CreateVersion7(),
            RefreshToken: "refresh-token",
            RefreshExpiresUtc: DateTime.UtcNow.AddDays(1)
        );
        var account1 = new Account { Id = Guid.CreateVersion7(), AccountName = "One" };
        var account2 = new Account { Id = Guid.CreateVersion7(), AccountName = "Two" };
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: [account1, account2]));

        var result = await _service.CompleteSignInAsync(httpContext, signInResult, null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
        _mockAuthenticationService.Verify(
            x => x.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CompleteSignInAsync_OneAccount_SwitchSuccess_RedirectsToDashboard()
    {
        var httpContext = new DefaultHttpContext();
        var signInResult = new SignInResultModel(
            SignInResultCode.Success,
            null,
            "auth-token",
            Guid.CreateVersion7(),
            RefreshToken: "refresh-token",
            RefreshExpiresUtc: DateTime.UtcNow.AddDays(1)
        );
        var switchedResult = new SignInResultModel(
            SignInResultCode.Success,
            null,
            "switched-token",
            Guid.CreateVersion7(),
            RefreshToken: "refresh-token",
            RefreshExpiresUtc: DateTime.UtcNow.AddDays(1)
        );
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "One" };
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: [account]));
        _mockAuthenticationService
            .Setup(x =>
                x.SwitchAccountAsync(It.Is<SwitchAccountRequest>(r => r.AccountId == account.Id))
            )
            .ReturnsAsync(switchedResult);

        var result = await _service.CompleteSignInAsync(httpContext, signInResult, null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
        _mockAuthenticationService.Verify(
            x => x.SwitchAccountAsync(It.Is<SwitchAccountRequest>(r => r.AccountId == account.Id)),
            Times.Once
        );
        _mockAuthCookieManager.Verify(
            x =>
                x.SetAuthCookies(
                    httpContext.Response.Cookies,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    false
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task CompleteSignInAsync_OneAccount_SwitchFails_RedirectsToSelectAccount()
    {
        var httpContext = new DefaultHttpContext();
        var signInResult = new SignInResultModel(
            SignInResultCode.Success,
            null,
            "auth-token",
            Guid.CreateVersion7(),
            RefreshToken: "refresh-token",
            RefreshExpiresUtc: DateTime.UtcNow.AddDays(1)
        );
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "One" };
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: [account]));
        _mockAuthenticationService
            .Setup(x =>
                x.SwitchAccountAsync(It.Is<SwitchAccountRequest>(r => r.AccountId == account.Id))
            )
            .ReturnsAsync(
                new SignInResultModel(
                    SignInResultCode.AccountNotFoundError,
                    "Not found",
                    null,
                    null
                )
            );

        var result = await _service.CompleteSignInAsync(httpContext, signInResult, null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
        _mockAuthCookieManager.Verify(
            x =>
                x.SetAuthCookies(
                    httpContext.Response.Cookies,
                    signInResult.AuthToken!,
                    signInResult.AuthTokenId!.Value,
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    false
                ),
            Times.Once
        );
    }

    private const string LOCAL_RETURN_URL = "/accounts/1/grants";

    private static SignInResultModel SignedIn() =>
        new(
            SignInResultCode.Success,
            null,
            "auth-token",
            Guid.CreateVersion7(),
            RefreshToken: "refresh-token",
            RefreshExpiresUtc: DateTime.UtcNow.AddDays(1)
        );

    private void WithAccounts(params Account[] accounts) =>
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: [.. accounts]));

    private static Account AnAccount() =>
        new() { Id = Guid.CreateVersion7(), AccountName = "Acme" };

    [Fact]
    public async Task CompleteSignInAsync_ReturnsToTheUrl_ForOneAccount()
    {
        var account = AnAccount();
        WithAccounts(account);
        _mockAuthenticationService
            .Setup(x => x.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()))
            .ReturnsAsync(SignedIn());

        var result = await _service.CompleteSignInAsync(
            new DefaultHttpContext(),
            SignedIn(),
            LOCAL_RETURN_URL
        );

        Assert.Equal(LOCAL_RETURN_URL, Assert.IsType<RedirectResult>(result).Url);
    }

    [Fact]
    public async Task CompleteSignInAsync_ReturnsToTheUrlWithoutChoosing_ForSeveralAccounts()
    {
        WithAccounts(AnAccount(), AnAccount());

        var result = await _service.CompleteSignInAsync(
            new DefaultHttpContext(),
            SignedIn(),
            LOCAL_RETURN_URL
        );

        Assert.Equal(LOCAL_RETURN_URL, Assert.IsType<RedirectResult>(result).Url);
        _mockAuthenticationService.Verify(
            x => x.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CompleteSignInAsync_GoesHome_ForNoAccountsEvenWithAReturnUrl()
    {
        WithAccounts();

        var result = await _service.CompleteSignInAsync(
            new DefaultHttpContext(),
            SignedIn(),
            LOCAL_RETURN_URL
        );

        Assert.Equal("/", Assert.IsType<RedirectResult>(result).Url);
    }

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("//evil.example/accounts")]
    public async Task CompleteSignInAsync_IgnoresTheUrl_ForAReturnUrlOffThisSite(string returnUrl)
    {
        WithAccounts(AnAccount(), AnAccount());

        var result = await _service.CompleteSignInAsync(
            new DefaultHttpContext(),
            SignedIn(),
            returnUrl
        );

        Assert.Equal(AppRoutes.SelectAccount, Assert.IsType<RedirectResult>(result).Url);
    }

    [Fact]
    public async Task CompleteSignInAsync_GoesToTheHostsHomePath_ForNoReturnUrl()
    {
        var service = new PostAuthenticationFlowService(
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object,
            _mockAuthCookieManager.Object,
            Options.Create(new IAMWebOptions { HomePath = "/home" })
        );
        WithAccounts();

        var result = await service.CompleteSignInAsync(new DefaultHttpContext(), SignedIn());

        Assert.Equal("/home", Assert.IsType<RedirectResult>(result).Url);
    }
}
