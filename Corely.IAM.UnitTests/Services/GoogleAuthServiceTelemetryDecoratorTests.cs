using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class GoogleAuthServiceTelemetryDecoratorTests
{
    private readonly Mock<IGoogleAuthService> _mockInnerService = new();
    private readonly Mock<ILogger<GoogleAuthServiceTelemetryDecorator>> _mockLogger = new();
    private readonly GoogleAuthServiceTelemetryDecorator _decorator;

    public GoogleAuthServiceTelemetryDecoratorTests()
    {
        _decorator = new GoogleAuthServiceTelemetryDecorator(
            _mockInnerService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GoogleAuthServiceTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GoogleAuthServiceTelemetryDecorator(_mockInnerService.Object, null!)
        );

    [Fact]
    public async Task LinkGoogleAuth_DelegatesToInnerAndLogsResult()
    {
        var request = new LinkGoogleAuthRequest("google-id-token");
        var expectedResult = new LinkGoogleAuthResult(
            LinkGoogleAuthResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.LinkGoogleAuthAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.LinkGoogleAuthAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.LinkGoogleAuthAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task LinkGoogleAuth_LogsFailureResult()
    {
        var request = new LinkGoogleAuthRequest("bad-token");
        var expectedResult = new LinkGoogleAuthResult(
            LinkGoogleAuthResultCode.InvalidGoogleTokenError,
            "invalid"
        );
        _mockInnerService.Setup(x => x.LinkGoogleAuthAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.LinkGoogleAuthAsync(request);

        Assert.Equal(expectedResult, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task UnlinkGoogleAuth_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new UnlinkGoogleAuthResult(
            UnlinkGoogleAuthResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.UnlinkGoogleAuthAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.UnlinkGoogleAuthAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.UnlinkGoogleAuthAsync(), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetAuthMethods_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new AuthMethodsResult(
            AuthMethodsResultCode.Success,
            string.Empty,
            true,
            true,
            "user@example.com"
        );
        _mockInnerService.Setup(x => x.GetAuthMethodsAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.GetAuthMethodsAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetAuthMethodsAsync(), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task UnlinkGoogleAuth_DoesNotSwallowExceptions()
    {
        _mockInnerService
            .Setup(x => x.UnlinkGoogleAuthAsync())
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _decorator.UnlinkGoogleAuthAsync()
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
