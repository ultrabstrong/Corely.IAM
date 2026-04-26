using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Pages.Authentication;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
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
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly Mock<IPostAuthenticationFlowService> _mockPostAuthenticationFlowService;
    private readonly SignInModel _model;

    public SignInModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _mockPostAuthenticationFlowService = new Mock<IPostAuthenticationFlowService>();
        _model = new SignInModel(
            _mockAuthService.Object,
            _mockCookieManager.Object,
            _mockPostAuthenticationFlowService.Object,
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
    public async Task OnPost_WithEmptyCredentials_ReturnsPageWithError()
    {
        _model.Username = "";
        _model.Password = "";

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Username and password are required.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPost_WithInvalidCredentials_ReturnsPageWithError()
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
    public async Task OnPost_WithValidCredentials_NoAccounts_RedirectsToDashboard()
    {
        _model.Username = "testuser";
        _model.Password = "goodpass";
        var tokenId = Guid.CreateVersion7();
        var signInResult = new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId);
        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(signInResult);
        _mockPostAuthenticationFlowService
            .Setup(s => s.CompleteSignInAsync(_model.HttpContext, signInResult, It.IsAny<int>()))
            .ReturnsAsync(new RedirectResult(AppRoutes.Dashboard));

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
        _mockPostAuthenticationFlowService.Verify(
            s => s.CompleteSignInAsync(_model.HttpContext, signInResult, It.IsAny<int>()),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPost_WithValidCredentials_DelegatesToPostAuthenticationFlow()
    {
        _model.Username = "testuser";
        _model.Password = "goodpass";
        var tokenId = Guid.CreateVersion7();
        var signInResult = new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId);

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(signInResult);
        _mockPostAuthenticationFlowService
            .Setup(s => s.CompleteSignInAsync(_model.HttpContext, signInResult, It.IsAny<int>()))
            .ReturnsAsync(new RedirectResult(AppRoutes.Dashboard));

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
        _mockPostAuthenticationFlowService.Verify(
            s => s.CompleteSignInAsync(_model.HttpContext, signInResult, It.IsAny<int>()),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPost_WithValidCredentials_MultipleAccounts_RedirectsToSelectAccount()
    {
        _model.Username = "testuser";
        _model.Password = "goodpass";
        var tokenId = Guid.CreateVersion7();
        var signInResult = new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId);

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(signInResult);
        _mockPostAuthenticationFlowService
            .Setup(s => s.CompleteSignInAsync(_model.HttpContext, signInResult, It.IsAny<int>()))
            .ReturnsAsync(new RedirectResult(AppRoutes.SelectAccount));

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
    }

    [Fact]
    public async Task OnPost_WithValidCredentials_OneAccount_SwitchFails_RedirectsToSelectAccount()
    {
        _model.Username = "testuser";
        _model.Password = "goodpass";
        var tokenId = Guid.CreateVersion7();
        var signInResult = new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId);

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(signInResult);
        _mockPostAuthenticationFlowService
            .Setup(s => s.CompleteSignInAsync(_model.HttpContext, signInResult, It.IsAny<int>()))
            .ReturnsAsync(new RedirectResult(AppRoutes.SelectAccount));

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
    }
}
