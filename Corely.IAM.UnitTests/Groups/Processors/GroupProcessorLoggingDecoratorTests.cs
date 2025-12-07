using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Groups.Processors;

public class GroupProcessorLoggingDecoratorTests
{
    private readonly Mock<IGroupProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<GroupProcessorLoggingDecorator>> _mockLogger;
    private readonly GroupProcessorLoggingDecorator _decorator;

    public GroupProcessorLoggingDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IGroupProcessor>();
        _mockLogger = new Mock<ILogger<GroupProcessorLoggingDecorator>>();
        _decorator = new GroupProcessorLoggingDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new CreateGroupRequest("testgroup", 1);
        var expectedResult = new CreateGroupResult(CreateGroupResultCode.Success, string.Empty, 1);
        _mockInnerProcessor.Setup(x => x.CreateGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AddUsersToGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new AddUsersToGroupRequest([1, 2], 1);
        var expectedResult = new AddUsersToGroupResult(
            AddUsersToGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AddUsersToGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AddUsersToGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.AddUsersToGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new AssignRolesToGroupRequest([1, 2], 1);
        var expectedResult = new AssignRolesToGroupResult(
            AssignRolesToGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AssignRolesToGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignRolesToGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.AssignRolesToGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GroupProcessorLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GroupProcessorLoggingDecorator(_mockInnerProcessor.Object, null!)
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
