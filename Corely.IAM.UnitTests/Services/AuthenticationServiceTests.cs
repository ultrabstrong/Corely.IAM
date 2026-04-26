using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.GoogleAuths.Providers;
using Corely.IAM.MfaChallenges.Constants;
using Corely.IAM.MfaChallenges.Entities;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Services;

public class AuthenticationServiceTests
{
    private const int MAX_LOGIN_ATTEMPTS = 5;
    private const int LOCKOUT_COOLDOWN_SECONDS = 900;
    private const string TEST_DEVICE_ID = "test-device";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IAuthenticationProvider> _authenticationProviderMock;
    private readonly Mock<IUserContextProvider> _userContextProviderMock;
    private readonly Mock<IUserContextSetter> _userContextSetterMock;
    private readonly Mock<IAuthorizationCacheClearer> _authorizationCacheClearerMock;
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock;
    private readonly Mock<ITotpAuthProcessor> _totpAuthProcessorMock;
    private readonly Mock<IGoogleAuthProcessor> _googleAuthProcessorMock;
    private readonly Mock<IGoogleIdTokenValidator> _googleIdTokenValidatorMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
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
        _authorizationCacheClearerMock = new Mock<IAuthorizationCacheClearer>();
        _basicAuthProcessorMock = GetMockBasicAuthProcessor();
        _totpAuthProcessorMock = new Mock<ITotpAuthProcessor>();
        _googleAuthProcessorMock = new Mock<IGoogleAuthProcessor>();
        _googleIdTokenValidatorMock = new Mock<IGoogleIdTokenValidator>();
        _timeProviderMock = new Mock<TimeProvider>();
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

