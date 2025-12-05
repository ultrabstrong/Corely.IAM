using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Roles.Processors;

public class LoggingRoleProcessorDecoratorTests
{
    private readonly Mock<IRoleProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<LoggingRoleProcessorDecorator>> _mockLogger;
    private readonly LoggingRoleProcessorDecorator _decorator;

    public LoggingRoleProcessorDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IRoleProcessor>();
        _mockLogger = new Mock<ILogger<LoggingRoleProcessorDecorator>>();
        _decorator = new LoggingRoleProcessorDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new CreateRoleRequest("testrole", 1);
        var expectedResult = new CreateRoleResult(CreateRoleResultCode.Success, string.Empty, 1);
        _mockInnerProcessor.Setup(x => x.CreateRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateRoleAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task CreateDefaultSystemRolesAsync_DelegatesToInner()
    {
        var ownerAccountId = 1;
        _mockInnerProcessor
            .Setup(x => x.CreateDefaultSystemRolesAsync(ownerAccountId))
            .Returns(Task.CompletedTask);

        await _decorator.CreateDefaultSystemRolesAsync(ownerAccountId);

        _mockInnerProcessor.Verify(
            x => x.CreateDefaultSystemRolesAsync(ownerAccountId),
            Times.Once
        );
        VerifyLogged();
    }

    [Fact]
    public async Task GetRoleAsyncById_DelegatesToInnerWithoutLoggingResult()
    {
        var roleId = 1;
        var expectedRole = new Role { Name = "testrole" };
        _mockInnerProcessor.Setup(x => x.GetRoleAsync(roleId)).ReturnsAsync(expectedRole);

        var result = await _decorator.GetRoleAsync(roleId);

        Assert.Equal(expectedRole, result);
        _mockInnerProcessor.Verify(x => x.GetRoleAsync(roleId), Times.Once);
        VerifyLoggedWithoutResult();
    }

    [Fact]
    public async Task GetRoleAsyncByNameAndAccount_DelegatesToInnerWithoutLoggingResult()
    {
        var roleName = "testrole";
        var ownerAccountId = 1;
        var expectedRole = new Role { Name = roleName };
        _mockInnerProcessor
            .Setup(x => x.GetRoleAsync(roleName, ownerAccountId))
            .ReturnsAsync(expectedRole);

        var result = await _decorator.GetRoleAsync(roleName, ownerAccountId);

        Assert.Equal(expectedRole, result);
        _mockInnerProcessor.Verify(x => x.GetRoleAsync(roleName, ownerAccountId), Times.Once);
        VerifyLoggedWithoutResult();
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new AssignPermissionsToRoleRequest([1, 2], 1);
        var expectedResult = new AssignPermissionsToRoleResult(
            AssignPermissionsToRoleResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AssignPermissionsToRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignPermissionsToRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.AssignPermissionsToRoleAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingRoleProcessorDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingRoleProcessorDecorator(_mockInnerProcessor.Object, null!)
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

    private void VerifyLoggedWithoutResult() =>
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("completed")
                            && !v.ToString()!.Contains("with result")
                    ),
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
