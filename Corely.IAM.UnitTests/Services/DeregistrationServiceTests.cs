using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class DeregistrationServiceTests
{
    private readonly Mock<ILogger<DeregistrationService>> _mockLogger = new();
    private readonly Mock<IPermissionProcessor> _mockPermissionProcessor = new();
    private readonly Mock<IRoleProcessor> _mockRoleProcessor = new();
    private readonly Mock<IGroupProcessor> _mockGroupProcessor = new();
    private readonly Mock<IAccountProcessor> _mockAccountProcessor = new();
    private readonly Mock<IUserProcessor> _mockUserProcessor = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly Mock<IUserContextSetter> _mockUserContextSetter = new();
    private readonly DeregistrationService _service;

    public DeregistrationServiceTests()
    {
        // Setup user context provider to return a valid context with account ID
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(new UserContext(1, 5));

        _service = new DeregistrationService(
            _mockLogger.Object,
            _mockPermissionProcessor.Object,
            _mockRoleProcessor.Object,
            _mockGroupProcessor.Object,
            _mockAccountProcessor.Object,
            _mockUserProcessor.Object,
            _mockUserContextProvider.Object,
            _mockUserContextSetter.Object
        );
    }

    #region DeregisterUserAsync Tests

    [Fact]
    public async Task DeregisterUserAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var processorResult = new DeleteUserResult(DeleteUserResultCode.Success, string.Empty);
        _mockUserProcessor
            .Setup(x => x.DeleteUserAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserAsync();

        Assert.Equal(DeregisterUserResultCode.Success, result.ResultCode);
        _mockUserProcessor.Verify(x => x.DeleteUserAsync(1), Times.Once);
        _mockUserContextSetter.Verify(x => x.ClearUserContext(1), Times.Once);
    }

    [Fact]
    public async Task DeregisterUserAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var processorResult = new DeleteUserResult(
            DeleteUserResultCode.UserNotFoundError,
            "User not found"
        );
        _mockUserProcessor
            .Setup(x => x.DeleteUserAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserAsync();

        Assert.Equal(DeregisterUserResultCode.UserNotFoundError, result.ResultCode);
        _mockUserContextSetter.Verify(x => x.ClearUserContext(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeregisterUserAsync_ReturnsUserIsSoleAccountOwnerError_WhenUserIsSoleOwner()
    {
        var processorResult = new DeleteUserResult(
            DeleteUserResultCode.UserIsSoleAccountOwnerError,
            "User is sole account owner"
        );
        _mockUserProcessor
            .Setup(x => x.DeleteUserAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserAsync();

        Assert.Equal(DeregisterUserResultCode.UserIsSoleAccountOwnerError, result.ResultCode);
        _mockUserContextSetter.Verify(x => x.ClearUserContext(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeregisterUserAsync_ReturnsUnauthorizedError_WhenUnauthorized()
    {
        var processorResult = new DeleteUserResult(
            DeleteUserResultCode.UnauthorizedError,
            "Unauthorized"
        );
        _mockUserProcessor
            .Setup(x => x.DeleteUserAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserAsync();

        Assert.Equal(DeregisterUserResultCode.UnauthorizedError, result.ResultCode);
        _mockUserContextSetter.Verify(x => x.ClearUserContext(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region DeregisterAccountAsync Tests

    [Fact]
    public async Task DeregisterAccountAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var processorResult = new DeleteAccountResult(
            DeleteAccountResultCode.Success,
            string.Empty
        );
        _mockAccountProcessor
            .Setup(x => x.DeleteAccountAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterAccountAsync();

        Assert.Equal(DeregisterAccountResultCode.Success, result.ResultCode);
        _mockAccountProcessor.Verify(x => x.DeleteAccountAsync(5), Times.Once);
    }

    [Fact]
    public async Task DeregisterAccountAsync_ClearsAccountFromContext_WhenSuccessAndContextMatchesAccount()
    {
        var processorResult = new DeleteAccountResult(
            DeleteAccountResultCode.Success,
            string.Empty
        );
        _mockAccountProcessor
            .Setup(x => x.DeleteAccountAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterAccountAsync();

        Assert.Equal(DeregisterAccountResultCode.Success, result.ResultCode);
        _mockUserContextSetter.Verify(
            x => x.SetUserContext(It.Is<UserContext>(c => c.UserId == 1 && c.AccountId == null)),
            Times.Once
        );
    }

    [Fact]
    public async Task DeregisterAccountAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var processorResult = new DeleteAccountResult(
            DeleteAccountResultCode.AccountNotFoundError,
            "Account not found"
        );
        _mockAccountProcessor
            .Setup(x => x.DeleteAccountAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterAccountAsync();

        Assert.Equal(DeregisterAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterAccountAsync_ReturnsUnauthorizedError_WhenUnauthorized()
    {
        var processorResult = new DeleteAccountResult(
            DeleteAccountResultCode.UnauthorizedError,
            "Unauthorized"
        );
        _mockAccountProcessor
            .Setup(x => x.DeleteAccountAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterAccountAsync();

        Assert.Equal(DeregisterAccountResultCode.UnauthorizedError, result.ResultCode);
    }

    #endregion

    #region DeregisterGroupAsync Tests

    [Fact]
    public async Task DeregisterGroupAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterGroupRequest(10);
        var processorResult = new DeleteGroupResult(DeleteGroupResultCode.Success, string.Empty);
        _mockGroupProcessor
            .Setup(x => x.DeleteGroupAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterGroupAsync(request);

        Assert.Equal(DeregisterGroupResultCode.Success, result.ResultCode);
        _mockGroupProcessor.Verify(x => x.DeleteGroupAsync(10), Times.Once);
    }

    [Fact]
    public async Task DeregisterGroupAsync_ReturnsGroupNotFound_WhenGroupDoesNotExist()
    {
        var request = new DeregisterGroupRequest(10);
        var processorResult = new DeleteGroupResult(
            DeleteGroupResultCode.GroupNotFoundError,
            "Group not found"
        );
        _mockGroupProcessor
            .Setup(x => x.DeleteGroupAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterGroupAsync(request);

        Assert.Equal(DeregisterGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterGroupAsync_ReturnsGroupHasSoleOwnersError_WhenGroupHasSoleOwners()
    {
        var request = new DeregisterGroupRequest(10);
        var processorResult = new DeleteGroupResult(
            DeleteGroupResultCode.GroupHasSoleOwnersError,
            "Group has sole owners"
        );
        _mockGroupProcessor
            .Setup(x => x.DeleteGroupAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterGroupAsync(request);

        Assert.Equal(DeregisterGroupResultCode.GroupHasSoleOwnersError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterGroupAsync_ReturnsUnauthorizedError_WhenUnauthorized()
    {
        var request = new DeregisterGroupRequest(10);
        var processorResult = new DeleteGroupResult(
            DeleteGroupResultCode.UnauthorizedError,
            "Unauthorized"
        );
        _mockGroupProcessor
            .Setup(x => x.DeleteGroupAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterGroupAsync(request);

        Assert.Equal(DeregisterGroupResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.DeregisterGroupAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region DeregisterRoleAsync Tests

    [Fact]
    public async Task DeregisterRoleAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterRoleRequest(10);
        var processorResult = new DeleteRoleResult(DeleteRoleResultCode.Success, string.Empty);
        _mockRoleProcessor
            .Setup(x => x.DeleteRoleAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRoleAsync(request);

        Assert.Equal(DeregisterRoleResultCode.Success, result.ResultCode);
        _mockRoleProcessor.Verify(x => x.DeleteRoleAsync(10), Times.Once);
    }

    [Fact]
    public async Task DeregisterRoleAsync_ReturnsRoleNotFound_WhenRoleDoesNotExist()
    {
        var request = new DeregisterRoleRequest(10);
        var processorResult = new DeleteRoleResult(
            DeleteRoleResultCode.RoleNotFoundError,
            "Role not found"
        );
        _mockRoleProcessor
            .Setup(x => x.DeleteRoleAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRoleAsync(request);

        Assert.Equal(DeregisterRoleResultCode.RoleNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterRoleAsync_ReturnsSystemDefinedRoleError_WhenRoleIsSystemDefined()
    {
        var request = new DeregisterRoleRequest(10);
        var processorResult = new DeleteRoleResult(
            DeleteRoleResultCode.SystemDefinedRoleError,
            "Role is system defined"
        );
        _mockRoleProcessor
            .Setup(x => x.DeleteRoleAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRoleAsync(request);

        Assert.Equal(DeregisterRoleResultCode.SystemDefinedRoleError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterRoleAsync_ReturnsUnauthorizedError_WhenUnauthorized()
    {
        var request = new DeregisterRoleRequest(10);
        var processorResult = new DeleteRoleResult(
            DeleteRoleResultCode.UnauthorizedError,
            "Unauthorized"
        );
        _mockRoleProcessor
            .Setup(x => x.DeleteRoleAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRoleAsync(request);

        Assert.Equal(DeregisterRoleResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterRoleAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.DeregisterRoleAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region DeregisterPermissionAsync Tests

    [Fact]
    public async Task DeregisterPermissionAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterPermissionRequest(10);
        var processorResult = new DeletePermissionResult(
            DeletePermissionResultCode.Success,
            string.Empty
        );
        _mockPermissionProcessor
            .Setup(x => x.DeletePermissionAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionAsync(request);

        Assert.Equal(DeregisterPermissionResultCode.Success, result.ResultCode);
        _mockPermissionProcessor.Verify(x => x.DeletePermissionAsync(10), Times.Once);
    }

    [Fact]
    public async Task DeregisterPermissionAsync_ReturnsPermissionNotFound_WhenPermissionDoesNotExist()
    {
        var request = new DeregisterPermissionRequest(10);
        var processorResult = new DeletePermissionResult(
            DeletePermissionResultCode.PermissionNotFoundError,
            "Permission not found"
        );
        _mockPermissionProcessor
            .Setup(x => x.DeletePermissionAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionAsync(request);

        Assert.Equal(DeregisterPermissionResultCode.PermissionNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterPermissionAsync_ReturnsSystemDefinedPermissionError_WhenPermissionIsSystemDefined()
    {
        var request = new DeregisterPermissionRequest(10);
        var processorResult = new DeletePermissionResult(
            DeletePermissionResultCode.SystemDefinedPermissionError,
            "Permission is system defined"
        );
        _mockPermissionProcessor
            .Setup(x => x.DeletePermissionAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionAsync(request);

        Assert.Equal(
            DeregisterPermissionResultCode.SystemDefinedPermissionError,
            result.ResultCode
        );
    }

    [Fact]
    public async Task DeregisterPermissionAsync_ReturnsUnauthorizedError_WhenUnauthorized()
    {
        var request = new DeregisterPermissionRequest(10);
        var processorResult = new DeletePermissionResult(
            DeletePermissionResultCode.UnauthorizedError,
            "Unauthorized"
        );
        _mockPermissionProcessor
            .Setup(x => x.DeletePermissionAsync(It.IsAny<int>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionAsync(request);

        Assert.Equal(DeregisterPermissionResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterPermissionAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.DeregisterPermissionAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region DeregisterUserFromAccountAsync Tests

    [Fact]
    public async Task DeregisterUserFromAccountAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterUserFromAccountRequest(1);
        var processorResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.Success,
            string.Empty
        );
        _mockAccountProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(It.IsAny<RemoveUserFromAccountRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserFromAccountAsync(request);

        Assert.Equal(DeregisterUserFromAccountResultCode.Success, result.ResultCode);
        _mockAccountProcessor.Verify(
            x =>
                x.RemoveUserFromAccountAsync(
                    It.Is<RemoveUserFromAccountRequest>(r => r.UserId == 1 && r.AccountId == 5)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DeregisterUserFromAccountAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var request = new DeregisterUserFromAccountRequest(1);
        var processorResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.UserNotFoundError,
            "User not found"
        );
        _mockAccountProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(It.IsAny<RemoveUserFromAccountRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserFromAccountAsync(request);

        Assert.Equal(DeregisterUserFromAccountResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterUserFromAccountAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var request = new DeregisterUserFromAccountRequest(1);
        var processorResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.AccountNotFoundError,
            "Account not found"
        );
        _mockAccountProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(It.IsAny<RemoveUserFromAccountRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserFromAccountAsync(request);

        Assert.Equal(DeregisterUserFromAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterUserFromAccountAsync_ReturnsUserNotInAccount_WhenUserNotInAccount()
    {
        var request = new DeregisterUserFromAccountRequest(1);
        var processorResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.UserNotInAccountError,
            "User not in account"
        );
        _mockAccountProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(It.IsAny<RemoveUserFromAccountRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserFromAccountAsync(request);

        Assert.Equal(DeregisterUserFromAccountResultCode.UserNotInAccountError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterUserFromAccountAsync_ReturnsSoleOwnerError_WhenUserIsSoleOwner()
    {
        var request = new DeregisterUserFromAccountRequest(1);
        var processorResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.UserIsSoleOwnerError,
            "User is sole owner"
        );
        _mockAccountProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(It.IsAny<RemoveUserFromAccountRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUserFromAccountAsync(request);

        Assert.Equal(DeregisterUserFromAccountResultCode.UserIsSoleOwnerError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterUserFromAccountAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.DeregisterUserFromAccountAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region DeregisterUsersFromGroupAsync Tests

    [Fact]
    public async Task DeregisterUsersFromGroupAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterUsersFromGroupRequest([1, 2], 5);
        var processorResult = new RemoveUsersFromGroupResult(
            RemoveUsersFromGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveUsersFromGroupAsync(It.IsAny<RemoveUsersFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUsersFromGroupAsync(request);

        Assert.Equal(DeregisterUsersFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(2, result.RemovedUserCount);
        _mockGroupProcessor.Verify(
            x =>
                x.RemoveUsersFromGroupAsync(
                    It.Is<RemoveUsersFromGroupRequest>(r =>
                        r.UserIds.SequenceEqual(new[] { 1, 2 }) && r.GroupId == 5
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DeregisterUsersFromGroupAsync_ReturnsPartialSuccess_WhenSomeUsersNotInGroup()
    {
        var request = new DeregisterUsersFromGroupRequest([1, 2, 3], 5);
        var processorResult = new RemoveUsersFromGroupResult(
            RemoveUsersFromGroupResultCode.PartialSuccess,
            "Some users not in group",
            2,
            [3]
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveUsersFromGroupAsync(It.IsAny<RemoveUsersFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUsersFromGroupAsync(request);

        Assert.Equal(DeregisterUsersFromGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(2, result.RemovedUserCount);
        Assert.Contains(3, result.InvalidUserIds);
    }

    [Fact]
    public async Task DeregisterUsersFromGroupAsync_ReturnsGroupNotFound_WhenGroupDoesNotExist()
    {
        var request = new DeregisterUsersFromGroupRequest([1, 2], 5);
        var processorResult = new RemoveUsersFromGroupResult(
            RemoveUsersFromGroupResultCode.GroupNotFoundError,
            "Group not found",
            0,
            [1, 2]
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveUsersFromGroupAsync(It.IsAny<RemoveUsersFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUsersFromGroupAsync(request);

        Assert.Equal(DeregisterUsersFromGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeregisterUsersFromGroupAsync_ReturnsUserIsSoleOwnerError_WhenUsersAreSoleOwners()
    {
        var request = new DeregisterUsersFromGroupRequest([1, 2], 5);
        var processorResult = new RemoveUsersFromGroupResult(
            RemoveUsersFromGroupResultCode.UserIsSoleOwnerError,
            "Users are sole owners",
            0,
            [],
            [1]
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveUsersFromGroupAsync(It.IsAny<RemoveUsersFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterUsersFromGroupAsync(request);

        Assert.Equal(DeregisterUsersFromGroupResultCode.UserIsSoleOwnerError, result.ResultCode);
        Assert.Contains(1, result.SoleOwnerUserIds);
    }

    [Fact]
    public async Task DeregisterUsersFromGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.DeregisterUsersFromGroupAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region DeregisterRolesFromGroupAsync Tests

    [Fact]
    public async Task DeregisterRolesFromGroupAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterRolesFromGroupRequest([1, 2], 5);
        var processorResult = new RemoveRolesFromGroupResult(
            RemoveRolesFromGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveRolesFromGroupAsync(It.IsAny<RemoveRolesFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromGroupAsync(request);

        Assert.Equal(DeregisterRolesFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(2, result.RemovedRoleCount);
        _mockGroupProcessor.Verify(
            x =>
                x.RemoveRolesFromGroupAsync(
                    It.Is<RemoveRolesFromGroupRequest>(r =>
                        r.RoleIds.SequenceEqual(new[] { 1, 2 }) && r.GroupId == 5
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DeregisterRolesFromGroupAsync_ReturnsPartialSuccess_WhenSomeRolesNotInGroup()
    {
        var request = new DeregisterRolesFromGroupRequest([1, 2, 3], 5);
        var processorResult = new RemoveRolesFromGroupResult(
            RemoveRolesFromGroupResultCode.PartialSuccess,
            "Some roles not in group",
            2,
            [3]
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveRolesFromGroupAsync(It.IsAny<RemoveRolesFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromGroupAsync(request);

        Assert.Equal(DeregisterRolesFromGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(2, result.RemovedRoleCount);
        Assert.Contains(3, result.InvalidRoleIds);
    }

    [Fact]
    public async Task DeregisterRolesFromGroupAsync_ReturnsGroupNotFound_WhenGroupDoesNotExist()
    {
        var request = new DeregisterRolesFromGroupRequest([1, 2], 5);
        var processorResult = new RemoveRolesFromGroupResult(
            RemoveRolesFromGroupResultCode.GroupNotFoundError,
            "Group not found",
            0,
            [1, 2]
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveRolesFromGroupAsync(It.IsAny<RemoveRolesFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromGroupAsync(request);

        Assert.Equal(DeregisterRolesFromGroupResultCode.GroupNotFoundError, result.ResultCode);
        Assert.Equal([1, 2], result.InvalidRoleIds);
    }

    [Fact]
    public async Task DeregisterRolesFromGroupAsync_ReturnsInvalidRoleIdsError_WhenAllRoleIdsInvalid()
    {
        var request = new DeregisterRolesFromGroupRequest([1, 2], 5);
        var processorResult = new RemoveRolesFromGroupResult(
            RemoveRolesFromGroupResultCode.InvalidRoleIdsError,
            "All role ids invalid",
            0,
            [1, 2]
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveRolesFromGroupAsync(It.IsAny<RemoveRolesFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromGroupAsync(request);

        Assert.Equal(DeregisterRolesFromGroupResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal([1, 2], result.InvalidRoleIds);
    }

    [Fact]
    public async Task DeregisterRolesFromGroupAsync_ReturnsOwnerRoleRemovalBlockedError_WhenOwnerRoleRemovalBlocked()
    {
        var request = new DeregisterRolesFromGroupRequest([1, 2], 5);
        var processorResult = new RemoveRolesFromGroupResult(
            RemoveRolesFromGroupResultCode.OwnerRoleRemovalBlockedError,
            "Owner role removal blocked",
            0,
            [],
            [1]
        );
        _mockGroupProcessor
            .Setup(x => x.RemoveRolesFromGroupAsync(It.IsAny<RemoveRolesFromGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromGroupAsync(request);

        Assert.Equal(
            DeregisterRolesFromGroupResultCode.OwnerRoleRemovalBlockedError,
            result.ResultCode
        );
        Assert.Contains(1, result.BlockedOwnerRoleIds);
    }

    [Fact]
    public async Task DeregisterRolesFromGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.DeregisterRolesFromGroupAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region DeregisterRolesFromUserAsync Tests

    [Fact]
    public async Task DeregisterRolesFromUserAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterRolesFromUserRequest([1, 2], 5);
        var processorResult = new RemoveRolesFromUserResult(
            RemoveRolesFromUserResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockUserProcessor
            .Setup(x => x.RemoveRolesFromUserAsync(It.IsAny<RemoveRolesFromUserRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromUserAsync(request);

        Assert.Equal(DeregisterRolesFromUserResultCode.Success, result.ResultCode);
        Assert.Equal(2, result.RemovedRoleCount);
        _mockUserProcessor.Verify(
            x =>
                x.RemoveRolesFromUserAsync(
                    It.Is<RemoveRolesFromUserRequest>(r =>
                        r.RoleIds.SequenceEqual(new[] { 1, 2 }) && r.UserId == 5
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DeregisterRolesFromUserAsync_ReturnsPartialSuccess_WhenSomeRolesNotAssigned()
    {
        var request = new DeregisterRolesFromUserRequest([1, 2, 3], 5);
        var processorResult = new RemoveRolesFromUserResult(
            RemoveRolesFromUserResultCode.PartialSuccess,
            "Some roles not assigned",
            2,
            [3]
        );
        _mockUserProcessor
            .Setup(x => x.RemoveRolesFromUserAsync(It.IsAny<RemoveRolesFromUserRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromUserAsync(request);

        Assert.Equal(DeregisterRolesFromUserResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(2, result.RemovedRoleCount);
        Assert.Contains(3, result.InvalidRoleIds);
    }

    [Fact]
    public async Task DeregisterRolesFromUserAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var request = new DeregisterRolesFromUserRequest([1, 2], 5);
        var processorResult = new RemoveRolesFromUserResult(
            RemoveRolesFromUserResultCode.UserNotFoundError,
            "User not found",
            0,
            [1, 2]
        );
        _mockUserProcessor
            .Setup(x => x.RemoveRolesFromUserAsync(It.IsAny<RemoveRolesFromUserRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromUserAsync(request);

        Assert.Equal(DeregisterRolesFromUserResultCode.UserNotFoundError, result.ResultCode);
        Assert.Equal([1, 2], result.InvalidRoleIds);
    }

    [Fact]
    public async Task DeregisterRolesFromUserAsync_ReturnsInvalidRoleIdsError_WhenAllRoleIdsInvalid()
    {
        var request = new DeregisterRolesFromUserRequest([1, 2], 5);
        var processorResult = new RemoveRolesFromUserResult(
            RemoveRolesFromUserResultCode.InvalidRoleIdsError,
            "All role ids invalid",
            0,
            [1, 2]
        );
        _mockUserProcessor
            .Setup(x => x.RemoveRolesFromUserAsync(It.IsAny<RemoveRolesFromUserRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromUserAsync(request);

        Assert.Equal(DeregisterRolesFromUserResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal([1, 2], result.InvalidRoleIds);
    }

    [Fact]
    public async Task DeregisterRolesFromUserAsync_ReturnsUserIsSoleOwnerError_WhenUserIsSoleOwner()
    {
        var request = new DeregisterRolesFromUserRequest([1, 2], 5);
        var processorResult = new RemoveRolesFromUserResult(
            RemoveRolesFromUserResultCode.UserIsSoleOwnerError,
            "User is sole owner",
            0,
            [],
            [1]
        );
        _mockUserProcessor
            .Setup(x => x.RemoveRolesFromUserAsync(It.IsAny<RemoveRolesFromUserRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterRolesFromUserAsync(request);

        Assert.Equal(DeregisterRolesFromUserResultCode.UserIsSoleOwnerError, result.ResultCode);
        Assert.Contains(1, result.BlockedOwnerRoleIds);
    }

    [Fact]
    public async Task DeregisterRolesFromUserAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.DeregisterRolesFromUserAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region DeregisterPermissionsFromRoleAsync Tests

    [Fact]
    public async Task DeregisterPermissionsFromRoleAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterPermissionsFromRoleRequest([1, 2], 5);
        var processorResult = new RemovePermissionsFromRoleResult(
            RemovePermissionsFromRoleResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockRoleProcessor
            .Setup(x =>
                x.RemovePermissionsFromRoleAsync(It.IsAny<RemovePermissionsFromRoleRequest>())
            )
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionsFromRoleAsync(request);

        Assert.Equal(DeregisterPermissionsFromRoleResultCode.Success, result.ResultCode);
        Assert.Equal(2, result.RemovedPermissionCount);
        _mockRoleProcessor.Verify(
            x =>
                x.RemovePermissionsFromRoleAsync(
                    It.Is<RemovePermissionsFromRoleRequest>(r =>
                        r.PermissionIds.SequenceEqual(new[] { 1, 2 }) && r.RoleId == 5
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DeregisterPermissionsFromRoleAsync_ReturnsPartialSuccess_WhenSomePermissionsNotAssigned()
    {
        var request = new DeregisterPermissionsFromRoleRequest([1, 2, 3], 5);
        var processorResult = new RemovePermissionsFromRoleResult(
            RemovePermissionsFromRoleResultCode.PartialSuccess,
            "Some permissions not assigned",
            2,
            [3]
        );
        _mockRoleProcessor
            .Setup(x =>
                x.RemovePermissionsFromRoleAsync(It.IsAny<RemovePermissionsFromRoleRequest>())
            )
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionsFromRoleAsync(request);

        Assert.Equal(DeregisterPermissionsFromRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(2, result.RemovedPermissionCount);
        Assert.Contains(3, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task DeregisterPermissionsFromRoleAsync_ReturnsRoleNotFound_WhenRoleDoesNotExist()
    {
        var request = new DeregisterPermissionsFromRoleRequest([1, 2], 5);
        var processorResult = new RemovePermissionsFromRoleResult(
            RemovePermissionsFromRoleResultCode.RoleNotFoundError,
            "Role not found",
            0,
            [1, 2]
        );
        _mockRoleProcessor
            .Setup(x =>
                x.RemovePermissionsFromRoleAsync(It.IsAny<RemovePermissionsFromRoleRequest>())
            )
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionsFromRoleAsync(request);

        Assert.Equal(DeregisterPermissionsFromRoleResultCode.RoleNotFoundError, result.ResultCode);
        Assert.Equal([1, 2], result.InvalidPermissionIds);
    }

    [Fact]
    public async Task DeregisterPermissionsFromRoleAsync_ReturnsInvalidPermissionIdsError_WhenAllPermissionIdsInvalid()
    {
        var request = new DeregisterPermissionsFromRoleRequest([1, 2], 5);
        var processorResult = new RemovePermissionsFromRoleResult(
            RemovePermissionsFromRoleResultCode.InvalidPermissionIdsError,
            "All permission ids invalid",
            0,
            [1, 2]
        );
        _mockRoleProcessor
            .Setup(x =>
                x.RemovePermissionsFromRoleAsync(It.IsAny<RemovePermissionsFromRoleRequest>())
            )
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionsFromRoleAsync(request);

        Assert.Equal(
            DeregisterPermissionsFromRoleResultCode.InvalidPermissionIdsError,
            result.ResultCode
        );
        Assert.Equal([1, 2], result.InvalidPermissionIds);
    }

    [Fact]
    public async Task DeregisterPermissionsFromRoleAsync_ReturnsSystemPermissionRemovalError_WhenSystemPermissionRemoval()
    {
        var request = new DeregisterPermissionsFromRoleRequest([1, 2], 5);
        var processorResult = new RemovePermissionsFromRoleResult(
            RemovePermissionsFromRoleResultCode.SystemPermissionRemovalError,
            "Cannot remove system permissions from system role",
            0,
            [],
            [1]
        );
        _mockRoleProcessor
            .Setup(x =>
                x.RemovePermissionsFromRoleAsync(It.IsAny<RemovePermissionsFromRoleRequest>())
            )
            .ReturnsAsync(processorResult);

        var result = await _service.DeregisterPermissionsFromRoleAsync(request);

        Assert.Equal(
            DeregisterPermissionsFromRoleResultCode.SystemPermissionRemovalError,
            result.ResultCode
        );
        Assert.Contains(1, result.SystemPermissionIds);
    }

    [Fact]
    public async Task DeregisterPermissionsFromRoleAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _service.DeregisterPermissionsFromRoleAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion
}
