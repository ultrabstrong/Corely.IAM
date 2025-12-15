using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Services;

public class RegistrationServiceAuthorizationDecoratorTests
{
    private readonly Mock<IRegistrationService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly RegistrationServiceAuthorizationDecorator _decorator;

    public RegistrationServiceAuthorizationDecoratorTests()
    {
        _decorator = new RegistrationServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object
        );
    }

    #region Passthrough Methods (No Authorization)

    [Fact]
    public async Task RegisterUserAsync_DelegatesToInner()
    {
        var request = new RegisterUserRequest("testuser", "test@test.com", "password");
        var expectedResult = new RegisterUserResult(
            RegisterUserResultCode.Success,
            string.Empty,
            1,
            Guid.NewGuid()
        );
        _mockInnerService.Setup(x => x.RegisterUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterUsersWithGroupAsync_DelegatesToInner()
    {
        var request = new RegisterUsersWithGroupRequest([1, 2], 1);
        var expectedResult = new RegisterUsersWithGroupResult(
            AddUsersToGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.RegisterUsersWithGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUsersWithGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUsersWithGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterRolesWithGroupAsync_DelegatesToInner()
    {
        var request = new RegisterRolesWithGroupRequest([1, 2], 1);
        var expectedResult = new RegisterRolesWithGroupResult(
            AssignRolesToGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.RegisterRolesWithGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRolesWithGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRolesWithGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterRolesWithUserAsync_DelegatesToInner()
    {
        var request = new RegisterRolesWithUserRequest([1, 2], 1);
        var expectedResult = new RegisterRolesWithUserResult(
            AssignRolesToUserResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.RegisterRolesWithUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRolesWithUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRolesWithUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterPermissionsWithRoleAsync_DelegatesToInner()
    {
        var request = new RegisterPermissionsWithRoleRequest([1, 2], 1);
        var expectedResult = new RegisterPermissionsWithRoleResult(
            AssignPermissionsToRoleResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.RegisterPermissionsWithRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterPermissionsWithRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterPermissionsWithRoleAsync(request), Times.Once);
    }

    #endregion

    #region RegisterAccountAsync

    [Fact]
    public async Task RegisterAccountAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterAccountRequest("TestAccount");
        var expectedResult = new RegisterAccountResult(
            RegisterAccountResultCode.Success,
            string.Empty,
            Guid.Empty
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.RegisterAccountAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterAccountAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterAccountRequest("TestAccount");
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(false);

        var result = await _decorator.RegisterAccountAsync(request);

        Assert.Equal(RegisterAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterAccountAsync(It.IsAny<RegisterAccountRequest>()),
            Times.Never
        );
    }

    #endregion

    #region RegisterGroupAsync

    [Fact]
    public async Task RegisterGroupAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterGroupRequest("TestGroup");
        var expectedResult = new RegisterGroupResult(
            CreateGroupResultCode.Success,
            string.Empty,
            1
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(true);
        _mockInnerService.Setup(x => x.RegisterGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterGroupAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterGroupRequest("TestGroup");
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(false);

        var result = await _decorator.RegisterGroupAsync(request);

        Assert.Equal(CreateGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterGroupAsync(It.IsAny<RegisterGroupRequest>()),
            Times.Never
        );
    }

    #endregion

    #region RegisterRoleAsync

    [Fact]
    public async Task RegisterRoleAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterRoleRequest("TestRole");
        var expectedResult = new RegisterRoleResult(CreateRoleResultCode.Success, string.Empty, 1);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(true);
        _mockInnerService.Setup(x => x.RegisterRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterRoleAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterRoleRequest("TestRole");
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(false);

        var result = await _decorator.RegisterRoleAsync(request);

        Assert.Equal(CreateRoleResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterRoleAsync(It.IsAny<RegisterRoleRequest>()),
            Times.Never
        );
    }

    #endregion

    #region RegisterPermissionAsync

    [Fact]
    public async Task RegisterPermissionAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterPermissionRequest("resource", 0);
        var expectedResult = new RegisterPermissionResult(
            CreatePermissionResultCode.Success,
            string.Empty,
            1
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(true);
        _mockInnerService
            .Setup(x => x.RegisterPermissionAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterPermissionAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterPermissionAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterPermissionAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterPermissionRequest("resource", 0);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(false);

        var result = await _decorator.RegisterPermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterPermissionAsync(It.IsAny<RegisterPermissionRequest>()),
            Times.Never
        );
    }

    #endregion

    #region RegisterUserWithAccountAsync

    [Fact]
    public async Task RegisterUserWithAccountAsync_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterUserWithAccountRequest(1);
        var expectedResult = new RegisterUserWithAccountResult(
            RegisterUserWithAccountResultCode.Success,
            string.Empty
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(true);
        _mockInnerService
            .Setup(x => x.RegisterUserWithAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUserWithAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUserWithAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterUserWithAccountAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterUserWithAccountRequest(1);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContextAsync()).ReturnsAsync(false);

        var result = await _decorator.RegisterUserWithAccountAsync(request);

        Assert.Equal(RegisterUserWithAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterUserWithAccountAsync(It.IsAny<RegisterUserWithAccountRequest>()),
            Times.Never
        );
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RegistrationServiceAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RegistrationServiceAuthorizationDecorator(_mockInnerService.Object, null!)
        );

    #endregion
}
