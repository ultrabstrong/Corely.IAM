using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Models;
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
    public async Task ListPermissions_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<Permission>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Permission>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListPermissionsAsync(It.IsAny<ListPermissionsRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListPermissionsAsync(new ListPermissionsRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.ListPermissionsAsync(It.IsAny<ListPermissionsRequest>()),
            Times.Once
        );
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetPermission_DelegatesToInnerAndLogsResult()
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
    public async Task ListGroups_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<Group>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Group>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListGroupsAsync(It.IsAny<ListGroupsRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListGroupsAsync(new ListGroupsRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListGroupsAsync(It.IsAny<ListGroupsRequest>()), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetGroup_DelegatesToInnerAndLogsResult()
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
    public async Task ListRoles_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<Role>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Role>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListRolesAsync(It.IsAny<ListRolesRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListRolesAsync(new ListRolesRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListRolesAsync(It.IsAny<ListRolesRequest>()), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetRole_DelegatesToInnerAndLogsResult()
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
    public async Task ListUsers_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<User>(
            RetrieveResultCode.Success,
            "",
            PagedResult<User>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListUsersAsync(new ListUsersRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetUser_DelegatesToInnerAndLogsResult()
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
    public async Task ListAccounts_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new RetrieveListResult<Account>(
            RetrieveResultCode.Success,
            "",
            PagedResult<Account>.Empty()
        );
        _mockInnerService
            .Setup(x => x.ListAccountsAsync(It.IsAny<ListAccountsRequest>()))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.ListAccountsAsync(new ListAccountsRequest());

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.ListAccountsAsync(It.IsAny<ListAccountsRequest>()),
            Times.Once
        );
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetAccount_DelegatesToInnerAndLogsResult()
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
    public async Task GetAccountSymmetricEncryptionProvider_DelegatesToInnerAndLogsResult()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockInnerService
            .Setup(x => x.GetAccountSymmetricEncryptionProviderAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountSymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.GetAccountSymmetricEncryptionProviderAsync(accountId),
            Times.Once
        );
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetAccountAsymmetricEncryptionProvider_DelegatesToInnerAndLogsResult()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockInnerService
            .Setup(x => x.GetAccountAsymmetricEncryptionProviderAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.GetAccountAsymmetricEncryptionProviderAsync(accountId),
            Times.Once
        );
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task GetAccountAsymmetricSignatureProvider_DelegatesToInnerAndLogsResult()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
            RetrieveResultCode.Success,
            "",
            null,
            null
        );
        _mockInnerService
            .Setup(x => x.GetAccountAsymmetricSignatureProviderAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsymmetricSignatureProviderAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(
            x => x.GetAccountAsymmetricSignatureProviderAsync(accountId),
            Times.Once
        );
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
