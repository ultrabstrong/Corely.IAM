using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.UnitTests.Services;

public class MfaServiceAuthorizationDecoratorTests
{
    private readonly Mock<IMfaService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly MfaServiceAuthorizationDecorator _decorator;

    public MfaServiceAuthorizationDecoratorTests()
    {
        _decorator = new MfaServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object
        );
    }

    #region EnableTotpAsync

    [Fact]
    public async Task EnableTotp_DelegatesToInner_WhenHasUserContext()
    {
        var expectedResult = new EnableTotpResult(
            EnableTotpResultCode.Success,
            "",
            "secret",
            "qrCode",
            ["code1"]
        );
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.EnableTotpAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.EnableTotpAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.EnableTotpAsync(), Times.Once);
    }

    [Fact]
    public async Task EnableTotp_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.EnableTotpAsync();

        Assert.Equal(EnableTotpResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.EnableTotpAsync(), Times.Never);
    }

    #endregion

    #region ConfirmTotpAsync

    [Fact]
    public async Task ConfirmTotp_DelegatesToInner_WhenHasUserContext()
    {
        var request = new ConfirmTotpRequest("123456");
        var expectedResult = new ConfirmTotpResult(ConfirmTotpResultCode.Success, "");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.ConfirmTotpAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ConfirmTotpAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ConfirmTotpAsync(request), Times.Once);
    }

    [Fact]
    public async Task ConfirmTotp_ReturnsUnauthorized_WhenNoUserContext()
    {
        var request = new ConfirmTotpRequest("123456");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.ConfirmTotpAsync(request);

        Assert.Equal(ConfirmTotpResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ConfirmTotpAsync(It.IsAny<ConfirmTotpRequest>()),
            Times.Never
        );
    }

    #endregion

    #region DisableTotpAsync

    [Fact]
    public async Task DisableTotp_DelegatesToInner_WhenHasUserContext()
    {
        var request = new DisableTotpRequest("123456");
        var expectedResult = new DisableTotpResult(DisableTotpResultCode.Success, "");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.DisableTotpAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.DisableTotpAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DisableTotpAsync(request), Times.Once);
    }

    [Fact]
    public async Task DisableTotp_ReturnsUnauthorized_WhenNoUserContext()
    {
        var request = new DisableTotpRequest("123456");
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.DisableTotpAsync(request);

        Assert.Equal(DisableTotpResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.DisableTotpAsync(It.IsAny<DisableTotpRequest>()),
            Times.Never
        );
    }

    #endregion

    #region RegenerateTotpRecoveryCodesAsync

    [Fact]
    public async Task RegenerateTotpRecoveryCodes_DelegatesToInner_WhenHasUserContext()
    {
        var expectedResult = new RegenerateTotpRecoveryCodesResult(
            RegenerateTotpRecoveryCodesResultCode.Success,
            "",
            ["code1", "code2"]
        );
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.RegenerateTotpRecoveryCodesAsync())
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegenerateTotpRecoveryCodesAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegenerateTotpRecoveryCodesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegenerateTotpRecoveryCodes_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.RegenerateTotpRecoveryCodesAsync();

        Assert.Equal(RegenerateTotpRecoveryCodesResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.RegenerateTotpRecoveryCodesAsync(), Times.Never);
    }

    #endregion

    #region GetTotpStatusAsync

    [Fact]
    public async Task GetTotpStatus_DelegatesToInner_WhenHasUserContext()
    {
        var expectedResult = new TotpStatusResult(TotpStatusResultCode.Success, "", true, 5);
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.GetTotpStatusAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.GetTotpStatusAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetTotpStatusAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTotpStatus_ReturnsUnauthorized_WhenNoUserContext()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        var result = await _decorator.GetTotpStatusAsync();

        Assert.Equal(TotpStatusResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(x => x.GetTotpStatusAsync(), Times.Never);
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new MfaServiceAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new MfaServiceAuthorizationDecorator(_mockInnerService.Object, null!)
        );

    #endregion
}
