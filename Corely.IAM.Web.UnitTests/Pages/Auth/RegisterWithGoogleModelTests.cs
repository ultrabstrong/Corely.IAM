using Corely.IAM.GoogleAuths.Models;
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

public class RegisterWithGoogleModelTests
{
    private readonly Mock<IRegistrationService> _mockRegistrationService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly Mock<IPostAuthenticationFlowService> _mockPostAuthenticationFlowService;
    private readonly RegisterWithGoogleModel _model;

    public RegisterWithGoogleModelTests()
    {
        _mockRegistrationService = new Mock<IRegistrationService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _mockPostAuthenticationFlowService = new Mock<IPostAuthenticationFlowService>();
        _model = new RegisterWithGoogleModel(
            _mockRegistrationService.Object,
            _mockAuthService.Object,
            _mockCookieManager.Object,
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
    public void OnGet_WithNoGoogleIdToken_RedirectsToSignIn()
    {
        var result = _model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }

    [Fact]
    public void OnGet_WithGoogleIdToken_ReturnsPage()
    {
        _model.TempData["GoogleIdToken"] = "some-google-token";

        var result = _model.OnGet();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPost_WithSuccess_SignsInAndRedirectsToDashboard()
    {
        _model.TempData["GoogleIdToken"] = "valid-google-token";
        var signInResult = new SignInResult(
            SignInResultCode.Success,
            null,
            "token",
            Guid.CreateVersion7()
        );

        _mockRegistrationService
            .Setup(s => s.RegisterUserWithGoogleAsync(It.IsAny<RegisterUserWithGoogleRequest>()))
            .ReturnsAsync(
                new RegisterUserWithGoogleResult(
                    RegisterUserWithGoogleResultCode.Success,
                    "",
                    Guid.CreateVersion7()
                )
            );
        _mockAuthService
            .Setup(s => s.SignInWithGoogleAsync(It.IsAny<SignInWithGoogleRequest>()))
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
    public async Task OnPost_WithInvalidToken_ReturnsPageWithError()
    {
        _model.TempData["GoogleIdToken"] = "invalid-google-token";

        _mockRegistrationService
            .Setup(s => s.RegisterUserWithGoogleAsync(It.IsAny<RegisterUserWithGoogleRequest>()))
            .ReturnsAsync(
                new RegisterUserWithGoogleResult(
                    RegisterUserWithGoogleResultCode.InvalidGoogleTokenError,
                    "Invalid token",
                    Guid.Empty
                )
            );

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Google authentication expired. Please try again.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPost_WithGoogleAccountInUse_ReturnsPageWithError()
    {
        _model.TempData["GoogleIdToken"] = "in-use-google-token";

        _mockRegistrationService
            .Setup(s => s.RegisterUserWithGoogleAsync(It.IsAny<RegisterUserWithGoogleRequest>()))
            .ReturnsAsync(
                new RegisterUserWithGoogleResult(
                    RegisterUserWithGoogleResultCode.GoogleAccountInUseError,
                    "Already linked",
                    Guid.Empty
                )
            );

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("This Google account is already linked to another user.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPost_WithNoGoogleIdToken_RedirectsToSignIn()
    {
        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }
}
