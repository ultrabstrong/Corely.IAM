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

public class SignInModelTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IUserContextProvider> _mockUserContextProvider;
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly SignInModel _model;

    public SignInModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockUserContextProvider = new Mock<IUserContextProvider>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _model = new SignInModel(
            _mockAuthService.Object,
            _mockUserContextProvider.Object,
            _mockCookieManager.Object,
            Options.Create(new SecurityOptions())
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public void OnGet_WithAuthCookie_RedirectsToDashboard()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = "authentication_token=some-token";
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);

        var result = _model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
    }

    [Fact]
    public void OnGet_WithoutAuthCookie_ReturnsPage()
    {
        var result = _model.OnGet();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_WithEmptyCredentials_ReturnsPageWithError()
    {
        _model.Username = "";
        _model.Password = "";

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Username and password are required.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_WithInvalidCredentials_ReturnsPageWithError()
    {
        _model.Username = "testuser";
        _model.Password = "wrongpass";
        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(
                new SignInResult(SignInResultCode.PasswordMismatchError, "Bad password", null, null)
            );

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Invalid username or password.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_WithValidCredentials_NoAccounts_RedirectsToDashboard()
    {
        _model.Username = "testuser";
        _model.Password = "goodpass";
        var tokenId = Guid.CreateVersion7();
        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));
        _mockUserContextProvider
            .Setup(p => p.GetUserContext())
            .Returns(
                new UserContext(
                    new User
                    {
                        Id = Guid.CreateVersion7(),
                        Username = "testuser",
                        Email = "test@test.com",
                    },
                    null,
                    "device-1",
                    []
                )
            );

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_WithValidCredentials_OneAccount_AutoSwitchesAndRedirectsToDashboard()
    {
        _model.Username = "testuser";
        _model.Password = "goodpass";
        var tokenId = Guid.CreateVersion7();
        var switchTokenId = Guid.CreateVersion7();
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));
        _mockAuthService
            .Setup(s =>
                s.SwitchAccountAsync(It.Is<SwitchAccountRequest>(r => r.AccountId == account.Id))
            )
            .ReturnsAsync(
                new SignInResult(SignInResultCode.Success, null, "switched-token", switchTokenId)
            );
        _mockUserContextProvider
            .Setup(p => p.GetUserContext())
            .Returns(
                new UserContext(
                    new User
                    {
                        Id = Guid.CreateVersion7(),
                        Username = "testuser",
                        Email = "test@test.com",
                    },
                    null,
                    "device-1",
                    [account]
                )
            );

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
        _mockAuthService.Verify(
            s => s.SwitchAccountAsync(It.Is<SwitchAccountRequest>(r => r.AccountId == account.Id)),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPostAsync_WithValidCredentials_MultipleAccounts_RedirectsToSelectAccount()
    {
        _model.Username = "testuser";
        _model.Password = "goodpass";
        var tokenId = Guid.CreateVersion7();
        var account1 = new Account { Id = Guid.CreateVersion7(), AccountName = "Account1" };
        var account2 = new Account { Id = Guid.CreateVersion7(), AccountName = "Account2" };

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));
        _mockUserContextProvider
            .Setup(p => p.GetUserContext())
            .Returns(
                new UserContext(
                    new User
                    {
                        Id = Guid.CreateVersion7(),
                        Username = "testuser",
                        Email = "test@test.com",
                    },
                    null,
                    "device-1",
                    [account1, account2]
                )
            );

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_WithValidCredentials_OneAccount_SwitchFails_RedirectsToSelectAccount()
    {
        _model.Username = "testuser";
        _model.Password = "goodpass";
        var tokenId = Guid.CreateVersion7();
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));
        _mockAuthService
            .Setup(s => s.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()))
            .ReturnsAsync(
                new SignInResult(SignInResultCode.AccountNotFoundError, "Not found", null, null)
            );
        _mockUserContextProvider
            .Setup(p => p.GetUserContext())
            .Returns(
                new UserContext(
                    new User
                    {
                        Id = Guid.CreateVersion7(),
                        Username = "testuser",
                        Email = "test@test.com",
                    },
                    null,
                    "device-1",
                    [account]
                )
            );

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
    }
}
