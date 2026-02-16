using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class ModificationServiceTests
{
    private readonly Mock<IAccountProcessor> _mockAccountProcessor = new();
    private readonly Mock<IUserProcessor> _mockUserProcessor = new();
    private readonly Mock<IGroupProcessor> _mockGroupProcessor = new();
    private readonly Mock<IRoleProcessor> _mockRoleProcessor = new();
    private readonly Mock<ILogger<ModificationService>> _mockLogger = new();
    private readonly ModificationService _service;

    public ModificationServiceTests()
    {
        _service = new ModificationService(
            _mockAccountProcessor.Object,
            _mockUserProcessor.Object,
            _mockGroupProcessor.Object,
            _mockRoleProcessor.Object,
            _mockLogger.Object
        );
    }

    #region ModifyAccountAsync Tests

    [Fact]
    public async Task ModifyAccountAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new UpdateAccountRequest(Guid.CreateVersion7(), "Updated Account");
        var processorResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockAccountProcessor
            .Setup(x => x.UpdateAccountAsync(It.IsAny<UpdateAccountRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.ModifyAccountAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        _mockAccountProcessor.Verify(x => x.UpdateAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task ModifyAccountAsync_ReturnsNotFound_WhenProcessorReturnsNotFound()
    {
        var request = new UpdateAccountRequest(Guid.CreateVersion7(), "Updated Account");
        var processorResult = new ModifyResult(ModifyResultCode.NotFoundError, "Account not found");
        _mockAccountProcessor
            .Setup(x => x.UpdateAccountAsync(It.IsAny<UpdateAccountRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.ModifyAccountAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task ModifyAccountAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyAccountAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region ModifyUserAsync Tests

    [Fact]
    public async Task ModifyUserAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new UpdateUserRequest(
            Guid.CreateVersion7(),
            "updateduser",
            "updated@test.com"
        );
        var processorResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockUserProcessor
            .Setup(x => x.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.ModifyUserAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        _mockUserProcessor.Verify(x => x.UpdateUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task ModifyUserAsync_ReturnsNotFound_WhenProcessorReturnsNotFound()
    {
        var request = new UpdateUserRequest(
            Guid.CreateVersion7(),
            "updateduser",
            "updated@test.com"
        );
        var processorResult = new ModifyResult(ModifyResultCode.NotFoundError, "User not found");
        _mockUserProcessor
            .Setup(x => x.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.ModifyUserAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task ModifyUserAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyUserAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region ModifyGroupAsync Tests

    [Fact]
    public async Task ModifyGroupAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new UpdateGroupRequest(
            Guid.CreateVersion7(),
            "Updated Group",
            "Updated description"
        );
        var processorResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockGroupProcessor
            .Setup(x => x.UpdateGroupAsync(It.IsAny<UpdateGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.ModifyGroupAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        _mockGroupProcessor.Verify(x => x.UpdateGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task ModifyGroupAsync_ReturnsNotFound_WhenProcessorReturnsNotFound()
    {
        var request = new UpdateGroupRequest(
            Guid.CreateVersion7(),
            "Updated Group",
            "Updated description"
        );
        var processorResult = new ModifyResult(ModifyResultCode.NotFoundError, "Group not found");
        _mockGroupProcessor
            .Setup(x => x.UpdateGroupAsync(It.IsAny<UpdateGroupRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.ModifyGroupAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task ModifyGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyGroupAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region ModifyRoleAsync Tests

    [Fact]
    public async Task ModifyRoleAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new UpdateRoleRequest(
            Guid.CreateVersion7(),
            "Updated Role",
            "Updated description"
        );
        var processorResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockRoleProcessor
            .Setup(x => x.UpdateRoleAsync(It.IsAny<UpdateRoleRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.ModifyRoleAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        _mockRoleProcessor.Verify(x => x.UpdateRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task ModifyRoleAsync_ReturnsNotFound_WhenProcessorReturnsNotFound()
    {
        var request = new UpdateRoleRequest(
            Guid.CreateVersion7(),
            "Updated Role",
            "Updated description"
        );
        var processorResult = new ModifyResult(ModifyResultCode.NotFoundError, "Role not found");
        _mockRoleProcessor
            .Setup(x => x.UpdateRoleAsync(It.IsAny<UpdateRoleRequest>()))
            .ReturnsAsync(processorResult);

        var result = await _service.ModifyRoleAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task ModifyRoleAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyRoleAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion
}
