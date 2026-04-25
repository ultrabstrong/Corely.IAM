using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;

namespace Corely.IAM.UnitTests.Services;

public class GoogleAuthServiceAuthorizationDecoratorTests
{
    private readonly Mock<IGoogleAuthService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly GoogleAuthServiceAuthorizationDecorator _decorator;

    public GoogleAuthServiceAuthorizationDecoratorTests()
    {
        _decorator = new GoogleAuthServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object
        );
    }

    #region LinkGoogleAuthAsync

    [Fact]
    public async Task LinkGoogleAuth_DelegatesToInner_WhenHasUserContext()
    {
        var request = new LinkGoogleAuthRequest("google-id-token");
        var expectedResult = new LinkGoogleAuthResult(LinkGoogleAuthResultCode.Success, "");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.LinkGoogleAuthAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.LinkGoogleAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.LinkGoogleAuthAsync(request), Times.Once);
    }

    [Fact]
    public async Task LinkGoogleAuth_ReturnsUnauthorized_WhenNoUserContext()
    {
        var request = new LinkGoogleAuthRequest("google-id-token");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.LinkGoogleAuthAsync(request);

        Assert.Equal(LinkGoogleAuthResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.LinkGoogleAuthAsync(It.IsAny<LinkGoogleAuthRequest>()),
            Times.Never
        );
    }

    #endregion

    #region UnlinkGoogleAuthAsync

    [Fact]
    public async Task UnlinkGoogleAuth_DelegatesToInner_WhenHasUserContext()
    {
        var expectedResult = new UnlinkGoogleAuthResult(UnlinkGoogleAuthResultCode.Success, "");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.UnlinkGoogleAuthAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.UnlinkGoogleAuthAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.UnlinkGoogleAuthAsync(), Times.Once);
    }

    [Fact]
    public async Task UnlinkGoogleAuth_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.UnlinkGoogleAuthAsync();

        Assert.Equal(UnlinkGoogleAuthResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.UnlinkGoogleAuthAsync(), Times.Never);
    }

    #endregion

    #region GetAuthMethodsAsync

    [Fact]
    public async Task GetAuthMethods_DelegatesToInner_WhenHasUserContext()
    {
        var expectedResult = new AuthMethodsResult(
            AuthMethodsResultCode.Success,
            "",
            true,
            false,
            null
        );
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.GetAuthMethodsAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.GetAuthMethodsAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetAuthMethodsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAuthMethods_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.GetAuthMethodsAsync();

        Assert.Equal(AuthMethodsResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.GetAuthMethodsAsync(), Times.Never);
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GoogleAuthServiceAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GoogleAuthServiceAuthorizationDecorator(_mockInnerService.Object, null!)
        );

    #endregion
}
