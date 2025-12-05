using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Permissions.Processors;

public class LoggingPermissionProcessorDecoratorTests
{
    private readonly Mock<IPermissionProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<LoggingPermissionProcessorDecorator>> _mockLogger;
    private readonly LoggingPermissionProcessorDecorator _decorator;

    public LoggingPermissionProcessorDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IPermissionProcessor>();
        _mockLogger = new Mock<ILogger<LoggingPermissionProcessorDecorator>>();
        _decorator = new LoggingPermissionProcessorDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreatePermissionAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new CreatePermissionRequest("testpermission", 1, "Resource", 1);
        var expectedResult = new CreatePermissionResult(
            CreatePermissionResultCode.Success,
            string.Empty,
            1
        );
        _mockInnerProcessor
            .Setup(x => x.CreatePermissionAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.CreatePermissionAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreatePermissionAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task CreateDefaultSystemPermissionsAsync_DelegatesToInner()
    {
        var accountId = 1;
        _mockInnerProcessor
            .Setup(x => x.CreateDefaultSystemPermissionsAsync(accountId))
            .Returns(Task.CompletedTask);

        await _decorator.CreateDefaultSystemPermissionsAsync(accountId);

        _mockInnerProcessor.Verify(
            x => x.CreateDefaultSystemPermissionsAsync(accountId),
            Times.Once
        );
        VerifyLogged();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingPermissionProcessorDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingPermissionProcessorDecorator(_mockInnerProcessor.Object, null!)
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

    private void VerifyLogged() =>
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
}
