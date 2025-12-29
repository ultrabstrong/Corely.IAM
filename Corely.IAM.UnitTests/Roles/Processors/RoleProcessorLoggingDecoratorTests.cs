using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Roles.Processors;

public class RoleProcessorLoggingDecoratorTests
{
    private readonly Mock<IRoleProcessor> _mockInnerProcessor;
    private readonly Mock<ILogger<RoleProcessorLoggingDecorator>> _mockLogger;
    private readonly RoleProcessorLoggingDecorator _decorator;

    public RoleProcessorLoggingDecoratorTests()
    {
        _mockInnerProcessor = new Mock<IRoleProcessor>();
        _mockLogger = new Mock<ILogger<RoleProcessorLoggingDecorator>>();
        _decorator = new RoleProcessorLoggingDecorator(
            _mockInnerProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new CreateRoleRequest("testrole", Guid.CreateVersion7());
        var expectedResult = new CreateRoleResult(
            CreateRoleResultCode.Success,
            string.Empty,
            request.OwnerAccountId
        );
        _mockInnerProcessor.Setup(x => x.CreateRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.CreateRoleAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task CreateDefaultSystemRolesAsync_DelegatesToInnerAndLogsResult()
    {
        var ownerAccountId = Guid.CreateVersion7();
        var expectedResult = new CreateDefaultSystemRolesResult(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7()
        );
        _mockInnerProcessor
            .Setup(x => x.CreateDefaultSystemRolesAsync(ownerAccountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.CreateDefaultSystemRolesAsync(ownerAccountId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(
            x => x.CreateDefaultSystemRolesAsync(ownerAccountId),
            Times.Once
        );
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetRoleAsyncById_DelegatesToInnerAndLogsResult()
    {
        var roleId = Guid.CreateVersion7();
        var expectedResult = new GetRoleResult(
            GetRoleResultCode.Success,
            string.Empty,
            new Role { Name = "testrole" }
        );
        _mockInnerProcessor.Setup(x => x.GetRoleAsync(roleId)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetRoleAsync(roleId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.GetRoleAsync(roleId), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetRoleAsyncByNameAndAccount_DelegatesToInnerAndLogsResult()
    {
        var roleName = "testrole";
        var ownerAccountId = Guid.CreateVersion7();
        var expectedResult = new GetRoleResult(
            GetRoleResultCode.Success,
            string.Empty,
            new Role { Name = roleName }
        );
        _mockInnerProcessor
            .Setup(x => x.GetRoleAsync(roleName, ownerAccountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetRoleAsync(roleName, ownerAccountId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.GetRoleAsync(roleName, ownerAccountId), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new AssignPermissionsToRoleRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
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
            new RoleProcessorLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RoleProcessorLoggingDecorator(_mockInnerProcessor.Object, null!)
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
