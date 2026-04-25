using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.GoogleAuths.Processors;

public class GoogleAuthProcessorTelemetryDecoratorTests
{
    private readonly Mock<IGoogleAuthProcessor> _mockInnerProcessor = new();
    private readonly Mock<ILogger<GoogleAuthProcessorTelemetryDecorator>> _mockLogger = new();
    private readonly GoogleAuthProcessorTelemetryDecorator _decorator;

    public GoogleAuthProcessorTelemetryDecoratorTests()
    {
        _decorator = new GoogleAuthProcessorTelemetryDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expected = new LinkGoogleAuthResult(LinkGoogleAuthResultCode.Success, string.Empty);
        _mockInnerProcessor
            .Setup(x => x.LinkGoogleAuthAsync(userId, "token"))
            .ReturnsAsync(expected);

        var result = await _decorator.LinkGoogleAuthAsync(userId, "token");

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task UnlinkGoogleAuthAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expected = new UnlinkGoogleAuthResult(UnlinkGoogleAuthResultCode.Success, string.Empty);
        _mockInnerProcessor.Setup(x => x.UnlinkGoogleAuthAsync(userId)).ReturnsAsync(expected);

        var result = await _decorator.UnlinkGoogleAuthAsync(userId);

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetAuthMethodsAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expected = new AuthMethodsResult(
            AuthMethodsResultCode.Success,
            string.Empty,
            true,
            true,
            "user@test.com"
        );
        _mockInnerProcessor.Setup(x => x.GetAuthMethodsAsync(userId)).ReturnsAsync(expected);

        var result = await _decorator.GetAuthMethodsAsync(userId);

        Assert.Equal(expected, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetUserIdByGoogleSubjectAsync_DelegatesToInnerAndLogsResult()
    {
        var expectedUserId = Guid.CreateVersion7();
        _mockInnerProcessor
            .Setup(x => x.GetUserIdByGoogleSubjectAsync("subject"))
            .ReturnsAsync(expectedUserId);

        var result = await _decorator.GetUserIdByGoogleSubjectAsync("subject");

        Assert.Equal(expectedUserId, result);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GoogleAuthProcessorTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GoogleAuthProcessorTelemetryDecorator(_mockInnerProcessor.Object, null!)
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
