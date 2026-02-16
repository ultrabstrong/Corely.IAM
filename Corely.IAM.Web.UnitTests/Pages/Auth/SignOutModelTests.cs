using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Pages.Authentication;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Auth;

public class SignOutModelTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly SignOutModel _model;

    public SignOutModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _model = new SignOutModel(_mockAuthService.Object, _mockCookieManager.Object);
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public void OnGet_RedirectsToDashboard()
    {
        var result = _model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_WithTokenId_CallsSignOutAndRedirectsToSignIn()
    {
        var tokenId = Guid.CreateVersion7().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = $"auth_token_id={tokenId}";
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);

        _mockAuthService.Setup(s => s.SignOutAsync(It.IsAny<SignOutRequest>())).ReturnsAsync(true);

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
        _mockAuthService.Verify(
            s => s.SignOutAsync(It.Is<SignOutRequest>(r => r.TokenId == tokenId)),
            Times.Once
        );
        _mockCookieManager.Verify(
            m => m.DeleteAuthCookies(It.IsAny<IResponseCookies>()),
            Times.Once
        );
        _mockCookieManager.Verify(
            m => m.DeleteDeviceIdCookie(It.IsAny<IResponseCookies>()),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPostAsync_WithoutTokenId_SkipsSignOutAndRedirectsToSignIn()
    {
        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
        _mockAuthService.Verify(s => s.SignOutAsync(It.IsAny<SignOutRequest>()), Times.Never);
        _mockCookieManager.Verify(
            m => m.DeleteAuthCookies(It.IsAny<IResponseCookies>()),
            Times.Once
        );
        _mockCookieManager.Verify(
            m => m.DeleteDeviceIdCookie(It.IsAny<IResponseCookies>()),
            Times.Once
        );
    }
}
