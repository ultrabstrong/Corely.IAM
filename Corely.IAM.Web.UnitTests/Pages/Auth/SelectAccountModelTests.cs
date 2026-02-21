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
    private readonly Mock<IRetrievalService> _mockRetrievalService;
    private readonly SelectAccountModel _model;

    public SelectAccountModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockUserContextProvider = new Mock<IUserContextProvider>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _mockRetrievalService = new Mock<IRetrievalService>();
        _model = new SelectAccountModel(
            _mockAuthService.Object,
            _mockUserContextProvider.Object,
            _mockCookieManager.Object,
            Options.Create(new SecurityOptions()),
            _mockRetrievalService.Object
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    private void SetupListAccounts(List<Account> accounts)
    {
        var pagedResult = PagedResult<Account>.Create(accounts, accounts.Count, 0, 25);
        _mockRetrievalService
            .Setup(s =>
                s.ListAccountsAsync(
                    It.IsAny<Corely.Common.Filtering.FilterBuilder<Account>?>(),
                    It.IsAny<Corely.Common.Filtering.Ordering.OrderBuilder<Account>?>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                )
            )
            .ReturnsAsync(
                new RetrieveListResult<Account>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    pagedResult
                )
            );
    }

    [Fact]
    public async Task OnGetAsync_NoUserContext_RedirectsToSignIn()
    {
        _mockUserContextProvider.Setup(p => p.GetUserContext()).Returns((UserContext?)null);

        var result = await _model.OnGetAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }

    [Fact]
    public async Task OnGetAsync_HasUserContext_ReturnsPageWithAccountsFromService()
    {
        var accounts = new List<Account>
        {
            new() { Id = Guid.CreateVersion7(), AccountName = "Account1" },
            new() { Id = Guid.CreateVersion7(), AccountName = "Account2" },
        };
        _mockUserContextProvider
            .Setup(p => p.GetUserContext())
            .Returns(PageTestHelpers.CreateUserContext(availableAccounts: accounts));
        SetupListAccounts(accounts);

        var result = await _model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(2, _model.Accounts.Count);
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
        SetupListAccounts(accounts);

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
