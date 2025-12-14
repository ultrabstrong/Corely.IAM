using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Processors;
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
            _mockUserContextProvider.Object
        );
    }

    // DeregisterUserFromAccount tests
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

    // DeregisterUsersFromGroup tests
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
    public async Task DeregisterUsersFromGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.DeregisterUsersFromGroupAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }
}
