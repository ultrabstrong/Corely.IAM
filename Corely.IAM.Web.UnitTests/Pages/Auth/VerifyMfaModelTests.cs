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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using SignInResult = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.UnitTests.Pages.Auth;

public class VerifyMfaModelTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IPostAuthenticationFlowService> _mockPostAuthenticationFlowService;
    private readonly VerifyMfaModel _model;

    public VerifyMfaModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockPostAuthenticationFlowService = new Mock<IPostAuthenticationFlowService>();
        _model = new VerifyMfaModel(
            _mockAuthService.Object,
            _mockPostAuthenticationFlowService.Object,
            Options.Create(new SecurityOptions())
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
        _model.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );
    }

    [Fact]
    public void OnGet_WithNoChallengeToken_RedirectsToSignIn()
    {
        var result = _model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }

    [Fact]
    public void OnGet_WithChallengeToken_ReturnsPage()
    {
        _model.TempData["MfaChallengeToken"] = "test-challenge-token";

        var result = _model.OnGet();

        Assert.IsType<PageResult>(result);
        Assert.Equal("test-challenge-token", _model.MfaChallengeToken);
    }

    [Fact]
    public async Task OnPost_WithValidCode_SetsCookiesAndRedirects()
    {
        var tokenId = Guid.CreateVersion7();
        var signInResult = new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId);
        _model.MfaChallengeToken = "test-challenge-token";
        _model.Code = "123456";
        _mockAuthService
            .Setup(s => s.VerifyMfaAsync(It.IsAny<VerifyMfaRequest>()))
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
    public async Task OnPost_WithExpiredChallenge_RedirectsToSignIn()
    {
        _model.MfaChallengeToken = "expired-token";
        _model.Code = "123456";
        _mockAuthService
            .Setup(s => s.VerifyMfaAsync(It.IsAny<VerifyMfaRequest>()))
            .ReturnsAsync(
                new SignInResult(
                    SignInResultCode.MfaChallengeExpiredError,
                    "Challenge expired",
                    null,
                    null
                )
            );

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }

    [Fact]
    public async Task OnPost_WithInvalidCode_ReturnsPageWithError()
    {
        _model.MfaChallengeToken = "test-challenge-token";
        _model.Code = "000000";
        _mockAuthService
            .Setup(s => s.VerifyMfaAsync(It.IsAny<VerifyMfaRequest>()))
            .ReturnsAsync(
                new SignInResult(SignInResultCode.InvalidMfaCodeError, "Invalid code", null, null)
            );

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Invalid code. Please try again.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPost_WithEmptyCode_ReturnsPageWithError()
    {
        _model.MfaChallengeToken = "test-challenge-token";
        _model.Code = "";

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Please enter a code.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPost_WithNoChallengeToken_RedirectsToSignIn()
    {
        _model.MfaChallengeToken = "";
        _model.Code = "123456";

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }

    [Fact]
    public async Task OnPost_WithValidCode_OneAccount_AutoSwitchesAndRedirects()
    {
        var tokenId = Guid.CreateVersion7();
        var signInResult = new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId);
        _model.MfaChallengeToken = "test-challenge-token";
        _model.Code = "123456";
        _mockAuthService
            .Setup(s => s.VerifyMfaAsync(It.IsAny<VerifyMfaRequest>()))
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
    public async Task OnPost_WithValidCode_MultipleAccounts_RedirectsToSelectAccount()
    {
        var tokenId = Guid.CreateVersion7();
        var signInResult = new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId);
        _model.MfaChallengeToken = "test-challenge-token";
        _model.Code = "123456";
        _mockAuthService
            .Setup(s => s.VerifyMfaAsync(It.IsAny<VerifyMfaRequest>()))
            .ReturnsAsync(signInResult);
        _mockPostAuthenticationFlowService
            .Setup(s => s.CompleteSignInAsync(_model.HttpContext, signInResult, It.IsAny<int>()))
            .ReturnsAsync(new RedirectResult(AppRoutes.SelectAccount));

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SelectAccount, redirect.Url);
    }
}
