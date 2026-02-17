using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
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

public class SwitchAccountModelTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly SwitchAccountModel _model;

    public SwitchAccountModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _model = new SwitchAccountModel(
            _mockAuthService.Object,
            _mockCookieManager.Object,
            Options.Create(new SecurityOptions())
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(false);
        mockUrlHelper.Setup(u => u.IsLocalUrl("/local-path")).Returns(true);
        _model.Url = mockUrlHelper.Object;
    }

    [Fact]
    public async Task OnGetAsync_WithoutAccountId_RedirectsToDashboard()
    {
        var result = await _model.OnGetAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
    }

    [Fact]
    public async Task OnGetAsync_WithAccountId_SwitchesAndRedirectsToReturnUrl()
    {
        var accountId = Guid.CreateVersion7();
        var tokenId = Guid.CreateVersion7();

        _mockAuthService
            .Setup(s =>
                s.SwitchAccountAsync(It.Is<SwitchAccountRequest>(r => r.AccountId == accountId))
            )
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));

        var result = await _model.OnGetAsync(accountId, "/local-path");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/local-path", redirect.Url);
        _mockCookieManager.Verify(
            c =>
                c.SetAuthCookies(
                    It.IsAny<IResponseCookies>(),
                    "auth-token",
                    tokenId,
                    It.IsAny<bool>(),
                    It.IsAny<int>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task OnGetAsync_WithAccountId_SwitchFails_RedirectsToSelectAccount()
    {
        var accountId = Guid.CreateVersion7();

        _mockAuthService
            .Setup(s => s.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()))
            .ReturnsAsync(
                new SignInResult(SignInResultCode.AccountNotFoundError, "Not found", null, null)
            );

        var result = await _model.OnGetAsync(accountId, "/local-path");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_Success_WithLocalReturnUrl_RedirectsToReturnUrl()
    {
        var accountId = Guid.CreateVersion7();
        var tokenId = Guid.CreateVersion7();

        _mockAuthService
            .Setup(s =>
                s.SwitchAccountAsync(It.Is<SwitchAccountRequest>(r => r.AccountId == accountId))
            )
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));

        var result = await _model.OnPostAsync(accountId, "/local-path");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/local-path", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_Success_WithNonLocalReturnUrl_RedirectsToDashboard()
    {
        var accountId = Guid.CreateVersion7();
        var tokenId = Guid.CreateVersion7();

        _mockAuthService
            .Setup(s => s.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));

        var result = await _model.OnPostAsync(accountId, "https://evil.com/redirect");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_Success_WithNullReturnUrl_RedirectsToDashboard()
    {
        var accountId = Guid.CreateVersion7();
        var tokenId = Guid.CreateVersion7();

        _mockAuthService
            .Setup(s => s.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));

        var result = await _model.OnPostAsync(accountId, null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_Failure_RedirectsToSelectAccount()
    {
        var accountId = Guid.CreateVersion7();

        _mockAuthService
            .Setup(s => s.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()))
            .ReturnsAsync(
                new SignInResult(SignInResultCode.AccountNotFoundError, "Not found", null, null)
            );

        var result = await _model.OnPostAsync(accountId, "/local-path");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
    }
}
