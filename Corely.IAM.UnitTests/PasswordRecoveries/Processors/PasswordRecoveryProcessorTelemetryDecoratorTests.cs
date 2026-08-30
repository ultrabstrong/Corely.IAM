using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.PasswordRecoveries.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.PasswordRecoveries.Processors;

public class PasswordRecoveryProcessorTelemetryDecoratorTests
{
    private readonly Mock<IPasswordRecoveryProcessor> _mockInnerProcessor = new();
    private readonly Mock<ILogger<PasswordRecoveryProcessorTelemetryDecorator>> _mockLogger = new();
    private readonly PasswordRecoveryProcessorTelemetryDecorator _decorator;

    public PasswordRecoveryProcessorTelemetryDecoratorTests()
    {
        _decorator = new PasswordRecoveryProcessorTelemetryDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new PasswordRecoveryProcessorTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new PasswordRecoveryProcessorTelemetryDecorator(_mockInnerProcessor.Object, null!)
        );

    [Fact]
    public async Task RequestPasswordRecovery_DelegatesToInnerAndLogsResult()
    {
        var request = new RequestPasswordRecoveryRequest("user@example.com");
        var expectedResult = new RequestPasswordRecoveryResult(
            RequestPasswordRecoveryResultCode.Success,
            string.Empty,
            "token"
        );
        _mockInnerProcessor
            .Setup(x => x.RequestPasswordRecoveryAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RequestPasswordRecoveryAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.RequestPasswordRecoveryAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ValidatePasswordRecoveryToken_DelegatesToInnerAndLogsResult()
    {
        var request = new ValidatePasswordRecoveryTokenRequest("token");
        var expectedResult = new ValidatePasswordRecoveryTokenResult(
            ValidatePasswordRecoveryTokenResultCode.Success,
            string.Empty
        );
        _mockInnerProcessor
            .Setup(x => x.ValidatePasswordRecoveryTokenAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ValidatePasswordRecoveryTokenAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.ValidatePasswordRecoveryTokenAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ValidatePasswordRecoveryToken_LogsFailureResult()
    {
        var request = new ValidatePasswordRecoveryTokenRequest("expired");
        var expectedResult = new ValidatePasswordRecoveryTokenResult(
            ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryExpiredError,
            "expired"
        );
        _mockInnerProcessor
            .Setup(x => x.ValidatePasswordRecoveryTokenAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ValidatePasswordRecoveryTokenAsync(request);

        Assert.Equal(expectedResult, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ResetPasswordWithRecovery_DelegatesToInnerAndLogsResult()
    {
        var request = new ResetPasswordWithRecoveryRequest("token", "password");
        var expectedResult = new ResetPasswordWithRecoveryResult(
            ResetPasswordWithRecoveryResultCode.Success,
            string.Empty
        );
        _mockInnerProcessor
            .Setup(x => x.ResetPasswordWithRecoveryAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ResetPasswordWithRecoveryAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.ResetPasswordWithRecoveryAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RequestPasswordRecovery_DoesNotSwallowExceptions()
    {
        var request = new RequestPasswordRecoveryRequest("user@example.com");
        _mockInnerProcessor
            .Setup(x => x.RequestPasswordRecoveryAsync(request))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _decorator.RequestPasswordRecoveryAsync(request)
        );
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
