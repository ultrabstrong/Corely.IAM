using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Groups.Processors;

public class GroupProcessorTelemetryDecoratorTests
{
    private readonly Mock<IGroupProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<GroupProcessorTelemetryDecorator>> _mockLogger;
    private readonly GroupProcessorTelemetryDecorator _decorator;

    public GroupProcessorTelemetryDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IGroupProcessor>();
        _mockLogger = new Mock<ILogger<GroupProcessorTelemetryDecorator>>();
        _decorator = new GroupProcessorTelemetryDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new CreateGroupRequest("testgroup", Guid.CreateVersion7());
        var expectedResult = new CreateGroupResult(
            CreateGroupResultCode.Success,
            string.Empty,
            request.OwnerAccountId
        );
        _mockInnerProcessor.Setup(x => x.CreateGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AddUsersToGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new AddUsersToGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
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
    public async Task RemoveUsersFromGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RemoveUsersFromGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        var expectedResult = new RemoveUsersFromGroupResult(
            RemoveUsersFromGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.RemoveUsersFromGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveUsersFromGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.RemoveUsersFromGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new AssignRolesToGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
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
            new GroupProcessorTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GroupProcessorTelemetryDecorator(_mockInnerProcessor.Object, null!)
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
