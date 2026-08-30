using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class MfaServiceTelemetryDecoratorTests
{
    private readonly Mock<IMfaService> _mockInnerService = new();
    private readonly Mock<ILogger<MfaServiceTelemetryDecorator>> _mockLogger = new();
    private readonly MfaServiceTelemetryDecorator _decorator;

    public MfaServiceTelemetryDecoratorTests()
    {
        _decorator = new MfaServiceTelemetryDecorator(_mockInnerService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new MfaServiceTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new MfaServiceTelemetryDecorator(_mockInnerService.Object, null!)
        );

    [Fact]
    public async Task EnableTotp_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new EnableTotpResult(
            EnableTotpResultCode.Success,
            string.Empty,
            "secret",
            "otpauth://uri",
            ["code1", "code2"]
        );
        _mockInnerService.Setup(x => x.EnableTotpAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.EnableTotpAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.EnableTotpAsync(), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ConfirmTotp_DelegatesToInnerAndLogsResult()
    {
        var request = new ConfirmTotpRequest("123456");
        var expectedResult = new ConfirmTotpResult(ConfirmTotpResultCode.Success, string.Empty);
        _mockInnerService.Setup(x => x.ConfirmTotpAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ConfirmTotpAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ConfirmTotpAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ConfirmTotp_LogsFailureResult()
    {
        var request = new ConfirmTotpRequest("000000");
        var expectedResult = new ConfirmTotpResult(
            ConfirmTotpResultCode.InvalidCodeError,
            "invalid"
        );
        _mockInnerService.Setup(x => x.ConfirmTotpAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ConfirmTotpAsync(request);

        Assert.Equal(expectedResult, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DisableTotp_DelegatesToInnerAndLogsResult()
    {
        var request = new DisableTotpRequest("123456");
        var expectedResult = new DisableTotpResult(DisableTotpResultCode.Success, string.Empty);
        _mockInnerService.Setup(x => x.DisableTotpAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.DisableTotpAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DisableTotpAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegenerateTotpRecoveryCodes_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RegenerateTotpRecoveryCodesResult(
            RegenerateTotpRecoveryCodesResultCode.Success,
            string.Empty,
            ["code1", "code2"]
        );
        _mockInnerService
            .Setup(x => x.RegenerateTotpRecoveryCodesAsync())
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegenerateTotpRecoveryCodesAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegenerateTotpRecoveryCodesAsync(), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetTotpStatus_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new TotpStatusResult(
            TotpStatusResultCode.Success,
            string.Empty,
            true,
            5
        );
        _mockInnerService.Setup(x => x.GetTotpStatusAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.GetTotpStatusAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetTotpStatusAsync(), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task EnableTotp_DoesNotSwallowExceptions()
    {
        _mockInnerService
            .Setup(x => x.EnableTotpAsync())
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _decorator.EnableTotpAsync());
    }

    private void VerifyLoggedWithResult() =>
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("with result")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
}
