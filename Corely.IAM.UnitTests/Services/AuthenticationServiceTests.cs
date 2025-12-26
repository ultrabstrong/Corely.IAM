using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
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
    private const string TEST_DEVICE_ID = "test-device";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IAuthenticationProvider> _authenticationProviderMock;
    private readonly Mock<IUserContextProvider> _userContextProviderMock;
    private readonly Mock<IUserContextSetter> _userContextSetterMock;
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock;
    private readonly AuthenticationService _authenticationService;

    private UserEntity? _testUserEntity;

    public AuthenticationServiceTests()
    {
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _fixture.Customize<Account>(c =>
            c.Without(a => a.SymmetricKeys).Without(a => a.AsymmetricKeys)
        );

        _authenticationProviderMock = GetMockAuthenticationProvider();
        _userContextProviderMock = GetMockUserContextProvider();
        _userContextSetterMock = GetMockUserContextSetter();
        _basicAuthProcessorMock = GetMockBasicAuthProcessor();

        _authenticationService = new AuthenticationService(
            _serviceFactory.GetRequiredService<ILogger<AuthenticationService>>(),
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _authenticationProviderMock.Object,
            _userContextProviderMock.Object,
            _userContextSetterMock.Object,
            _basicAuthProcessorMock.Object,
            Options.Create(new SecurityOptions() { MaxLoginAttempts = MAX_LOGIN_ATTEMPTS }),
            TimeProvider.System
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

    private Mock<IAuthenticationProvider> GetMockAuthenticationProvider()
    {
        var mock = new Mock<IAuthenticationProvider>();

        mock.Setup(m => m.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.Success,
                    "test-token",
                    "test-token-id",
                    new User() { Id = 1 },
                    null,
                    [.. _fixture.CreateMany<Account>()]
                )
            );

        mock.Setup(m => m.ValidateUserAuthTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    new User() { Id = 1 },
                    null,
                    TEST_DEVICE_ID,
                    []
                )
            );

        return mock;
    }

    private static Mock<IUserContextProvider> GetMockUserContextProvider()
    {
        var mock = new Mock<IUserContextProvider>();
        mock.Setup(m => m.GetUserContext())
            .Returns(new UserContext(new User() { Id = 1 }, null, TEST_DEVICE_ID, []));
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
        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        Assert.NotNull(result.AuthToken);

        // Verify context was set
        _userContextSetterMock.Verify(m => m.SetUserContext(It.IsAny<UserContext>()), Times.Once);

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
        var request = new SignInRequest(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );

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

        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.UserLockedError, result.ResultCode);
        Assert.Equal("User is locked out", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_FailsAndUpdatedFailedLogins_WhenPasswordIsInvalid()
    {
        var userEntity = await CreateTestUserAsync();
        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );

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
        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.SignatureKeyNotFound,
                    null,
                    null,
                    null,
                    null,
                    []
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
            TEST_DEVICE_ID,
            accountPublicId
        );

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.AccountNotFound,
                    null,
                    null,
                    null,
                    null,
                    []
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
            TEST_DEVICE_ID,
            accountEntity.PublicId
        );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        _authenticationProviderMock.Verify(
            m =>
                m.GetUserAuthTokenAsync(
                    It.Is<GetUserAuthTokenRequest>(r => r.AccountPublicId == accountEntity.PublicId)
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
    public async Task SwitchAccountAsync_Succeeds_WithValidTokenAndAccount()
    {
        var accountPublicId = Guid.NewGuid();
        var request = new SwitchAccountRequest("valid-token", accountPublicId);

        var result = await _authenticationService.SwitchAccountAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        Assert.NotNull(result.AuthToken);
        _authenticationProviderMock.Verify(
            m => m.ValidateUserAuthTokenAsync("valid-token"),
            Times.Once
        );
        _authenticationProviderMock.Verify(
            m =>
                m.GetUserAuthTokenAsync(
                    It.Is<GetUserAuthTokenRequest>(r => r.AccountPublicId == accountPublicId)
                ),
            Times.Once
        );
        _userContextSetterMock.Verify(m => m.SetUserContext(It.IsAny<UserContext>()), Times.Once);
    }

    [Fact]
    public async Task SwitchAccountAsync_Fails_WhenTokenValidationFails()
    {
        var request = new SwitchAccountRequest("invalid-token", Guid.NewGuid());

        _authenticationProviderMock
            .Setup(m => m.ValidateUserAuthTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.TokenValidationFailed,
                    null,
                    null,
                    null,
                    []
                )
            );

        var result = await _authenticationService.SwitchAccountAsync(request);

        Assert.Equal(SignInResultCode.InvalidAuthTokenError, result.ResultCode);
        Assert.Contains("validation failed", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SwitchAccountAsync_Fails_WhenAccountNotFound()
    {
        var accountPublicId = Guid.NewGuid();
        var request = new SwitchAccountRequest("valid-token", accountPublicId);

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.AccountNotFound,
                    null,
                    null,
                    null,
                    null,
                    []
                )
            );

        var result = await _authenticationService.SwitchAccountAsync(request);

        Assert.Equal(SignInResultCode.AccountNotFoundError, result.ResultCode);
        Assert.Contains("not found", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SwitchAccountAsync_Fails_WhenSignatureKeyNotFound()
    {
        var request = new SwitchAccountRequest("valid-token", Guid.NewGuid());

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.SignatureKeyNotFound,
                    null,
                    null,
                    null,
                    null,
                    []
                )
            );

        var result = await _authenticationService.SwitchAccountAsync(request);

        Assert.Equal(SignInResultCode.SignatureKeyNotFoundError, result.ResultCode);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SwitchAccountAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _authenticationService.SwitchAccountAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignOutAsync_CallsAuthenticationProvider()
    {
        var userId = 1;
        var tokenId = _fixture.Create<string>();
        int? accountId = null;
        var signOutRequest = new SignOutRequest(tokenId);

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(new UserContext(new User() { Id = 1 }, null, TEST_DEVICE_ID, []));

        _authenticationProviderMock
            .Setup(m =>
                m.RevokeUserAuthTokenAsync(
                    It.Is<RevokeUserAuthTokenRequest>(r =>
                        r.UserId == userId
                        && r.TokenId == tokenId
                        && r.DeviceId == TEST_DEVICE_ID
                        && r.AccountId == accountId
                    )
                )
            )
            .ReturnsAsync(true);

        var result = await _authenticationService.SignOutAsync(signOutRequest);

        Assert.True(result);
        _authenticationProviderMock.Verify(
            m =>
                m.RevokeUserAuthTokenAsync(
                    It.Is<RevokeUserAuthTokenRequest>(r =>
                        r.UserId == userId
                        && r.TokenId == tokenId
                        && r.DeviceId == TEST_DEVICE_ID
                        && r.AccountId == accountId
                    )
                ),
            Times.Once
        );
        _userContextSetterMock.Verify(m => m.ClearUserContext(userId), Times.Once);
    }

    [Fact]
    public async Task SignOutAsync_ReturnsFalse_WhenNoUserContext()
    {
        var signOutRequest = new SignOutRequest("token-id");

        _userContextProviderMock.Setup(m => m.GetUserContext()).Returns((UserContext?)null);

        var result = await _authenticationService.SignOutAsync(signOutRequest);

        Assert.False(result);
        _authenticationProviderMock.Verify(
            m => m.RevokeUserAuthTokenAsync(It.IsAny<RevokeUserAuthTokenRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SignOutAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _authenticationService.SignOutAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignOutAllAsync_CallsAuthenticationProvider()
    {
        var userId = 1;

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(new UserContext(new User() { Id = userId }, null, TEST_DEVICE_ID, []));

        await _authenticationService.SignOutAllAsync();

        _authenticationProviderMock.Verify(m => m.RevokeAllUserAuthTokensAsync(userId), Times.Once);
        _userContextSetterMock.Verify(m => m.ClearUserContext(userId), Times.Once);
    }

    [Fact]
    public async Task SignOutAllAsync_DoesNothing_WhenNoUserContext()
    {
        _userContextProviderMock.Setup(m => m.GetUserContext()).Returns((UserContext?)null);

        await _authenticationService.SignOutAllAsync();

        _authenticationProviderMock.Verify(
            m => m.RevokeAllUserAuthTokensAsync(It.IsAny<int>()),
            Times.Never
        );
        _userContextSetterMock.Verify(m => m.ClearUserContext(It.IsAny<int>()), Times.Never);
    }
}
