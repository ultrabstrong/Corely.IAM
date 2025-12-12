using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Services;

public class AuthenticationServiceTests
{
    private const int MAX_LOGIN_ATTEMPTS = 5;

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IAuthenticationProvider> _authenticationProviderMock;
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock;
    private readonly AuthenticationService _authenticationService;

    private UserEntity? _testUserEntity;

    public AuthenticationServiceTests()
    {
        _authenticationProviderMock = GetMockAuthenticationProvider();
        _basicAuthProcessorMock = GetMockBasicAuthProcessor();

        _authenticationService = new AuthenticationService(
            _serviceFactory.GetRequiredService<ILogger<AuthenticationService>>(),
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _authenticationProviderMock.Object,
            _basicAuthProcessorMock.Object,
            Options.Create(new SecurityOptions() { MaxLoginAttempts = MAX_LOGIN_ATTEMPTS })
        );
    }

    private async Task<UserEntity> CreateTestUserAsync()
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var userEntity = new UserEntity
        {
            Username = _fixture.Create<string>(),
            Email = _fixture.Create<string>() + "@test.com",
            TotalSuccessfulLogins = 0,
            LastSuccessfulLoginUtc = null,
            TotalFailedLogins = 0,
            FailedLoginsSinceLastSuccess = 0,
            LastFailedLoginUtc = null,
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        _testUserEntity = await userRepo.CreateAsync(userEntity);
        return _testUserEntity;
    }

