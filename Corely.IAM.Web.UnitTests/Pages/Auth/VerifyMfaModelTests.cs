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
using SignInResult = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.UnitTests.Pages.Auth;

public class VerifyMfaModelTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IUserContextProvider> _mockUserContextProvider;
    private readonly Mock<IAuthCookieManager> _mockCookieManager;
    private readonly VerifyMfaModel _model;

    public VerifyMfaModelTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockUserContextProvider = new Mock<IUserContextProvider>();
        _mockCookieManager = new Mock<IAuthCookieManager>();
        _model = new VerifyMfaModel(
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
        _model.MfaChallengeToken = "test-challenge-token";
        _model.Code = "123456";
        _mockAuthService
            .Setup(s => s.VerifyMfaAsync(It.IsAny<VerifyMfaRequest>()))
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
        var switchTokenId = Guid.CreateVersion7();
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        _model.MfaChallengeToken = "test-challenge-token";
        _model.Code = "123456";
        _mockAuthService
            .Setup(s => s.VerifyMfaAsync(It.IsAny<VerifyMfaRequest>()))
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
    public async Task OnPost_WithValidCode_MultipleAccounts_RedirectsToSelectAccount()
    {
        var tokenId = Guid.CreateVersion7();
        var account1 = new Account { Id = Guid.CreateVersion7(), AccountName = "Account1" };
        var account2 = new Account { Id = Guid.CreateVersion7(), AccountName = "Account2" };
        _model.MfaChallengeToken = "test-challenge-token";
        _model.Code = "123456";
        _mockAuthService
            .Setup(s => s.VerifyMfaAsync(It.IsAny<VerifyMfaRequest>()))
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
