using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Entities;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Security.Providers;

public class ReauthenticationProviderTests
{
    private const int MAX_LOGIN_ATTEMPTS = 3;
    private const int LOCKOUT_COOLDOWN_SECONDS = 900;
    private const string CORRECT_PASSWORD = "correct";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly IRepo<UserEntity> _userRepo;
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock = new();
    private readonly Mock<ITotpAuthProcessor> _totpAuthProcessorMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly SecurityOptions _securityOptions = new()
    {
        MaxLoginAttempts = MAX_LOGIN_ATTEMPTS,
        LockoutCooldownSeconds = LOCKOUT_COOLDOWN_SECONDS,
    };
    private readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly ReauthenticationProvider _provider;
    private bool _hasBasicAuth = true;

    public ReauthenticationProviderTests()
    {
        _userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(_now));
        _basicAuthProcessorMock
            .Setup(m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(
                (VerifyBasicAuthRequest r) =>
                    _hasBasicAuth
                        ? new VerifyBasicAuthResult(
                            VerifyBasicAuthResultCode.Success,
                            string.Empty,
                            r.Password == CORRECT_PASSWORD
                        )
                        : new VerifyBasicAuthResult(
                            VerifyBasicAuthResultCode.UserNotFoundError,
                            string.Empty,
                            false
                        )
            );

        _provider = new ReauthenticationProvider(
            _userRepo,
            _basicAuthProcessorMock.Object,
            _totpAuthProcessorMock.Object,
            Options.Create(_securityOptions),
            _timeProviderMock.Object
        );
    }

    private async Task<UserEntity> CreateUserAsync(
        int failedLogins = 0,
        DateTime? lockedUtc = null
    ) =>
        await _userRepo.CreateAsync(
            new UserEntity
            {
                Id = Guid.CreateVersion7(),
                Username = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@test.com",
                FailedLoginsSinceLastSuccess = failedLogins,
                LockedUtc = lockedUtc,
                Accounts = [],
                Groups = [],
                Roles = [],
            }
        );

    private async Task<UserEntity> ReloadAsync(Guid userId) =>
        (await _userRepo.GetAsync(u => u.Id == userId))!;

    [Fact]
    public async Task VerifyCurrentPassword_Succeeds_WhenPasswordIsCorrect()
    {
        var user = await CreateUserAsync();

        var result = await _provider.VerifyCurrentPasswordAsync(user.Id, CORRECT_PASSWORD);

        Assert.Equal(ReauthenticationResultCode.Success, result.ResultCode);
        Assert.Equal(0, (await ReloadAsync(user.Id)).FailedLoginsSinceLastSuccess);
    }

    [Fact]
    public async Task VerifyCurrentPassword_CountsAFailedLogin_WhenPasswordIsWrong()
    {
        var user = await CreateUserAsync();

        var result = await _provider.VerifyCurrentPasswordAsync(user.Id, "wrong");

        Assert.Equal(ReauthenticationResultCode.InvalidCurrentPasswordError, result.ResultCode);
        var reloaded = await ReloadAsync(user.Id);
        Assert.Equal(1, reloaded.FailedLoginsSinceLastSuccess);
        Assert.Equal(1, reloaded.TotalFailedLogins);
        Assert.Equal(_now, reloaded.LastFailedLoginUtc);
        Assert.Null(reloaded.LockedUtc);
    }

    [Fact]
    public async Task VerifyCurrentPassword_LocksTheUser_WhenFailuresReachTheLimit()
    {
        var user = await CreateUserAsync(failedLogins: MAX_LOGIN_ATTEMPTS - 1);

        await _provider.VerifyCurrentPasswordAsync(user.Id, "wrong");

        Assert.Equal(_now, (await ReloadAsync(user.Id)).LockedUtc);
    }

