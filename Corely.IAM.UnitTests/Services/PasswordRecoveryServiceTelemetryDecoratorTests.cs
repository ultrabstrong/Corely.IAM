using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class PasswordRecoveryServiceTelemetryDecoratorTests
{
    private readonly Mock<IPasswordRecoveryService> _mockInnerService = new();
    private readonly Mock<ILogger<PasswordRecoveryServiceTelemetryDecorator>> _mockLogger = new();
    private readonly PasswordRecoveryServiceTelemetryDecorator _decorator;

    public PasswordRecoveryServiceTelemetryDecoratorTests()
    {
        _decorator = new PasswordRecoveryServiceTelemetryDecorator(
            _mockInnerService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new PasswordRecoveryServiceTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new PasswordRecoveryServiceTelemetryDecorator(_mockInnerService.Object, null!)
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
        _mockInnerService
            .Setup(x => x.RequestPasswordRecoveryAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RequestPasswordRecoveryAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RequestPasswordRecoveryAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RequestPasswordRecovery_LogsFailureResult()
    {
        var request = new RequestPasswordRecoveryRequest("missing@example.com");
        var expectedResult = new RequestPasswordRecoveryResult(
            RequestPasswordRecoveryResultCode.UserNotFoundError,
            "not found",
            null
        );
        _mockInnerService
            .Setup(x => x.RequestPasswordRecoveryAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RequestPasswordRecoveryAsync(request);

        Assert.Equal(expectedResult, result);
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
        _mockInnerService
            .Setup(x => x.ValidatePasswordRecoveryTokenAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ValidatePasswordRecoveryTokenAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ValidatePasswordRecoveryTokenAsync(request), Times.Once);
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
        _mockInnerService
            .Setup(x => x.ResetPasswordWithRecoveryAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ResetPasswordWithRecoveryAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ResetPasswordWithRecoveryAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ResetPasswordWithRecovery_DoesNotSwallowExceptions()
    {
        var request = new ResetPasswordWithRecoveryRequest("token", "password");
        _mockInnerService
            .Setup(x => x.ResetPasswordWithRecoveryAsync(request))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _decorator.ResetPasswordWithRecoveryAsync(request)
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
