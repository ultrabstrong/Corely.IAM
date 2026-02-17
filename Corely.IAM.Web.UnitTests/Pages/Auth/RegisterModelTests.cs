using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Pages.Authentication;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.UnitTests.Helpers;
using Corely.Security.Password;
using Corely.Security.PasswordValidation.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SignInResult = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.UnitTests.Pages.Auth;

public class RegisterModelTests
{
    private readonly Mock<IRegistrationService> _mockRegistrationService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly RegisterModel _model;

    public RegisterModelTests()
    {
        _mockRegistrationService = new Mock<IRegistrationService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _model = new RegisterModel(
            _mockRegistrationService.Object,
            _mockAuthService.Object,
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
    public async Task OnPostAsync_PasswordMismatch_ReturnsPageWithError()
    {
        _model.Username = "testuser";
        _model.Email = "test@test.com";
        _model.Password = "password1";
        _model.ConfirmPassword = "password2";

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Passwords do not match.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_RegistrationFails_ReturnsPageWithError()
    {
        _model.Username = "testuser";
        _model.Email = "test@test.com";
        _model.Password = "password1";
        _model.ConfirmPassword = "password1";
        _mockRegistrationService
            .Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>()))
            .ReturnsAsync(
                new RegisterUserResult(
                    RegisterUserResultCode.UserCreationError,
                    "Username already taken",
                    Guid.Empty
                )
            );

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Username already taken", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_PasswordValidationFails_ReturnsPageWithError()
    {
        _model.Username = "testuser";
        _model.Email = "test@test.com";
        _model.Password = "weak";
        _model.ConfirmPassword = "weak";
        _mockRegistrationService
            .Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>()))
            .ThrowsAsync(
                new PasswordValidationException(
                    new PasswordValidationResult(false, ["Password must be at least 8 characters"]),
                    "Password validation failed"
                )
            );

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("at least 8 characters", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_Success_AutoSignsInAndRedirectsToDashboard()
    {
        _model.Username = "testuser";
        _model.Email = "test@test.com";
        _model.Password = "password1";
        _model.ConfirmPassword = "password1";
        var tokenId = Guid.CreateVersion7();

        _mockRegistrationService
            .Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>()))
            .ReturnsAsync(
                new RegisterUserResult(RegisterUserResultCode.Success, null, Guid.CreateVersion7())
            );
        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "auth-token", tokenId));

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.Dashboard, redirect.Url);
        _mockAuthService.Verify(
            s =>
                s.SignInAsync(
                    It.Is<SignInRequest>(r => r.Username == "testuser" && r.Password == "password1")
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPostAsync_Success_AutoSignInFails_RedirectsToSignIn()
    {
        _model.Username = "testuser";
        _model.Email = "test@test.com";
        _model.Password = "password1";
        _model.ConfirmPassword = "password1";

        _mockRegistrationService
            .Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>()))
            .ReturnsAsync(
                new RegisterUserResult(RegisterUserResultCode.Success, null, Guid.CreateVersion7())
            );
        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(
                new SignInResult(SignInResultCode.PasswordMismatchError, "Failed", null, null)
            );

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }
}
