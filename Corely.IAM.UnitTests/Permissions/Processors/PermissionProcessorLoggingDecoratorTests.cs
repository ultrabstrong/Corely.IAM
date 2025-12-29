using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Permissions.Processors;

public class PermissionProcessorLoggingDecoratorTests
{
    private readonly Mock<IPermissionProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<PermissionProcessorLoggingDecorator>> _mockLogger;
    private readonly PermissionProcessorLoggingDecorator _decorator;

    public PermissionProcessorLoggingDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IPermissionProcessor>();
        _mockLogger = new Mock<ILogger<PermissionProcessorLoggingDecorator>>();
        _decorator = new PermissionProcessorLoggingDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreatePermissionAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new CreatePermissionRequest(
            Guid.CreateVersion7(),
            "group",
            Guid.CreateVersion7(),
            Read: true
        );
        var expectedResult = new CreatePermissionResult(
            CreatePermissionResultCode.Success,
            string.Empty,
            request.OwnerAccountId
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
        var accountId = Guid.CreateVersion7();
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
            new PermissionProcessorLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionProcessorLoggingDecorator(_mockInnerProcessor.Object, null!)
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