        _authenticationService = new AuthenticationService(
            _serviceFactory.GetRequiredService<ILogger<AuthenticationService>>(),
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _authenticationProviderMock.Object,
            _userContextProviderMock.Object,
            _userContextSetterMock.Object,
            _authorizationCacheClearerMock.Object,
            _basicAuthProcessorMock.Object,
            _totpAuthProcessorMock.Object,
            _googleAuthProcessorMock.Object,
            _googleIdTokenValidatorMock.Object,
            _serviceFactory.GetRequiredService<IRepo<MfaChallengeEntity>>(),
            Options.Create(
                new SecurityOptions()
                {
                    MaxLoginAttempts = MAX_LOGIN_ATTEMPTS,
                    LockoutCooldownSeconds = LOCKOUT_COOLDOWN_SECONDS,
                }
            ),
            _timeProviderMock.Object
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
                    Guid.CreateVersion7(),
                    new User() { Id = Guid.CreateVersion7() },
                    null,
                    [.. _fixture.CreateMany<Account>()]
                )
            );

        mock.Setup(m => m.ValidateUserAuthTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    new User() { Id = Guid.CreateVersion7() },
                    null,
                    TEST_DEVICE_ID,
                    Guid.CreateVersion7(),
                    []
                )
            );

        return mock;
    }

    private static Mock<IUserContextProvider> GetMockUserContextProvider()
    {
        var mock = new Mock<IUserContextProvider>();
        mock.Setup(m => m.GetUserContext())
            .Returns(
                new UserContext(new User() { Id = Guid.CreateVersion7() }, null, TEST_DEVICE_ID, [])
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
    public async Task SignIn_SucceedsAndUpdateSuccessfulLogin_WhenUserExistsAndPasswordIsValid()
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
    public async Task SignIn_Fails_WhenUserDoesNotExist()
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
    public async Task SignIn_Fails_WhenUserIsLockedOut()
    {
        var now = DateTimeOffset.UtcNow;
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(now);

        var userEntity = await CreateTestUserAsync();
        userEntity.LockedUtc = now.UtcDateTime.AddSeconds(-10);
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
    public async Task SignIn_LockedUser_AfterCooldownExpires_AllowsRetry()
    {
        var now = DateTimeOffset.UtcNow;
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(now);

        var userEntity = await CreateTestUserAsync();
        userEntity.LockedUtc = now.UtcDateTime.AddSeconds(-(LOCKOUT_COOLDOWN_SECONDS + 1));
        userEntity.FailedLoginsSinceLastSuccess = MAX_LOGIN_ATTEMPTS;
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        await userRepo.UpdateAsync(userEntity);

        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        Assert.NotNull(result.AuthToken);

        var updatedUser = await userRepo.GetAsync(u => u.Id == userEntity.Id);
        Assert.NotNull(updatedUser);
        Assert.Null(updatedUser.LockedUtc);
        Assert.Equal(0, updatedUser.FailedLoginsSinceLastSuccess);
    }

    [Fact]
    public async Task SignIn_LockedUser_AfterCooldownExpires_FailedAttempt_IncrementsFromZero()
    {
        var now = DateTimeOffset.UtcNow;
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(now);

        var userEntity = await CreateTestUserAsync();
        userEntity.LockedUtc = now.UtcDateTime.AddSeconds(-(LOCKOUT_COOLDOWN_SECONDS + 1));
        userEntity.FailedLoginsSinceLastSuccess = MAX_LOGIN_ATTEMPTS;
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        await userRepo.UpdateAsync(userEntity);

        _basicAuthProcessorMock
            .Setup(m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(
                new VerifyBasicAuthResult(VerifyBasicAuthResultCode.Success, string.Empty, false)
            );

        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.PasswordMismatchError, result.ResultCode);

        var updatedUser = await userRepo.GetAsync(u => u.Id == userEntity.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.FailedLoginsSinceLastSuccess);
    }

    [Fact]
    public async Task SignIn_FailsAndUpdatedFailedLogins_WhenPasswordIsInvalid()
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
    public async Task SignIn_Fails_WhenSignatureKeyNotFound()
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
                    UserAuthTokenResultCode.SignatureKeyNotFoundError,
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
    public async Task SignIn_Fails_WhenAccountNotFound()
    {
        var userEntity = await CreateTestUserAsync();
        var accountId = Guid.CreateVersion7();
        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID,
            accountId
        );

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.AccountNotFoundError,
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
    public async Task SignIn_Succeeds_WithValidAccountId()
    {
        var userEntity = await CreateTestUserAsync();
        var accountEntity = await CreateTestAccountAsync();
        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID,
            accountEntity.Id
        );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        _authenticationProviderMock.Verify(
            m =>
                m.GetUserAuthTokenAsync(
                    It.Is<GetUserAuthTokenRequest>(r => r.AccountId == accountEntity.Id)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SignIn_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _authenticationService.SignInAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SwitchAccount_Succeeds_WhenContextExists()
    {
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(new UserContext(new User() { Id = userId }, null, TEST_DEVICE_ID, []));

        var request = new SwitchAccountRequest(accountId);
        var result = await _authenticationService.SwitchAccountAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        Assert.NotNull(result.AuthToken);
        _authenticationProviderMock.Verify(
            m =>
                m.GetUserAuthTokenAsync(
                    It.Is<GetUserAuthTokenRequest>(r =>
                        r.AccountId == accountId
                        && r.UserId == userId
                        && r.DeviceId == TEST_DEVICE_ID
                    )
                ),
            Times.Once
        );
        _userContextSetterMock.Verify(m => m.SetUserContext(It.IsAny<UserContext>()), Times.Once);
    }

    [Fact]
    public async Task SwitchAccount_Fails_WhenNoUserContext()
    {
        var request = new SwitchAccountRequest(Guid.CreateVersion7());

        _userContextProviderMock.Setup(m => m.GetUserContext()).Returns((UserContext?)null);

        var result = await _authenticationService.SwitchAccountAsync(request);

        Assert.Equal(SignInResultCode.InvalidAuthTokenError, result.ResultCode);
        Assert.Contains("No user context", result.Message);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SwitchAccount_Fails_WhenAccountNotFound()
    {
        var accountId = Guid.CreateVersion7();
        var request = new SwitchAccountRequest(accountId);

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(
                new UserContext(new User() { Id = Guid.CreateVersion7() }, null, TEST_DEVICE_ID, [])
            );

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.AccountNotFoundError,
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
    public async Task SwitchAccount_Fails_WhenSignatureKeyNotFound()
    {
        var request = new SwitchAccountRequest(Guid.CreateVersion7());

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(
                new UserContext(new User() { Id = Guid.CreateVersion7() }, null, TEST_DEVICE_ID, [])
            );

        _authenticationProviderMock
            .Setup(m => m.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.SignatureKeyNotFoundError,
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
    public async Task SwitchAccount_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _authenticationService.SwitchAccountAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignOut_CallsAuthenticationProvider()
    {
        var userId = Guid.CreateVersion7();
        var tokenId = _fixture.Create<string>();
        Guid? accountId = null;
        var signOutRequest = new SignOutRequest(tokenId);

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(new UserContext(new User() { Id = userId }, null, TEST_DEVICE_ID, []));

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
    public async Task SignOut_ReturnsFalse_WhenNoUserContext()
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
    public async Task SignOut_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _authenticationService.SignOutAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignOutAll_CallsAuthenticationProvider()
    {
        var userId = Guid.CreateVersion7();

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(new UserContext(new User() { Id = userId }, null, TEST_DEVICE_ID, []));

        await _authenticationService.SignOutAllAsync();

        _authenticationProviderMock.Verify(m => m.RevokeAllUserAuthTokensAsync(userId), Times.Once);
        _userContextSetterMock.Verify(m => m.ClearUserContext(userId), Times.Once);
    }

    [Fact]
    public async Task SignOutAll_DoesNothing_WhenNoUserContext()
    {
        _userContextProviderMock.Setup(m => m.GetUserContext()).Returns((UserContext?)null);

        await _authenticationService.SignOutAllAsync();

        _authenticationProviderMock.Verify(
            m => m.RevokeAllUserAuthTokensAsync(It.IsAny<Guid>()),
            Times.Never
        );
        _userContextSetterMock.Verify(m => m.ClearUserContext(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ListSessions_ReturnsSessionsForCurrentUser()
    {
        var userId = Guid.CreateVersion7();
        var currentSessionId = Guid.CreateVersion7();
        var expectedSessions = new List<UserSession>
        {
            new(
                currentSessionId,
                TEST_DEVICE_ID,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                true
            ),
            new(
                Guid.CreateVersion7(),
                "other-device",
                Guid.CreateVersion7(),
                DateTime.UtcNow.AddMinutes(-5),
                DateTime.UtcNow.AddHours(1),
                false
            ),
        };

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(
                new UserContext(
                    new User() { Id = userId },
                    null,
                    TEST_DEVICE_ID,
                    [],
                    currentSessionId
                )
            );
        _authenticationProviderMock
            .Setup(m => m.ListUserSessionsAsync(userId, currentSessionId))
            .ReturnsAsync(expectedSessions);

        var result = await _authenticationService.ListSessionsAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.Equal(currentSessionId, result.Data.Items[0].SessionId);
    }

    [Fact]
    public async Task RevokeSession_ClearsContext_WhenRevokingCurrentSession()
    {
        var userId = Guid.CreateVersion7();
        var currentSessionId = Guid.CreateVersion7();

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(
                new UserContext(
                    new User() { Id = userId },
                    null,
                    TEST_DEVICE_ID,
                    [],
                    currentSessionId
                )
            );
        _authenticationProviderMock
            .Setup(m => m.RevokeUserAuthTokenByIdAsync(userId, currentSessionId))
            .ReturnsAsync(true);

        var result = await _authenticationService.RevokeSessionAsync(
            new RevokeSessionRequest(currentSessionId)
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        _userContextSetterMock.Verify(m => m.ClearUserContext(userId), Times.Once);
        _authorizationCacheClearerMock.Verify(m => m.ClearCache(), Times.Once);
    }

    [Fact]
    public async Task RevokeOtherSessions_RevokesAllExceptCurrentSession()
    {
        var userId = Guid.CreateVersion7();
        var currentSessionId = Guid.CreateVersion7();

        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(
                new UserContext(
                    new User() { Id = userId },
                    null,
                    TEST_DEVICE_ID,
                    [],
                    currentSessionId
                )
            );
        _authenticationProviderMock
            .Setup(m => m.RevokeOtherUserAuthTokensAsync(userId, currentSessionId))
            .ReturnsAsync(true);

        var result = await _authenticationService.RevokeOtherSessionsAsync();

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        _authenticationProviderMock.Verify(
            m => m.RevokeOtherUserAuthTokensAsync(userId, currentSessionId),
            Times.Once
        );
        _userContextSetterMock.Verify(m => m.ClearUserContext(It.IsAny<Guid>()), Times.Never);
    }

    #region MFA Flow Tests

    [Fact]
    public async Task SignIn_ReturnsMfaRequiredChallenge_WhenTotpIsEnabled()
    {
        var userEntity = await CreateTestUserAsync();
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(userEntity.Id)).ReturnsAsync(true);

        var request = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );

        var result = await _authenticationService.SignInAsync(request);

        Assert.Equal(SignInResultCode.MfaRequiredChallenge, result.ResultCode);
        Assert.NotNull(result.MfaChallengeToken);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInWithGoogle_ReturnsSuccess_WhenValidTokenAndNoMfa()
    {
        var userEntity = await CreateTestUserAsync();
        var googleSubject = "google-subject-123";

        _googleIdTokenValidatorMock
            .Setup(v => v.ValidateAsync("valid-google-token"))
            .ReturnsAsync(new GoogleIdTokenPayload(googleSubject, "user@gmail.com", true));
        _googleAuthProcessorMock
            .Setup(m => m.GetUserIdByGoogleSubjectAsync(googleSubject))
            .ReturnsAsync(userEntity.Id);
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(userEntity.Id)).ReturnsAsync(false);

        var request = new SignInWithGoogleRequest("valid-google-token", TEST_DEVICE_ID);

        var result = await _authenticationService.SignInWithGoogleAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        Assert.NotNull(result.AuthToken);
    }

    [Fact]
    public async Task SignInWithGoogle_ReturnsMfaRequiredChallenge_WhenTotpIsEnabled()
    {
        var userEntity = await CreateTestUserAsync();
        var googleSubject = "google-subject-456";

        _googleIdTokenValidatorMock
            .Setup(v => v.ValidateAsync("valid-google-token"))
            .ReturnsAsync(new GoogleIdTokenPayload(googleSubject, "user@gmail.com", true));
        _googleAuthProcessorMock
            .Setup(m => m.GetUserIdByGoogleSubjectAsync(googleSubject))
            .ReturnsAsync(userEntity.Id);
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(userEntity.Id)).ReturnsAsync(true);

        var request = new SignInWithGoogleRequest("valid-google-token", TEST_DEVICE_ID);

        var result = await _authenticationService.SignInWithGoogleAsync(request);

        Assert.Equal(SignInResultCode.MfaRequiredChallenge, result.ResultCode);
        Assert.NotNull(result.MfaChallengeToken);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInWithGoogle_ReturnsInvalidGoogleTokenError_WhenTokenIsInvalid()
    {
        _googleIdTokenValidatorMock
            .Setup(v => v.ValidateAsync("bad-token"))
            .ReturnsAsync((GoogleIdTokenPayload?)null);

        var request = new SignInWithGoogleRequest("bad-token", TEST_DEVICE_ID);

        var result = await _authenticationService.SignInWithGoogleAsync(request);

        Assert.Equal(SignInResultCode.InvalidGoogleTokenError, result.ResultCode);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task VerifyMfa_ReturnsSuccess_WhenTotpCodeIsValid()
    {
        var userEntity = await CreateTestUserAsync();
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(userEntity.Id)).ReturnsAsync(true);

        // First sign in to create the MFA challenge
        var signInRequest = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );
        var signInResult = await _authenticationService.SignInAsync(signInRequest);
        Assert.Equal(SignInResultCode.MfaRequiredChallenge, signInResult.ResultCode);
        var challengeToken = signInResult.MfaChallengeToken!;

        // Set up TOTP verification to succeed
        _totpAuthProcessorMock
            .Setup(m =>
                m.VerifyTotpOrRecoveryCodeAsync(
                    It.Is<VerifyTotpOrRecoveryCodeRequest>(r =>
                        r.UserId == userEntity.Id && r.Code == "123456"
                    )
                )
            )
            .ReturnsAsync(
                new VerifyTotpOrRecoveryCodeResult(
                    VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid,
                    string.Empty
                )
            );

        var verifyRequest = new VerifyMfaRequest(challengeToken, "123456");
        var result = await _authenticationService.VerifyMfaAsync(verifyRequest);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        Assert.NotNull(result.AuthToken);
    }

    [Fact]
    public async Task VerifyMfa_ReturnsSuccess_WhenRecoveryCodeIsValid()
    {
        var userEntity = await CreateTestUserAsync();
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(userEntity.Id)).ReturnsAsync(true);

        // First sign in to create the MFA challenge
        var signInRequest = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );
        var signInResult = await _authenticationService.SignInAsync(signInRequest);
        var challengeToken = signInResult.MfaChallengeToken!;

        // Set up recovery code verification to succeed
        _totpAuthProcessorMock
            .Setup(m =>
                m.VerifyTotpOrRecoveryCodeAsync(
                    It.Is<VerifyTotpOrRecoveryCodeRequest>(r =>
                        r.UserId == userEntity.Id && r.Code == "ABCD-1234"
                    )
                )
            )
            .ReturnsAsync(
                new VerifyTotpOrRecoveryCodeResult(
                    VerifyTotpOrRecoveryCodeResultCode.RecoveryCodeValid,
                    string.Empty
                )
            );

        var verifyRequest = new VerifyMfaRequest(challengeToken, "ABCD-1234");
        var result = await _authenticationService.VerifyMfaAsync(verifyRequest);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
        Assert.NotNull(result.AuthToken);
    }

    [Fact]
    public async Task VerifyMfa_ReturnsInvalidMfaCodeError_WhenCodeIsInvalid()
    {
        var userEntity = await CreateTestUserAsync();
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(userEntity.Id)).ReturnsAsync(true);

        // First sign in to create the MFA challenge
        var signInRequest = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );
        var signInResult = await _authenticationService.SignInAsync(signInRequest);
        var challengeToken = signInResult.MfaChallengeToken!;

        // Set up TOTP verification to fail
        _totpAuthProcessorMock
            .Setup(m =>
                m.VerifyTotpOrRecoveryCodeAsync(It.IsAny<VerifyTotpOrRecoveryCodeRequest>())
            )
            .ReturnsAsync(
                new VerifyTotpOrRecoveryCodeResult(
                    VerifyTotpOrRecoveryCodeResultCode.InvalidCodeError,
                    "Invalid TOTP or recovery code"
                )
            );

        var verifyRequest = new VerifyMfaRequest(challengeToken, "000000");
        var result = await _authenticationService.VerifyMfaAsync(verifyRequest);

        Assert.Equal(SignInResultCode.InvalidMfaCodeError, result.ResultCode);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task VerifyMfa_ReturnsMfaChallengeExpiredError_WhenChallengeIsExpired()
    {
        var now = DateTimeOffset.UtcNow;
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(now);

        var userEntity = await CreateTestUserAsync();
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(userEntity.Id)).ReturnsAsync(true);

        // Sign in to create the MFA challenge
        var signInRequest = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );
        var signInResult = await _authenticationService.SignInAsync(signInRequest);
        var challengeToken = signInResult.MfaChallengeToken!;

        // Move time forward past the challenge timeout
        var expiredTime = now.AddSeconds(301);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(expiredTime);

        var verifyRequest = new VerifyMfaRequest(challengeToken, "123456");
        var result = await _authenticationService.VerifyMfaAsync(verifyRequest);

        Assert.Equal(SignInResultCode.MfaChallengeExpiredError, result.ResultCode);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task VerifyMfa_ReturnsMfaChallengeExpiredError_WhenChallengeTokenNotFound()
    {
        var verifyRequest = new VerifyMfaRequest("nonexistent-token", "123456");
        var result = await _authenticationService.VerifyMfaAsync(verifyRequest);

        Assert.Equal(SignInResultCode.MfaChallengeExpiredError, result.ResultCode);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task VerifyMfa_ReturnsMfaChallengeExpiredError_WhenChallengeAlreadyCompleted()
    {
        var userEntity = await CreateTestUserAsync();
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(userEntity.Id)).ReturnsAsync(true);
        _totpAuthProcessorMock
            .Setup(m =>
                m.VerifyTotpOrRecoveryCodeAsync(It.IsAny<VerifyTotpOrRecoveryCodeRequest>())
            )
            .ReturnsAsync(
                new VerifyTotpOrRecoveryCodeResult(
                    VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid,
                    string.Empty
                )
            );

        // Sign in to create the MFA challenge
        var signInRequest = new SignInRequest(
            userEntity.Username,
            _fixture.Create<string>(),
            TEST_DEVICE_ID
        );
        var signInResult = await _authenticationService.SignInAsync(signInRequest);
        var challengeToken = signInResult.MfaChallengeToken!;

        // Complete the challenge
        var verifyRequest = new VerifyMfaRequest(challengeToken, "123456");
        await _authenticationService.VerifyMfaAsync(verifyRequest);

        // Try to use the same challenge again
        var result = await _authenticationService.VerifyMfaAsync(verifyRequest);

        Assert.Equal(SignInResultCode.MfaChallengeExpiredError, result.ResultCode);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task VerifyMfa_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _authenticationService.VerifyMfaAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignInWithGoogle_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _authenticationService.SignInWithGoogleAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task SignInWithGoogle_ReturnsGoogleAuthNotLinkedError_WhenNoLinkedUser()
    {
        var googleSubject = "unlinked-subject";

        _googleIdTokenValidatorMock
            .Setup(v => v.ValidateAsync("valid-token"))
            .ReturnsAsync(new GoogleIdTokenPayload(googleSubject, "user@gmail.com", true));
        _googleAuthProcessorMock
            .Setup(m => m.GetUserIdByGoogleSubjectAsync(googleSubject))
            .ReturnsAsync((Guid?)null);

        var request = new SignInWithGoogleRequest("valid-token", TEST_DEVICE_ID);

        var result = await _authenticationService.SignInWithGoogleAsync(request);

        Assert.Equal(SignInResultCode.GoogleAuthNotLinkedError, result.ResultCode);
        Assert.Null(result.AuthToken);
    }

    #endregion

    #region AuthenticateWithTokenAsync Tests

    [Fact]
    public async Task AuthenticateWithTokenAsync_ReturnsSuccess_WhenTokenIsValid()
    {
        var result = await _authenticationService.AuthenticateWithTokenAsync("valid-token");

        Assert.Equal(UserAuthTokenValidationResultCode.Success, result);
        _userContextSetterMock.Verify(m => m.SetUserContext(It.IsAny<UserContext>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateWithTokenAsync_ReturnsResultCode_WhenTokenValidationFails()
    {
        _authenticationProviderMock
            .Setup(m => m.ValidateUserAuthTokenAsync("bad-token"))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.TokenValidationFailed,
                    null,
                    null,
                    null,
                    null,
                    []
                )
            );

        var result = await _authenticationService.AuthenticateWithTokenAsync("bad-token");

        Assert.Equal(UserAuthTokenValidationResultCode.TokenValidationFailed, result);
        _userContextSetterMock.Verify(m => m.SetUserContext(It.IsAny<UserContext>()), Times.Never);
    }

    [Theory]
    [InlineData(UserAuthTokenValidationResultCode.InvalidTokenFormat)]
    [InlineData(UserAuthTokenValidationResultCode.MissingUserIdClaim)]
    [InlineData(UserAuthTokenValidationResultCode.TokenValidationFailed)]
    public async Task AuthenticateWithTokenAsync_ReturnsCorrectResultCode_ForEachFailureType(
        UserAuthTokenValidationResultCode expectedResultCode
    )
    {
        _authenticationProviderMock
            .Setup(m => m.ValidateUserAuthTokenAsync("bad-token"))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(expectedResultCode, null, null, null, null, [])
            );

        var result = await _authenticationService.AuthenticateWithTokenAsync("bad-token");

        Assert.Equal(expectedResultCode, result);
        _userContextSetterMock.Verify(m => m.SetUserContext(It.IsAny<UserContext>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateWithTokenAsync_ReturnsMissingDeviceIdClaim_WhenDeviceIdIsNull()
    {
        _authenticationProviderMock
            .Setup(m => m.ValidateUserAuthTokenAsync("token"))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    new User() { Id = Guid.CreateVersion7() },
                    null,
                    null,
                    Guid.CreateVersion7(),
                    []
                )
            );

        var result = await _authenticationService.AuthenticateWithTokenAsync("token");

        Assert.Equal(UserAuthTokenValidationResultCode.MissingDeviceIdClaim, result);
        _userContextSetterMock.Verify(m => m.SetUserContext(It.IsAny<UserContext>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateWithTokenAsync_SetsContextWithNullAccount_WhenNoSignedInAccount()
    {
        _authenticationProviderMock
            .Setup(m => m.ValidateUserAuthTokenAsync("token"))
            .ReturnsAsync(
                new UserAuthTokenValidationResult(
                    UserAuthTokenValidationResultCode.Success,
                    new User() { Id = Guid.CreateVersion7() },
                    null,
                    TEST_DEVICE_ID,
                    Guid.CreateVersion7(),
                    []
                )
            );

        var result = await _authenticationService.AuthenticateWithTokenAsync("token");

        Assert.Equal(UserAuthTokenValidationResultCode.Success, result);
        _userContextSetterMock.Verify(
            m =>
                m.SetUserContext(
                    It.Is<UserContext>(c =>
                        c.CurrentAccount == null && c.DeviceId == TEST_DEVICE_ID
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task AuthenticateWithTokenAsync_ThrowsArgumentNullException_WhenTokenIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _authenticationService.AuthenticateWithTokenAsync(null!)
        );
    }

    #endregion

    #region AuthenticateAsSystem Tests

    [Fact]
    public void AuthenticateAsSystem_CallsSetSystemContext()
    {
        _authenticationService.AuthenticateAsSystem(TEST_DEVICE_ID);

        _userContextSetterMock.Verify(m => m.SetSystemContext(TEST_DEVICE_ID), Times.Once);
    }

    #endregion
}
