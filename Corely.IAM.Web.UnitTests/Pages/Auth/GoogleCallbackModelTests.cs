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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SignInResult = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.UnitTests.Pages.Auth;

public class GoogleCallbackModelTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IUserContextProvider> _mockUserContextProvider;
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly GoogleCallbackModel _model;

    public GoogleCallbackModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockUserContextProvider = new Mock<IUserContextProvider>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _model = new GoogleCallbackModel(
            _mockAuthService.Object,
            _mockUserContextProvider.Object,
            _mockCookieManager.Object,
            Options.Create(new SecurityOptions())
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
        _model.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );
    }

    [Fact]
    public void OnGet_RedirectsToSignIn()
    {
        var result = _model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.SignIn, redirect.Url);
    }

    [Fact]
    public async Task OnPost_WithNoCredential_ReturnsPageWithError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("No credential received from Google.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPost_WithValidCredential_SetsCookiesAndRedirects()
    {
        var tokenId = Guid.CreateVersion7();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(
            new Dictionary<string, StringValues> { { "credential", "test-google-token" } }
        );
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);
        _mockCookieManager
            .Setup(c => c.GetOrCreateDeviceId(It.IsAny<HttpContext>()))
            .Returns("device-1");
        _mockAuthService
            .Setup(s => s.SignInWithGoogleAsync(It.IsAny<SignInWithGoogleRequest>()))
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
    public async Task OnPost_WithMfaRequired_RedirectsToVerifyMfa()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(
            new Dictionary<string, StringValues> { { "credential", "test-google-token" } }
        );
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);
        _model.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        _mockCookieManager
            .Setup(c => c.GetOrCreateDeviceId(It.IsAny<HttpContext>()))
            .Returns("device-1");
        _mockAuthService
            .Setup(s => s.SignInWithGoogleAsync(It.IsAny<SignInWithGoogleRequest>()))
            .ReturnsAsync(
                new SignInResult(
                    SignInResultCode.MfaRequiredChallenge,
                    null,
                    null,
                    null,
                    "mfa-challenge-token"
                )
            );

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.VerifyMfa, redirect.Url);
    }

    [Fact]
    public async Task OnPost_WithInvalidToken_ReturnsPageWithError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(
            new Dictionary<string, StringValues> { { "credential", "invalid-google-token" } }
        );
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);
        _mockCookieManager
            .Setup(c => c.GetOrCreateDeviceId(It.IsAny<HttpContext>()))
            .Returns("device-1");
        _mockAuthService
            .Setup(s => s.SignInWithGoogleAsync(It.IsAny<SignInWithGoogleRequest>()))
            .ReturnsAsync(
                new SignInResult(
                    SignInResultCode.InvalidGoogleTokenError,
                    "Invalid token",
                    null,
                    null
                )
            );

        var result = await _model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Google authentication failed. Please try again.", _model.ErrorMessage);
    }

    [Fact]
    public async Task OnPost_WithUnlinkedGoogle_ReturnsPageWithError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(
            new Dictionary<string, StringValues> { { "credential", "unlinked-google-token" } }
        );
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);
        _mockCookieManager
            .Setup(c => c.GetOrCreateDeviceId(It.IsAny<HttpContext>()))
            .Returns("device-1");
        _mockAuthService
            .Setup(s => s.SignInWithGoogleAsync(It.IsAny<SignInWithGoogleRequest>()))
            .ReturnsAsync(
                new SignInResult(
                    SignInResultCode.GoogleAuthNotLinkedError,
                    "Not linked",
                    null,
                    null
                )
            );

        var result = await _model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(AppRoutes.RegisterWithGoogle, redirect.Url);
    }

    [Fact]
    public async Task OnPost_WithValidCredential_OneAccount_AutoSwitchesAndRedirects()
    {
        var tokenId = Guid.CreateVersion7();
        var switchTokenId = Guid.CreateVersion7();
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(
            new Dictionary<string, StringValues> { { "credential", "test-google-token" } }
        );
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);
        _mockCookieManager
            .Setup(c => c.GetOrCreateDeviceId(It.IsAny<HttpContext>()))
            .Returns("device-1");
        _mockAuthService
            .Setup(s => s.SignInWithGoogleAsync(It.IsAny<SignInWithGoogleRequest>()))
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
    public async Task OnPost_WithValidCredential_MultipleAccounts_RedirectsToSelectAccount()
    {
        var tokenId = Guid.CreateVersion7();
        var account1 = new Account { Id = Guid.CreateVersion7(), AccountName = "Account1" };
        var account2 = new Account { Id = Guid.CreateVersion7(), AccountName = "Account2" };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(
            new Dictionary<string, StringValues> { { "credential", "test-google-token" } }
        );
        _model.PageContext = PageTestHelpers.CreatePageContext(httpContext);
        _mockCookieManager
            .Setup(c => c.GetOrCreateDeviceId(It.IsAny<HttpContext>()))
            .Returns("device-1");
        _mockAuthService
            .Setup(s => s.SignInWithGoogleAsync(It.IsAny<SignInWithGoogleRequest>()))
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
}
