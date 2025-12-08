using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Exceptions;
using Corely.IAM.Security.Processors;
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
        var expectedResult = new CreateUserResult(CreateUserResultCode.Success, "", 1);
        _mockInnerProcessor.Setup(x => x.CreateUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(It.IsAny<string>(), It.IsAny<AuthAction>(), It.IsAny<int?>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.CreateUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetUserAsyncById_CallsAuthorizationProviderWithResourceId()
    {
        var userId = 5;
        var expectedUser = new User { Id = userId, Username = "testuser" };
        _mockInnerProcessor.Setup(x => x.GetUserAsync(userId)).ReturnsAsync(expectedUser);

        var result = await _decorator.GetUserAsync(userId);

        Assert.Equal(expectedUser, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Read, userId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUserAsyncById_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var userId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Read, userId)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.USER_RESOURCE_TYPE,
                    AuthAction.Read.ToString(),
                    userId
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() => _decorator.GetUserAsync(userId));

        _mockInnerProcessor.Verify(x => x.GetUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetUserAsyncByName_CallsAuthorizationProvider()
    {
        var userName = "testuser";
        var expectedUser = new User { Id = 1, Username = userName };
        _mockInnerProcessor.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(expectedUser);

        var result = await _decorator.GetUserAsync(userName);

        Assert.Equal(expectedUser, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Read, null),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateUserAsync_CallsAuthorizationProviderWithResourceId()
    {
        var user = new User { Id = 5, Username = "testuser" };

        await _decorator.UpdateUserAsync(user);

        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Update, 5),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var user = new User { Id = 5, Username = "testuser" };
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Update, 5)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.USER_RESOURCE_TYPE,
                    AuthAction.Update.ToString(),
                    5
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() => _decorator.UpdateUserAsync(user));

        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_CallsAuthorizationProviderWithResourceId()
    {
        var userId = 5;
        var expectedToken = "test-token";
        _mockInnerProcessor.Setup(x => x.GetUserAuthTokenAsync(userId)).ReturnsAsync(expectedToken);

        var result = await _decorator.GetUserAuthTokenAsync(userId);

        Assert.Equal(expectedToken, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Read, userId),
            Times.Once
        );
    }

    [Fact]
    public async Task IsUserAuthTokenValidAsync_CallsAuthorizationProviderWithResourceId()
    {
        var userId = 5;
        var authToken = "test-token";
        _mockInnerProcessor
            .Setup(x => x.IsUserAuthTokenValidAsync(userId, authToken))
            .ReturnsAsync(true);

        var result = await _decorator.IsUserAuthTokenValidAsync(userId, authToken);

        Assert.True(result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Read, userId),
            Times.Once
        );
    }

    [Fact]
    public async Task RevokeUserAuthTokenAsync_CallsAuthorizationProviderWithResourceId()
    {
        var userId = 5;
        var jti = "test-jti";
        _mockInnerProcessor.Setup(x => x.RevokeUserAuthTokenAsync(userId, jti)).ReturnsAsync(true);

        var result = await _decorator.RevokeUserAuthTokenAsync(userId, jti);

        Assert.True(result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Update, userId),
            Times.Once
        );
    }

    [Fact]
    public async Task RevokeUserAuthTokenAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var userId = 5;
        var jti = "test-jti";
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Update, userId)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.USER_RESOURCE_TYPE,
                    AuthAction.Update.ToString(),
                    userId
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.RevokeUserAuthTokenAsync(userId, jti)
        );

        _mockInnerProcessor.Verify(
            x => x.RevokeUserAuthTokenAsync(It.IsAny<int>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RevokeAllUserAuthTokensAsync_CallsAuthorizationProviderWithResourceId()
    {
        var userId = 5;

        await _decorator.RevokeAllUserAuthTokensAsync(userId);

        _mockAuthorizationProvider.Verify(
            x =>
                x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Update, userId),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.RevokeAllUserAuthTokensAsync(userId), Times.Once);
    }

    [Fact]
    public async Task RevokeAllUserAuthTokensAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var userId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Update, userId)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.USER_RESOURCE_TYPE,
                    AuthAction.Update.ToString(),
                    userId
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.RevokeAllUserAuthTokensAsync(userId)
        );

        _mockInnerProcessor.Verify(
            x => x.RevokeAllUserAuthTokensAsync(It.IsAny<int>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteUserAsync_CallsAuthorizationProvider()
    {
        var userId = 5;
        var expectedResult = new DeleteUserResult(DeleteUserResultCode.Success, "");
        _mockInnerProcessor.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Delete, userId),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var userId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Delete, userId)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.USER_RESOURCE_TYPE,
                    AuthAction.Delete.ToString(),
                    userId
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() => _decorator.DeleteUserAsync(userId));

        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(It.IsAny<int>()), Times.Never);
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
