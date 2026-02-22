using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;

namespace Corely.IAM.UnitTests.Users.Processors;

public class UserProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IUserProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly UserProcessorAuthorizationDecorator _decorator;

    public UserProcessorAuthorizationDecoratorTests()
    {
        _decorator = new UserProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task CreateUserAsync_BypassesAuthorization()
    {
        var request = new CreateUserRequest("testuser", "test@test.com");
        var expectedResult = new CreateUserResult(
            CreateUserResultCode.Success,
            "",
            Guid.CreateVersion7()
        );
        _mockInnerProcessor.Setup(x => x.CreateUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateUserAsync(request);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task GetUserAsyncById_CallsAuthorizationProviderWithResourceId()
    {
        var userId = Guid.CreateVersion7();
        var expectedResult = new GetUserResult(
            GetUserResultCode.Success,
            string.Empty,
            new User { Id = userId, Username = "testuser" }
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, userId)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor.Setup(x => x.GetUserAsync(userId)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    userId
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUserAsyncById_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, userId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(GetUserResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.User);
        _mockInnerProcessor.Verify(x => x.GetUserAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_Succeeds_WhenUserUpdatesOwnAccount()
    {
        var userId = Guid.CreateVersion7();
        var request = new UpdateUserRequest(userId, "testuser", "test@test.com");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(true);
        _mockInnerProcessor.Setup(x => x.UpdateUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.UpdateUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var request = new UpdateUserRequest(Guid.CreateVersion7(), "testuser", "test@test.com");
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(request.UserId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.UpdateUserAsync(request);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.UpdateUserAsync(It.IsAny<UpdateUserRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_CallsAuthorizationProviderWithResourceId()
    {
        var userId = Guid.CreateVersion7();
        var expectedResult = new GetAsymmetricKeyResult(
            GetAsymmetricKeyResultCode.Success,
            string.Empty,
            "test-key"
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, userId)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.GetAsymmetricSignatureVerificationKeyAsync(userId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAsymmetricSignatureVerificationKeyAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    userId
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, userId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetAsymmetricSignatureVerificationKeyAsync(userId);

        Assert.Equal(GetAsymmetricKeyResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.PublicKey);
        _mockInnerProcessor.Verify(
            x => x.GetAsymmetricSignatureVerificationKeyAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.DeleteUserAsync(userId);

        Assert.Equal(DeleteUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_Succeeds_WhenUserDeletesOwnAccount()
    {
        var userId = Guid.CreateVersion7();
        var expectedResult = new DeleteUserResult(DeleteUserResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(true);
        _mockInnerProcessor.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_CallsAuthorizationProvider()
    {
        var request = new AssignRolesToUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        var expectedResult = new AssignRolesToUserResult(
            AssignRolesToUserResultCode.Success,
            "",
            2,
            []
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    request.UserId
                )
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    It.IsAny<Guid[]>()
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.AssignRolesToUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignRolesToUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    request.UserId
                ),
            Times.Once
        );
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    request.RoleIds[0],
                    request.RoleIds[1]
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.AssignRolesToUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new AssignRolesToUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    request.UserId
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AssignRolesToUserAsync(It.IsAny<AssignRolesToUserRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AssignRolesToUserAsync_ReturnsUnauthorized_WhenNotAuthorizedToReadRoles()
    {
        var request = new AssignRolesToUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    request.UserId
                )
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    It.IsAny<Guid[]>()
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AssignRolesToUserAsync(It.IsAny<AssignRolesToUserRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AssignOwnerRolesToUserAsync_DelegatesWithoutAuthCheck()
    {
        var roleId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var expectedResult = new AssignRolesToUserResult(
            AssignRolesToUserResultCode.Success,
            "",
            1,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AssignOwnerRolesToUserAsync(roleId, userId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignOwnerRolesToUserAsync(roleId, userId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.AssignOwnerRolesToUserAsync(roleId, userId), Times.Once);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_CallsAuthorizationProvider()
    {
        var request = new RemoveRolesFromUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        var expectedResult = new RemoveRolesFromUserResult(
            RemoveRolesFromUserResultCode.Success,
            "",
            2,
            []
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    request.UserId
                )
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    It.IsAny<Guid[]>()
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.RemoveRolesFromUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveRolesFromUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    request.UserId
                ),
            Times.Once
        );
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    request.RoleIds[0],
                    request.RoleIds[1]
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.RemoveRolesFromUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new RemoveRolesFromUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    request.UserId
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RemoveRolesFromUserAsync(It.IsAny<RemoveRolesFromUserRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_ReturnsUnauthorized_WhenNotAuthorizedToReadRoles()
    {
        var request = new RemoveRolesFromUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    request.UserId
                )
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    It.IsAny<Guid[]>()
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RemoveRolesFromUserAsync(It.IsAny<RemoveRolesFromUserRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetUserByIdAsync_Succeeds_WhenIsOwnUser()
    {
        var userId = Guid.CreateVersion7();
        var expectedResult = new GetResult<User>(
            RetrieveResultCode.Success,
            "",
            new User { Id = userId }
        );
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(true);
        _mockInnerProcessor
            .Setup(x => x.GetUserByIdAsync(userId, false))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserByIdAsync(userId, false);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.GetUserByIdAsync(userId, false), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_Succeeds_WhenAuthorizedViaPermission()
    {
        var userId = Guid.CreateVersion7();
        var expectedResult = new GetResult<User>(
            RetrieveResultCode.Success,
            "",
            new User { Id = userId }
        );
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, userId)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.GetUserByIdAsync(userId, false))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetUserByIdAsync(userId, false);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.GetUserByIdAsync(userId, false), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUnauthorized_WhenNotOwnUserAndNoPermission()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, userId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetUserByIdAsync(userId, false);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new UserProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new UserProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
