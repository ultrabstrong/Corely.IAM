using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Pages.Authentication;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SignInResult = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.UnitTests.Pages.Auth;

public class SelectAccountModelTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IUserContextProvider> _mockUserContextProvider;
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly SelectAccountModel _model;

    public SelectAccountModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockUserContextProvider = new Mock<IUserContextProvider>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _model = new SelectAccountModel(
            _mockAuthService.Object,
            _mockUserContextProvider.Object,
            _mockCookieManager.Object,
            Options.Create(new SecurityOptions())
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public void OnGet_NoUserContext_RedirectsToSignIn()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);

        var result = _model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }

    [Fact]
    public void OnGet_HasCurrentAccount_ReturnsPageWithSortedAccounts()
    {
        var accounts = new List<Account>
        {
            new() { Id = Guid.CreateVersion7(), AccountName = "Zebra" },
            new() { Id = Guid.CreateVersion7(), AccountName = "Alpha" },
        };
        var currentAccount = accounts[0];
        _mockUserContextProvider
            .Setup(p => p.GetUserContext())
            .Returns(
                PageTestHelpers.CreateUserContext(
                    currentAccount: currentAccount,
                    availableAccounts: accounts
                )
            );

        var result = _model.OnGet();

        Assert.IsType<PageResult>(result);
        Assert.Equal(2, _model.Accounts.Count);
        Assert.Equal("Alpha", _model.Accounts[0].AccountName);
        Assert.Equal("Zebra", _model.Accounts[1].AccountName);
    }

    [Fact]
    public void OnGet_NoCurrentAccount_ReturnsPageWithAccounts()
    {
        var accounts = new List<Account>
        {
            new() { Id = Guid.CreateVersion7(), AccountName = "Account1" },
            new() { Id = Guid.CreateVersion7(), AccountName = "Account2" },
        };
        _mockUserContextProvider
            .Setup(p => p.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: accounts));

        var result = _model.OnGet();

        Assert.IsType<PageResult>(result);
        Assert.Equal(2, _model.Accounts.Count);
        Assert.Equal("Account1", _model.Accounts[0].AccountName);
        Assert.Equal("Account2", _model.Accounts[1].AccountName);
    }

    [Fact]
    public async Task OnPostAsync_SwitchFails_ReturnsPageWithError()
    {
        var accountId = Guid.CreateVersion7();
        var accounts = new List<Account>
        {
            new() { Id = accountId, AccountName = "Account1" },
        };

        _mockAuthService
            .Setup(s => s.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()))
            .ReturnsAsync(
                new SignInResult(SignInResultCode.AccountNotFoundError, "Not found", null, null)
            );
        _mockUserContextProvider
            .Setup(p => p.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: accounts));

        var result = await _model.OnPostAsync(accountId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to switch account.", _model.ErrorMessage);
        Assert.Single(_model.Accounts);
    }

    [Fact]
    public async Task OnPostAsync_SwitchSucceeds_RedirectsToDashboard()
    {
        var accountId = Guid.CreateVersion7();
        var tokenId = Guid.CreateVersion7();

        _mockAuthService
            .Setup(s =>
                s.SwitchAccountAsync(It.Is<SwitchAccountRequest>(r => r.AccountId == accountId))
            )
            .ReturnsAsync(
                new SignInResult(SignInResultCode.Success, null, "switched-token", tokenId)
            );

        var result = await _model.OnPostAsync(accountId);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
    }
}
