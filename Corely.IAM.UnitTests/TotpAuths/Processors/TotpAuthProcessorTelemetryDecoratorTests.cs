using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.TotpAuths.Processors;

public class TotpAuthProcessorTelemetryDecoratorTests
{
    private readonly Mock<ITotpAuthProcessor> _mockInnerProcessor = new();
    private readonly Mock<ILogger<TotpAuthProcessorTelemetryDecorator>> _mockLogger = new();
    private readonly TotpAuthProcessorTelemetryDecorator _decorator;

    public TotpAuthProcessorTelemetryDecoratorTests()
    {
        _decorator = new TotpAuthProcessorTelemetryDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task EnableTotpAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expected = new EnableTotpResult(
            EnableTotpResultCode.Success,
            string.Empty,
            "secret",
            "setup-uri",
            ["ABCD-EFGH"]
        );
        _mockInnerProcessor
            .Setup(x => x.EnableTotpAsync(userId, "issuer", "label"))
            .ReturnsAsync(expected);

        var result = await _decorator.EnableTotpAsync(userId, "issuer", "label");

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ConfirmTotpAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expected = new ConfirmTotpResult(ConfirmTotpResultCode.Success, string.Empty);
        _mockInnerProcessor.Setup(x => x.ConfirmTotpAsync(userId, "123456")).ReturnsAsync(expected);

        var result = await _decorator.ConfirmTotpAsync(userId, "123456");

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DisableTotpAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expected = new DisableTotpResult(DisableTotpResultCode.Success, string.Empty);
        _mockInnerProcessor.Setup(x => x.DisableTotpAsync(userId, "123456")).ReturnsAsync(expected);

        var result = await _decorator.DisableTotpAsync(userId, "123456");

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetTotpStatusAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expected = new TotpStatusResult(TotpStatusResultCode.Success, string.Empty, true, 10);
        _mockInnerProcessor.Setup(x => x.GetTotpStatusAsync(userId)).ReturnsAsync(expected);

        var result = await _decorator.GetTotpStatusAsync(userId);

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegenerateTotpRecoveryCodesAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expected = new RegenerateTotpRecoveryCodesResult(
            RegenerateTotpRecoveryCodesResultCode.Success,
            string.Empty,
            ["ABCD-EFGH"]
        );
        _mockInnerProcessor
            .Setup(x => x.RegenerateTotpRecoveryCodesAsync(userId))
            .ReturnsAsync(expected);

        var result = await _decorator.RegenerateTotpRecoveryCodesAsync(userId);

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task VerifyTotpOrRecoveryCodeAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new VerifyTotpOrRecoveryCodeRequest(Guid.CreateVersion7(), "123456");
        var expected = new VerifyTotpOrRecoveryCodeResult(
            VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid,
            string.Empty
        );
        _mockInnerProcessor
            .Setup(x => x.VerifyTotpOrRecoveryCodeAsync(request))
            .ReturnsAsync(expected);

        var result = await _decorator.VerifyTotpOrRecoveryCodeAsync(request);

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task IsTotpEnabledAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        _mockInnerProcessor.Setup(x => x.IsTotpEnabledAsync(userId)).ReturnsAsync(true);

        var result = await _decorator.IsTotpEnabledAsync(userId);

        Assert.True(result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new TotpAuthProcessorTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new TotpAuthProcessorTelemetryDecorator(_mockInnerProcessor.Object, null!)
        );

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
