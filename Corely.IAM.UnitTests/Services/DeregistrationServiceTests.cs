using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Services;
using Corely.IAM.Users.Processors;
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
    private readonly DeregistrationService _service;

    public DeregistrationServiceTests()
    {
        _service = new DeregistrationService(
            _mockLogger.Object,
            _mockPermissionProcessor.Object,
            _mockRoleProcessor.Object,
            _mockGroupProcessor.Object,
            _mockAccountProcessor.Object,
            _mockUserProcessor.Object
        );
    }

    [Fact]
    public async Task DeregisterUserFromAccountAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new DeregisterUserFromAccountRequest(1, 5);
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
        var request = new DeregisterUserFromAccountRequest(1, 5);
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
        var request = new DeregisterUserFromAccountRequest(1, 5);
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
        var request = new DeregisterUserFromAccountRequest(1, 5);
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
        var request = new DeregisterUserFromAccountRequest(1, 5);
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
}