    [Fact]
    public async Task VerifyCurrentPassword_RefusesWithoutChecking_WhileLockedOut()
    {
        var user = await CreateUserAsync(MAX_LOGIN_ATTEMPTS, _now.AddSeconds(-1));

        var result = await _provider.VerifyCurrentPasswordAsync(user.Id, CORRECT_PASSWORD);

        Assert.Equal(ReauthenticationResultCode.UserLockedError, result.ResultCode);
        _basicAuthProcessorMock.Verify(
            m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task VerifyCurrentPassword_StartsCountingAgain_AfterTheLockoutExpires()
    {
        var user = await CreateUserAsync(
            MAX_LOGIN_ATTEMPTS,
            _now.AddSeconds(-LOCKOUT_COOLDOWN_SECONDS - 1)
        );

        var result = await _provider.VerifyCurrentPasswordAsync(user.Id, "wrong");

        Assert.Equal(ReauthenticationResultCode.InvalidCurrentPasswordError, result.ResultCode);
        var reloaded = await ReloadAsync(user.Id);
        Assert.Equal(1, reloaded.FailedLoginsSinceLastSuccess);
        Assert.Null(reloaded.LockedUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyCurrentPassword_RefusesWithoutCounting_WhenPasswordMissing(
        string? currentPassword
    )
    {
        var user = await CreateUserAsync();

        var result = await _provider.VerifyCurrentPasswordAsync(user.Id, currentPassword);

        Assert.Equal(ReauthenticationResultCode.InvalidCurrentPasswordError, result.ResultCode);
        Assert.Equal(0, (await ReloadAsync(user.Id)).FailedLoginsSinceLastSuccess);
    }

    [Fact]
    public async Task VerifyCurrentPassword_ReportsNoPassword_WithoutCounting()
    {
        _hasBasicAuth = false;
        var user = await CreateUserAsync();

        var result = await _provider.VerifyCurrentPasswordAsync(user.Id, "anything");

        Assert.Equal(ReauthenticationResultCode.BasicAuthNotFoundError, result.ResultCode);
        Assert.Equal(0, (await ReloadAsync(user.Id)).FailedLoginsSinceLastSuccess);
    }

    [Fact]
    public async Task VerifyTotpCode_Succeeds_WhenTotpIsNotEnabled()
    {
        _totpAuthProcessorMock
            .Setup(m => m.IsTotpEnabledAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var result = await _provider.VerifyTotpCodeAsync(Guid.CreateVersion7(), null);

        Assert.Equal(ReauthenticationResultCode.Success, result.ResultCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task VerifyTotpCode_RequiresACode_WhenTotpIsEnabled(string? code)
    {
        _totpAuthProcessorMock
            .Setup(m => m.IsTotpEnabledAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _provider.VerifyTotpCodeAsync(Guid.CreateVersion7(), code);

        Assert.Equal(ReauthenticationResultCode.TotpRequiredError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyTotpCode_Refuses_WhenTheCodeIsWrong()
    {
        SetupEnabledTotp(VerifyTotpOrRecoveryCodeResultCode.InvalidCodeError);

        var result = await _provider.VerifyTotpCodeAsync(Guid.CreateVersion7(), "000000");

        Assert.Equal(ReauthenticationResultCode.InvalidTotpCodeError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyTotpCode_Succeeds_WithAValidTotpCode()
    {
        SetupEnabledTotp(VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid);

        var result = await _provider.VerifyTotpCodeAsync(Guid.CreateVersion7(), "123456");

        Assert.Equal(ReauthenticationResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task VerifyTotpCode_Succeeds_WithARecoveryCode()
    {
        SetupEnabledTotp(VerifyTotpOrRecoveryCodeResultCode.RecoveryCodeValid);

        var result = await _provider.VerifyTotpCodeAsync(Guid.CreateVersion7(), "ABCD-EFGH");

        Assert.Equal(ReauthenticationResultCode.Success, result.ResultCode);
    }

    private void SetupEnabledTotp(VerifyTotpOrRecoveryCodeResultCode verifyResultCode)
    {
        _totpAuthProcessorMock
            .Setup(m => m.IsTotpEnabledAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);
        _totpAuthProcessorMock
            .Setup(m =>
                m.VerifyTotpOrRecoveryCodeAsync(It.IsAny<VerifyTotpOrRecoveryCodeRequest>())
            )
            .ReturnsAsync(new VerifyTotpOrRecoveryCodeResult(verifyResultCode, string.Empty));
    }
}
