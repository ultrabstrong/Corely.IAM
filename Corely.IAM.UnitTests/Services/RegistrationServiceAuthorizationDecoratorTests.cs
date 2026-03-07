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
    public async Task RegisterUser_DelegatesToInner()
    {
        var request = new RegisterUserRequest("testuser", "test@test.com", "password");
        var expectedResult = new RegisterUserResult(
            RegisterUserResultCode.Success,
            string.Empty,
            Guid.CreateVersion7()
        );
        _mockInnerService.Setup(x => x.RegisterUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUserAsync(request), Times.Once);
    }

    #endregion

    #region Account-Context-Gated Methods

    [Fact]
    public async Task RegisterUsersWithGroup_DelegatesToInner_WhenHasAccountContext()
    {
        var request = new RegisterUsersWithGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        var expectedResult = new RegisterUsersWithGroupResult(
            AddUsersToGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.RegisterUsersWithGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUsersWithGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUsersWithGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterUsersWithGroup_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterUsersWithGroupRequest(
            [Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.RegisterUsersWithGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterUsersWithGroupAsync(It.IsAny<RegisterUsersWithGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RegisterRolesWithGroup_DelegatesToInner_WhenHasAccountContext()
    {
        var request = new RegisterRolesWithGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        var expectedResult = new RegisterRolesWithGroupResult(
            AssignRolesToGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.RegisterRolesWithGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRolesWithGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRolesWithGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterRolesWithGroup_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterRolesWithGroupRequest(
            [Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.RegisterRolesWithGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterRolesWithGroupAsync(It.IsAny<RegisterRolesWithGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RegisterRolesWithUser_DelegatesToInner_WhenHasAccountContext()
    {
        var request = new RegisterRolesWithUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        var expectedResult = new RegisterRolesWithUserResult(
            AssignRolesToUserResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.RegisterRolesWithUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRolesWithUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRolesWithUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterRolesWithUser_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterRolesWithUserRequest(
            [Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.RegisterRolesWithUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterRolesWithUserAsync(It.IsAny<RegisterRolesWithUserRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RegisterPermissionsWithRole_DelegatesToInner_WhenHasAccountContext()
    {
        var request = new RegisterPermissionsWithRoleRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        var expectedResult = new RegisterPermissionsWithRoleResult(
            AssignPermissionsToRoleResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.RegisterPermissionsWithRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterPermissionsWithRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterPermissionsWithRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterPermissionsWithRole_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterPermissionsWithRoleRequest(
            [Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

        var result = await _decorator.RegisterPermissionsWithRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.RegisterPermissionsWithRoleAsync(It.IsAny<RegisterPermissionsWithRoleRequest>()),
            Times.Never
        );
    }

    #endregion

    #region RegisterAccountAsync

    [Fact]
    public async Task RegisterAccount_Succeeds_WhenHasAccountContext()
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
    public async Task RegisterAccount_ReturnsUnauthorized_WhenNoAccountContext()
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
    public async Task RegisterGroup_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterGroupRequest("TestGroup");
        var expectedResult = new RegisterGroupResult(
            CreateGroupResultCode.Success,
            string.Empty,
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.RegisterGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterGroup_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterGroupRequest("TestGroup");
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

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
    public async Task RegisterRole_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterRoleRequest("TestRole");
        var expectedResult = new RegisterRoleResult(
            CreateRoleResultCode.Success,
            string.Empty,
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService.Setup(x => x.RegisterRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRoleAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterRole_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterRoleRequest("TestRole");
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

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
    public async Task RegisterPermission_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterPermissionRequest("resource", Guid.Empty);
        var expectedResult = new RegisterPermissionResult(
            CreatePermissionResultCode.Success,
            string.Empty,
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.RegisterPermissionAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterPermissionAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterPermissionAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterPermission_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterPermissionRequest("resource", Guid.Empty);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

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
    public async Task RegisterUserWithAccount_Succeeds_WhenHasAccountContext()
    {
        var request = new RegisterUserWithAccountRequest(Guid.CreateVersion7());
        var expectedResult = new RegisterUserWithAccountResult(
            RegisterUserWithAccountResultCode.Success,
            string.Empty
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(true);
        _mockInnerService
            .Setup(x => x.RegisterUserWithAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUserWithAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUserWithAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task RegisterUserWithAccount_ReturnsUnauthorized_WhenNoAccountContext()
    {
        var request = new RegisterUserWithAccountRequest(Guid.CreateVersion7());
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext()).Returns(false);

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
