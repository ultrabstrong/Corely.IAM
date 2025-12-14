using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Services;

public class AuthenticationServiceTests
{
    private const int MAX_LOGIN_ATTEMPTS = 5;

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IAuthenticationProvider> _authenticationProviderMock;
    private readonly Mock<IUserContextSetter> _userContextSetterMock;
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock;
    private readonly AuthenticationService _authenticationService;

    private UserEntity? _testUserEntity;

    public AuthenticationServiceTests()
    {
        _authenticationProviderMock = GetMockAuthenticationProvider();
        _userContextSetterMock = GetMockUserContextSetter();
        _basicAuthProcessorMock = GetMockBasicAuthProcessor();

        _authenticationService = new AuthenticationService(
            _serviceFactory.GetRequiredService<ILogger<AuthenticationService>>(),
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _authenticationProviderMock.Object,
            _userContextSetterMock.Object,
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

    private async Task<AccountEntity> CreateTestAccountAsync()
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = new AccountEntity { AccountName = _fixture.Create<string>() };
        return await accountRepo.CreateAsync(accountEntity);
    }

    private static Mock<IAuthenticationProvider> GetMockAuthenticationProvider()
    {
        var mock = new Mock<IAuthenticationProvider>();

        mock.Setup(m => m.GetUserAuthTokenAsync(It.IsAny<UserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.Success,
                    "test-token",
                    "test-token-id",
                    [],
                    null
                )
            );

        return mock;
    }

    private static Mock<IUserContextSetter> GetMockUserContextSetter()
    {
        var mock = new Mock<IUserContextSetter>();
        return mock;
    }

    private static Mock<IBasicAuthProcessor> GetMockBasicAuthProcessor()
    {
        var mock = new Mock<IBasicAuthProcessor>();

        mock.Setup(m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(
                new VerifyBasicAuthResult(VerifyBasicAuthResultCode.Success, string.Empty, true)
            );

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
            .ReturnsAsync(
                new VerifyBasicAuthResult(VerifyBasicAuthResultCode.Success, string.Empty, false)
            );

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
        var accountPublicId = Guid.NewGuid();
        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            accountPublicId
        );

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<UserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.AccountNotFound,
                    null,
                    null,
                    [],
                    null
                )
            );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.AccountNotFoundError, result.ResultCode);
        Assert.Contains("not found", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_Succeeds_WithValidAccountPublicId()
    {
        var userEntity = await CreateTestUserAsync();
        var accountEntity = await CreateTestAccountAsync();
        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            accountEntity.PublicId
        );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        _authenticationProviderMock.Verify(
            m =>
                m.GetUserAuthTokenAsync(
                    It.Is<UserAuthTokenRequest>(r => r.AccountPublicId == accountEntity.PublicId)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SignInAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _authenticationService.SignInAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignOutAsync_CallsAuthenticationProvider()
    {
        var userId = _fixture.Create<int>();
        var tokenId = _fixture.Create<string>();
        _authenticationProviderMock
            .Setup(m => m.RevokeUserAuthTokenAsync(userId, tokenId))
            .ReturnsAsync(true);

        var result = await _authenticationService.SignOutAsync(userId, tokenId);

        Assert.True(result);
        _authenticationProviderMock.Verify(
            m => m.RevokeUserAuthTokenAsync(userId, tokenId),
            Times.Once
        );
        _userContextSetterMock.Verify(m => m.ClearUserContext(userId), Times.Once);
    }

    [Fact]
    public async Task SignOutAsync_Throws_WithNullTokenId()
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
        _userContextSetterMock.Verify(m => m.ClearUserContext(userId), Times.Once);
    }
}
