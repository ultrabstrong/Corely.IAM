using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class RetrievalServiceTelemetryDecoratorTests
{
    private readonly Mock<IRetrievalService> _mockInnerService;
    private readonly Mock<ILogger<RetrievalServiceTelemetryDecorator>> _mockLogger;
    private readonly RetrievalServiceTelemetryDecorator _decorator;

    public RetrievalServiceTelemetryDecoratorTests()
    {
        _mockInnerService = new Mock<IRetrievalService>();
        _mockLogger = new Mock<ILogger<RetrievalServiceTelemetryDecorator>>();
        _decorator = new RetrievalServiceTelemetryDecorator(
            _mockInnerService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ListPermissionsAsync_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<Permission>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Permission>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListPermissionsAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListPermissionsAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListPermissionsAsync(null, null, 0, 25), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetPermissionAsync_DelegatesToInnerAndLogsResult()
    {
        var permissionId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<Permission>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockInnerService
            .Setup(x => x.GetPermissionAsync(permissionId, false))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetPermissionAsync(permissionId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetPermissionAsync(permissionId, false), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ListGroupsAsync_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<Group>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Group>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListGroupsAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListGroupsAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListGroupsAsync(null, null, 0, 25), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var groupId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<Group>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockInnerService.Setup(x => x.GetGroupAsync(groupId, false)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetGroupAsync(groupId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetGroupAsync(groupId, false), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ListRolesAsync_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<Role>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Role>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListRolesAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListRolesAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListRolesAsync(null, null, 0, 25), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var roleId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<Role>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockInnerService.Setup(x => x.GetRoleAsync(roleId, false)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetRoleAsync(roleId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetRoleAsync(roleId, false), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ListUsersAsync_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<User>(
            RetrieveResultCode.Success,
            "",
            PagedResult<User>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListUsersAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListUsersAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListUsersAsync(null, null, 0, 25), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetUserAsync_DelegatesToInnerAndLogsResult()
    {
        var userId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<User>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockInnerService.Setup(x => x.GetUserAsync(userId, false)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetUserAsync(userId, false), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ListAccountsAsync_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<Account>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Account>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListAccountsAsync(null, null, 0, 25))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListAccountsAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListAccountsAsync(null, null, 0, 25), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetAccountAsync_DelegatesToInnerAndLogsResult()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<Account>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockInnerService
            .Setup(x => x.GetAccountAsync(accountId, false))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.GetAccountAsync(accountId, false), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RetrievalServiceTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RetrievalServiceTelemetryDecorator(_mockInnerService.Object, null!)
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
