using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Exceptions;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Users.Processors;

public class UserProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IUserProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly Mock<IIamUserContextProvider> _mockUserContextProvider = new();
    private readonly UserProcessorAuthorizationDecorator _decorator;

    public UserProcessorAuthorizationDecoratorTests()
    {
        _decorator = new UserProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object,
            _mockUserContextProvider.Object
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
    public async Task UpdateUserAsync_Succeeds_WhenUserUpdatesOwnAccount()
    {
        var userId = 5;
        var user = new User { Id = userId, Username = "testuser" };
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(new IamUserContext(userId, 1));

        await _decorator.UpdateUserAsync(user);

        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ThrowsUserContextNotSetException_WhenNoUserContext()
    {
        var user = new User { Id = 5, Username = "testuser" };
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((IamUserContext?)null);

        await Assert.ThrowsAsync<UserContextNotSetException>(() =>
            _decorator.UpdateUserAsync(user)
        );

        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_ThrowsAuthorizationException_WhenUserUpdatesOtherUser()
    {
        var user = new User { Id = 5, Username = "testuser" };
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(new IamUserContext(99, 1)); // Different user

        await Assert.ThrowsAsync<AuthorizationException>(() => _decorator.UpdateUserAsync(user));

        _mockInnerProcessor.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_CallsAuthorizationProviderWithResourceId()
    {
        var userId = 5;
        var expectedKey = "test-key";
        _mockInnerProcessor
            .Setup(x => x.GetAsymmetricSignatureVerificationKeyAsync(userId))
            .ReturnsAsync(expectedKey);

        var result = await _decorator.GetAsymmetricSignatureVerificationKeyAsync(userId);

        Assert.Equal(expectedKey, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Read, userId),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteUserAsync_ThrowsUserContextNotSetException_WhenNoUserContext()
    {
        var userId = 5;
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((IamUserContext?)null);

        await Assert.ThrowsAsync<UserContextNotSetException>(() =>
            _decorator.DeleteUserAsync(userId)
        );

        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_Succeeds_WhenUserDeletesOwnAccount()
    {
        var userId = 5;
        var expectedResult = new DeleteUserResult(DeleteUserResultCode.Success, "");
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(new IamUserContext(userId, 1));
        _mockInnerProcessor.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteUserAsync(userId);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ThrowsAuthorizationException_WhenUserDeletesOtherUser()
    {
        var userId = 5;
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(new IamUserContext(99, 1));

        await Assert.ThrowsAsync<AuthorizationException>(() => _decorator.DeleteUserAsync(userId));

        _mockInnerProcessor.Verify(x => x.DeleteUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new UserProcessorAuthorizationDecorator(
                null!,
                _mockAuthorizationProvider.Object,
                _mockUserContextProvider.Object
            )
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new UserProcessorAuthorizationDecorator(
                _mockInnerProcessor.Object,
                null!,
                _mockUserContextProvider.Object
            )
        );

    [Fact]
    public void Constructor_ThrowsOnNullUserContextProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new UserProcessorAuthorizationDecorator(
                _mockInnerProcessor.Object,
                _mockAuthorizationProvider.Object,
                null!
            )
        );
}
