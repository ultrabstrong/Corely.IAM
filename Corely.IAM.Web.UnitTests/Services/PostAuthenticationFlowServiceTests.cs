using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SignInResultModel = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.UnitTests.Services;

public class PostAuthenticationFlowServiceTests
{
    private readonly Mock<IAuthenticationService> _mockAuthenticationService = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly Mock<IAuthCookieManager> _mockAuthCookieManager = new();
    private readonly PostAuthenticationFlowService _service;

    public PostAuthenticationFlowServiceTests()
    {
        _service = new PostAuthenticationFlowService(
            _mockAuthenticationService.Object,
            _mockUserContextProvider.Object,
            _mockAuthCookieManager.Object
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
            Guid.CreateVersion7()
        );
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: []));

        var result = await _service.CompleteSignInAsync(httpContext, signInResult, 3600);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
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
                    false,
                    3600
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
            Guid.CreateVersion7()
        );
        var account1 = new Account { Id = Guid.CreateVersion7(), AccountName = "One" };
        var account2 = new Account { Id = Guid.CreateVersion7(), AccountName = "Two" };
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: [account1, account2]));

        var result = await _service.CompleteSignInAsync(httpContext, signInResult, 3600);

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
            Guid.CreateVersion7()
        );
        var switchedResult = new SignInResultModel(
            SignInResultCode.Success,
            null,
            "switched-token",
            Guid.CreateVersion7()
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

        var result = await _service.CompleteSignInAsync(httpContext, signInResult, 3600);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
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
                    false,
                    3600
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
            Guid.CreateVersion7()
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

        var result = await _service.CompleteSignInAsync(httpContext, signInResult, 3600);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
        _mockAuthCookieManager.Verify(
            x =>
                x.SetAuthCookies(
                    httpContext.Response.Cookies,
                    signInResult.AuthToken!,
                    signInResult.AuthTokenId!.Value,
                    false,
                    3600
                ),
            Times.Once
        );
    }
}
