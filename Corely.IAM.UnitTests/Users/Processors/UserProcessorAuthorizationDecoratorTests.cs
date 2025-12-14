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
            1,
            Guid.NewGuid()
        );
        _mockInnerProcessor.Setup(x => x.CreateUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateUserAsync(request);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task GetUserAsyncById_CallsAuthorizationProviderWithResourceId()
    {
        var userId = 5;
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
        var userId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, userId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(GetUserResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.User);
        _mockInnerProcessor.Verify(x => x.GetUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_Succeeds_WhenUserUpdatesOwnAccount()
    {
        var userId = 5;
        var user = new User { Id = userId, Username = "testuser" };
        var expectedResult = new UpdateUserResult(UpdateUserResultCode.Success, string.Empty);
        _mockAuthorizationProvider.Setup(x => x.IsAuthorizedForOwnUser(userId)).Returns(true);
        _mockInnerProcessor.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(expectedResult);

        var result = await _decorator.UpdateUserAsync(user);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var user = new User { Id = 5, Username = "testuser" };
        _mockAuthorizationProvider.Setup(x => x.IsAuthorizedForOwnUser(user.Id)).Returns(false);

        var result = await _decorator.UpdateUserAsync(user);

        Assert.Equal(UpdateUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_CallsAuthorizationProviderWithResourceId()
    {
        var userId = 5;
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
        var userId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, userId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetAsymmetricSignatureVerificationKeyAsync(userId);

        Assert.Equal(GetAsymmetricKeyResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.PublicKey);
        _mockInnerProcessor.Verify(
            x => x.GetAsymmetricSignatureVerificationKeyAsync(It.IsAny<int>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = 5;
        _mockAuthorizationProvider.Setup(x => x.IsAuthorizedForOwnUser(userId)).Returns(false);

        var result = await _decorator.DeleteUserAsync(userId);

        Assert.Equal(DeleteUserResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_Succeeds_WhenUserDeletesOwnAccount()
    {
        var userId = 5;
        var expectedResult = new DeleteUserResult(DeleteUserResultCode.Success, "");
        _mockAuthorizationProvider.Setup(x => x.IsAuthorizedForOwnUser(userId)).Returns(true);
        _mockInnerProcessor.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(userId), Times.Once);
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