    private static Mock<IAuthenticationProvider> GetMockAuthenticationProvider()
    {
        var mock = new Mock<IAuthenticationProvider>();

        mock.Setup(m => m.GetUserAuthTokenAsync(It.IsAny<UserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(UserAuthTokenResultCode.Success, "test-token", [], null)
            );

        return mock;
    }

    private static Mock<IBasicAuthProcessor> GetMockBasicAuthProcessor()
    {
        var mock = new Mock<IBasicAuthProcessor>();

        mock.Setup(m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(true);

        return mock;
    }

    [Fact]
    public async Task SignInAsync_SucceedsAndUpdateSuccessfulLogin_WhenUserExistsAndPasswordIsValid()
    {
        var userEntity = await CreateTestUserAsync();
        var request = new SignInRequest(userEntity.Username, _fixture.Create<string>());

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);

        // Verify the user was updated in the repo
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var updatedUser = await userRepo.GetAsync(u => u.Id == userEntity.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.TotalSuccessfulLogins);
        Assert.NotNull(updatedUser.LastSuccessfulLoginUtc);
        Assert.Equal(0, updatedUser.FailedLoginsSinceLastSuccess);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenUserDoesNotExist()
    {
        var request = new SignInRequest(_fixture.Create<string>(), _fixture.Create<string>());

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.UserNotFoundError, result.ResultCode);
        Assert.Equal("User not found", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenUserIsLockedOut()
    {
        var userEntity = await CreateTestUserAsync();
        userEntity.FailedLoginsSinceLastSuccess = MAX_LOGIN_ATTEMPTS;
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        await userRepo.UpdateAsync(userEntity);

        var request = new SignInRequest(userEntity.Username, _fixture.Create<string>());

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.UserLockedError, result.ResultCode);
        Assert.Equal("User is locked out", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_FailsAndUpdatedFailedLogins_WhenPasswordIsInvalid()
    {
        var userEntity = await CreateTestUserAsync();
        var request = new SignInRequest(userEntity.Username, _fixture.Create<string>());

        _basicAuthProcessorMock
            .Setup(m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(false);

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.PasswordMismatchError, result.ResultCode);
        Assert.Equal("Invalid password", result.Message);
        Assert.Null(result.AuthToken);

        // Verify the user was updated with failed login info
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var updatedUser = await userRepo.GetAsync(u => u.Id == userEntity.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.TotalFailedLogins);
        Assert.Equal(1, updatedUser.FailedLoginsSinceLastSuccess);
        Assert.NotNull(updatedUser.LastFailedLoginUtc);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenSignatureKeyNotFound()
    {
        var userEntity = await CreateTestUserAsync();
        var request = new SignInRequest(userEntity.Username, _fixture.Create<string>());

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<UserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.SignatureKeyNotFound,
                    null,
                    [],
                    null
                )
            );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.SignatureKeyNotFoundError, result.ResultCode);
        Assert.Equal("User signature key not found", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenAccountNotFound()
    {
        var userEntity = await CreateTestUserAsync();
        var accountId = _fixture.Create<int>();
        var request = new SignInRequest(userEntity.Username, _fixture.Create<string>(), accountId);

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<UserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(UserAuthTokenResultCode.AccountNotFound, null, [], null)
            );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.AccountNotFoundError, result.ResultCode);
        Assert.Contains("not found for user", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _authenticationService.SignInAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task ValidateAuthTokenAsync_ReturnsTrue_WhenTokenIsValidAndUserIdMatches()
    {
        var userId = _fixture.Create<int>();
        var authToken = _fixture.Create<string>();
        var validationResult = new UserAuthTokenValidationResult(
            UserAuthTokenValidationResultCode.Success,
            userId,
            null
        );
        _authenticationProviderMock
            .Setup(m => m.ValidateUserAuthTokenAsync(authToken))
            .ReturnsAsync(validationResult);

        var result = await _authenticationService.ValidateAuthTokenAsync(userId, authToken);

        Assert.True(result);
        _authenticationProviderMock.Verify(
            m => m.ValidateUserAuthTokenAsync(authToken),
            Times.Once
        );
    }

    [Fact]
    public async Task ValidateAuthTokenAsync_ReturnsFalse_WhenTokenIsInvalid()
    {
        var userId = _fixture.Create<int>();
        var authToken = _fixture.Create<string>();
        var validationResult = new UserAuthTokenValidationResult(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            null,
            null
        );
        _authenticationProviderMock
            .Setup(m => m.ValidateUserAuthTokenAsync(authToken))
            .ReturnsAsync(validationResult);

        var result = await _authenticationService.ValidateAuthTokenAsync(userId, authToken);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAuthTokenAsync_ReturnsFalse_WhenUserIdDoesNotMatch()
    {
        var userId = _fixture.Create<int>();
        var authToken = _fixture.Create<string>();
        var validationResult = new UserAuthTokenValidationResult(
            UserAuthTokenValidationResultCode.Success,
            userId + 1,
            null
        );
        _authenticationProviderMock
            .Setup(m => m.ValidateUserAuthTokenAsync(authToken))
            .ReturnsAsync(validationResult);

        var result = await _authenticationService.ValidateAuthTokenAsync(userId, authToken);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAuthTokenAsync_Throws_WithNullAuthToken()
    {
        var ex = await Record.ExceptionAsync(() =>
            _authenticationService.ValidateAuthTokenAsync(1, null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignOutAsync_CallsAuthenticationProvider()
    {
        var userId = _fixture.Create<int>();
        var jti = _fixture.Create<string>();
        _authenticationProviderMock
            .Setup(m => m.RevokeUserAuthTokenAsync(userId, jti))
            .ReturnsAsync(true);

        var result = await _authenticationService.SignOutAsync(userId, jti);

        Assert.True(result);
        _authenticationProviderMock.Verify(
            m => m.RevokeUserAuthTokenAsync(userId, jti),
            Times.Once
        );
    }

    [Fact]
    public async Task SignOutAsync_Throws_WithNullJti()
    {
        var ex = await Record.ExceptionAsync(() => _authenticationService.SignOutAsync(1, null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignOutAllAsync_CallsAuthenticationProvider()
    {
        var userId = _fixture.Create<int>();

        await _authenticationService.SignOutAllAsync(userId);

        _authenticationProviderMock.Verify(m => m.RevokeAllUserAuthTokensAsync(userId), Times.Once);
    }
}
