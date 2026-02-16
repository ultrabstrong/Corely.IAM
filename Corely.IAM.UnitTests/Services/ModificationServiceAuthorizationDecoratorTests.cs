using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Services;

public class ModificationServiceAuthorizationDecoratorTests
{
    private readonly Mock<IModificationService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly ModificationServiceAuthorizationDecorator _decorator;

    public ModificationServiceAuthorizationDecoratorTests()
    {
        _decorator = new ModificationServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object
        );
    }

    #region ModifyAccountAsync

    [Fact]
    public async Task ModifyAccountAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new UpdateAccountRequest(Guid.CreateVersion7(), "TestAccount");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.ModifyAccountAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ModifyAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task ModifyAccountAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new UpdateAccountRequest(Guid.CreateVersion7(), "TestAccount");
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ModifyAccountAsync(request);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ModifyAccountAsync(It.IsAny<UpdateAccountRequest>()),
            Times.Never
        );
    }

    #endregion

    #region ModifyUserAsync

    [Fact]
    public async Task ModifyUserAsync_Succeeds_WhenHasUserContext()
    {
        var request = new UpdateUserRequest(Guid.CreateVersion7(), "testuser", "test@test.com");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.ModifyUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ModifyUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task ModifyUserAsync_ReturnsUnauthorized_WhenNoUserContext()
    {
        var request = new UpdateUserRequest(Guid.CreateVersion7(), "testuser", "test@test.com");
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

        var result = await _decorator.ModifyUserAsync(request);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ModifyUserAsync(It.IsAny<UpdateUserRequest>()),
            Times.Never
        );
    }

    #endregion

    #region ModifyGroupAsync

    [Fact]
    public async Task ModifyGroupAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new UpdateGroupRequest(Guid.CreateVersion7(), "TestGroup", "Description");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.ModifyGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ModifyGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task ModifyGroupAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new UpdateGroupRequest(Guid.CreateVersion7(), "TestGroup", "Description");
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ModifyGroupAsync(request);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ModifyGroupAsync(It.IsAny<UpdateGroupRequest>()),
            Times.Never
        );
    }

    #endregion

    #region ModifyRoleAsync

    [Fact]
    public async Task ModifyRoleAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new UpdateRoleRequest(Guid.CreateVersion7(), "TestRole", "Description");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.ModifyRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ModifyRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task ModifyRoleAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new UpdateRoleRequest(Guid.CreateVersion7(), "TestRole", "Description");
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.ModifyRoleAsync(request);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ModifyRoleAsync(It.IsAny<UpdateRoleRequest>()),
            Times.Never
        );
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ModificationServiceAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ModificationServiceAuthorizationDecorator(_mockInnerService.Object, null!)
        );

    #endregion
}
