using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class ModificationServiceTests
{
    private readonly Mock<IAccountProcessor> _mockAccountProcessor = new();
    private readonly Mock<IUserProcessor> _mockUserProcessor = new();
    private readonly Mock<IGroupProcessor> _mockGroupProcessor = new();
    private readonly Mock<IRoleProcessor> _mockRoleProcessor = new();
    private readonly Mock<IPermissionProcessor> _mockPermissionProcessor = new();
    private readonly Mock<IKeyRotationProcessor> _mockKeyRotationProcessor = new();
    private readonly Mock<IBasicAuthProcessor> _mockBasicAuthProcessor = new();
    private readonly Mock<IReauthenticationProvider> _mockReauthenticationProvider = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly Mock<ILogger<ModificationService>> _mockLogger = new();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly ModificationService _service;

    public ModificationServiceTests()
    {
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(new UserContext(new User() { Id = _userId }, null, "test-device", []));
        _mockReauthenticationProvider
            .Setup(x => x.VerifyCurrentPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
            .ReturnsAsync(ReauthenticationResult.Succeeded);
        _mockReauthenticationProvider
            .Setup(x => x.VerifyTotpCodeAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
            .ReturnsAsync(ReauthenticationResult.Succeeded);

        _service = new ModificationService(
            _mockLogger.Object,
            _mockAccountProcessor.Object,
            _mockUserProcessor.Object,
            _mockGroupProcessor.Object,
            _mockRoleProcessor.Object,
            _mockPermissionProcessor.Object,
            _mockKeyRotationProcessor.Object,
            _mockBasicAuthProcessor.Object,
            _mockReauthenticationProvider.Object,
            _mockUserContextProvider.Object
        );
    }

    #region ModifyPasswordAsync Tests

    [Theory]
    [InlineData(UpdateBasicAuthResultCode.Success, ModifyPasswordResultCode.Success)]
    [InlineData(
        UpdateBasicAuthResultCode.BasicAuthNotFoundError,
        ModifyPasswordResultCode.BasicAuthNotFoundError
    )]
    [InlineData(
        UpdateBasicAuthResultCode.PasswordValidationError,
        ModifyPasswordResultCode.PasswordValidationError
    )]
    [InlineData(
        UpdateBasicAuthResultCode.ValidationError,
        ModifyPasswordResultCode.ValidationError
    )]
    [InlineData(
        UpdateBasicAuthResultCode.UnauthorizedError,
        ModifyPasswordResultCode.UnauthorizedError
    )]
    public async Task ModifyPassword_ReturnsTheUpdateResult_WhenChecksPass(
        UpdateBasicAuthResultCode updateResultCode,
        ModifyPasswordResultCode expected
    )
    {
        _mockBasicAuthProcessor
            .Setup(x => x.UpdateBasicAuthAsync(It.IsAny<UpdateBasicAuthRequest>()))
            .ReturnsAsync(new UpdateBasicAuthResult(updateResultCode, "update"));

        var result = await _service.ModifyPasswordAsync(new("current", "NewPassword1!", "123456"));

        Assert.Equal(expected, result.ResultCode);
        _mockReauthenticationProvider.Verify(
            x => x.VerifyCurrentPasswordAsync(_userId, "current"),
            Times.Once
        );
        _mockReauthenticationProvider.Verify(
            x => x.VerifyTotpCodeAsync(_userId, "123456"),
            Times.Once
        );
        _mockBasicAuthProcessor.Verify(
            x =>
                x.UpdateBasicAuthAsync(
                    It.Is<UpdateBasicAuthRequest>(r =>
                        r.UserId == _userId && r.Password == "NewPassword1!"
                    )
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(ModifyPasswordResultCode.BasicAuthNotFoundError)]
    [InlineData(ModifyPasswordResultCode.InvalidCurrentPasswordError)]
    [InlineData(ModifyPasswordResultCode.UserLockedError)]
    public async Task ModifyPassword_StopsBeforeTheCode_WhenCurrentPasswordCheckFails(
        ModifyPasswordResultCode expected
    )
    {
        _mockReauthenticationProvider
            .Setup(x => x.VerifyCurrentPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
            .ReturnsAsync(
                new ReauthenticationResult(
                    Enum.Parse<ReauthenticationResultCode>(expected.ToString()),
                    "no"
                )
            );

        var result = await _service.ModifyPasswordAsync(new("wrong", "NewPassword1!"));

        Assert.Equal(expected, result.ResultCode);
        Assert.Equal("no", result.Message);
        _mockReauthenticationProvider.Verify(
            x => x.VerifyTotpCodeAsync(It.IsAny<Guid>(), It.IsAny<string?>()),
            Times.Never
        );
        _mockBasicAuthProcessor.Verify(
            x => x.UpdateBasicAuthAsync(It.IsAny<UpdateBasicAuthRequest>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(ModifyPasswordResultCode.TotpRequiredError)]
    [InlineData(ModifyPasswordResultCode.InvalidTotpCodeError)]
    public async Task ModifyPassword_DoesNotUpdate_WhenTotpCheckFails(
        ModifyPasswordResultCode expected
    )
    {
        _mockReauthenticationProvider
            .Setup(x => x.VerifyTotpCodeAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
            .ReturnsAsync(
                new ReauthenticationResult(
                    Enum.Parse<ReauthenticationResultCode>(expected.ToString()),
                    "no"
                )
            );

        var result = await _service.ModifyPasswordAsync(new("current", "NewPassword1!"));

        Assert.Equal(expected, result.ResultCode);
        _mockBasicAuthProcessor.Verify(
            x => x.UpdateBasicAuthAsync(It.IsAny<UpdateBasicAuthRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ModifyPassword_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyPasswordAsync(null!));

        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region ModifyAccountAsync Tests

    [Fact]
    public async Task ModifyAccount_ReturnsSuccess_WhenProcessorSucceeds()
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
    public async Task ModifyAccount_ReturnsNotFound_WhenProcessorReturnsNotFound()
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
    public async Task ModifyAccount_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyAccountAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region ModifyUserAsync Tests

    [Fact]
    public async Task ModifyUser_ReturnsSuccess_WhenProcessorSucceeds()
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
    public async Task ModifyUser_ReturnsNotFound_WhenProcessorReturnsNotFound()
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
    public async Task ModifyUser_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyUserAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region ModifyGroupAsync Tests

    [Fact]
    public async Task ModifyGroup_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new UpdateGroupRequest(
            Guid.CreateVersion7(),
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
    public async Task ModifyGroup_ReturnsNotFound_WhenProcessorReturnsNotFound()
    {
        var request = new UpdateGroupRequest(
            Guid.CreateVersion7(),
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
    public async Task ModifyGroup_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyGroupAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    #region ModifyPermissionAsync Tests

    [Fact]
    public async Task ModifyPermission_DelegatesToProcessor_ForRequest()
    {
        var request = new UpdatePermissionRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Read reports"
        );
        var expected = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockPermissionProcessor
            .Setup(x => x.UpdatePermissionAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.ModifyPermissionAsync(request);

        Assert.Equal(expected, result);
        _mockPermissionProcessor.Verify(x => x.UpdatePermissionAsync(request), Times.Once);
    }

    #endregion

    #region ModifyRoleAsync Tests

    [Fact]
    public async Task ModifyRole_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var request = new UpdateRoleRequest(
            Guid.CreateVersion7(),
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
    public async Task ModifyRole_ReturnsNotFound_WhenProcessorReturnsNotFound()
    {
        var request = new UpdateRoleRequest(
            Guid.CreateVersion7(),
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
    public async Task ModifyRole_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.ModifyRoleAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    #endregion

    [Fact]
    public async Task RotateAccountKey_ReturnsProcessorResult()
    {
        var request = new RotateAccountKeyRequest(
            Guid.CreateVersion7(),
            KeyType.AsymmetricSignature
        );
        var processorResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockKeyRotationProcessor
            .Setup(x => x.RotateAccountKeyAsync(request))
            .ReturnsAsync(processorResult);

        var result = await _service.RotateAccountKeyAsync(request);

        Assert.Same(processorResult, result);
    }

    [Fact]
    public async Task RotateAccountKey_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _service.RotateAccountKeyAsync(null!));

        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RotateCurrentUserKey_ReturnsProcessorResult()
    {
        var processorResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockKeyRotationProcessor
            .Setup(x => x.RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption))
            .ReturnsAsync(processorResult);

        var result = await _service.RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption);

        Assert.Same(processorResult, result);
    }
}
