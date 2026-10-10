using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Platform.Constants;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Audits.Providers;

public class AuditPolicyTests
{
    private const int CACHE_TTL_SECONDS = 30;

    private readonly ServiceFactory _serviceFactory = new();
    private readonly ControllableTimeProvider _timeProvider = new();
    private readonly AuditPolicy _policy;

    public AuditPolicyTests()
    {
        _policy = new AuditPolicy(
            _serviceFactory.GetRequiredService<IReadonlyRepo<PlatformSettingsEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountAuditSettingsEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            new AuditSettingsCache(
                _timeProvider,
                Options.Create(new AuditOptions { SettingsCacheTtlSeconds = CACHE_TTL_SECONDS })
            )
        );
    }

    [Fact]
    public async Task GetPlatformSettings_ReturnsDefaults_WhenNoRowExists()
    {
        Assert.Equal(PlatformSettings.Default, await _policy.GetPlatformSettingsAsync());
    }

    [Fact]
    public async Task GetPlatformSettings_ReturnsTheStoredRow()
    {
        await CreatePlatformSettingsAsync(auditEnabled: false, maxRetentionDays: 10);

        var settings = await _policy.GetPlatformSettingsAsync();

        Assert.False(settings.AuditEnabled);
        Assert.Equal(10, settings.AuditMaxRetentionDays);
    }

    [Fact]
    public async Task GetPlatformSettings_KeepsTheCachedValue_UntilItExpires()
    {
        await _policy.GetPlatformSettingsAsync();
        await CreatePlatformSettingsAsync(auditEnabled: false, maxRetentionDays: 10);

        Assert.True((await _policy.GetPlatformSettingsAsync()).AuditEnabled);

        _timeProvider.Advance(TimeSpan.FromSeconds(CACHE_TTL_SECONDS));

        Assert.False((await _policy.GetPlatformSettingsAsync()).AuditEnabled);
    }

    [Fact]
    public async Task GetAccountSettings_ReturnsDefaults_ForAnAccountWithoutARow()
    {
        var accountId = await CreateAccountAsync();

        Assert.Equal(
            AccountAuditSettings.Default(accountId),
            await _policy.GetAccountSettingsAsync(accountId)
        );
    }

    [Fact]
    public async Task GetAccountSettings_ReturnsTheStoredRow()
    {
        var accountId = await CreateAccountAsync();
        await CreateAccountSettingsAsync(accountId, AuditActions.Read, AuditActions.None, 7);

        var settings = await _policy.GetAccountSettingsAsync(accountId);

        Assert.Equal(
            new AccountAuditSettings(accountId, AuditActions.Read, AuditActions.None, 7),
            settings
        );
    }

    [Fact]
    public async Task GetAccountSettings_ReturnsNull_ForADeletedAccount()
    {
        Assert.Null(await _policy.GetAccountSettingsAsync(Guid.CreateVersion7()));
    }

    [Theory]
    [InlineData(AuthAction.Create, true)]
    [InlineData(AuthAction.Update, true)]
    [InlineData(AuthAction.Delete, true)]
    [InlineData(AuthAction.Read, false)]
    [InlineData(AuthAction.Execute, false)]
    public async Task IsRecorded_FollowsTheDefaults_ForAccountMembers(
        AuthAction action,
        bool expected
    )
    {
        var accountId = await CreateAccountAsync();

        Assert.Equal(
            expected,
            await _policy.IsRecordedAsync(AuditCohort.AccountMember, accountId, action)
        );
    }

    [Fact]
    public async Task IsRecorded_ReturnsFalse_WhenTheAccountSwitchesPlatformMembersOff()
    {
        var accountId = await CreateAccountAsync();
        await CreateAccountSettingsAsync(accountId, AuditActions.All, AuditActions.None, 30);

        Assert.False(
            await _policy.IsRecordedAsync(AuditCohort.PlatformMember, accountId, AuthAction.Delete)
        );
        Assert.True(
            await _policy.IsRecordedAsync(AuditCohort.AccountMember, accountId, AuthAction.Delete)
        );
    }

    [Fact]
    public async Task IsRecorded_RecordsAccountlessSignIns_AndNothingForSystemContext_ByDefault()
    {
        Assert.True(
            await _policy.IsRecordedAsync(AuditCohort.Accountless, null, AuthAction.Execute)
        );
        Assert.False(
            await _policy.IsRecordedAsync(AuditCohort.SystemContext, null, AuthAction.Delete)
        );
    }

    [Theory]
    [InlineData(nameof(IDeregistrationService.DeregisterUserAsync), true)]
    [InlineData(nameof(IDeregistrationService.DeregisterAccountAsync), true)]
    [InlineData(nameof(IDeregistrationService.DeregisterGroupAsync), false)]
    public void IsAlwaysRecorded_CoversOnlyUserAndAccountDeletion(string operation, bool expected)
    {
        Assert.Equal(expected, _policy.IsAlwaysRecorded(nameof(IDeregistrationService), operation));
    }

    [Fact]
    public void IsAlwaysRecorded_ReturnsFalse_ForTheSameOperationNameOnAnotherService()
    {
        Assert.False(
            _policy.IsAlwaysRecorded(
                nameof(IRegistrationService),
                nameof(IDeregistrationService.DeregisterUserAsync)
            )
        );
    }

    [Fact]
    public async Task RetentionDays_UsesThePlatformMaximum_OutsideAnyAccount()
    {
        await CreatePlatformSettingsAsync(auditEnabled: true, maxRetentionDays: 200);

        Assert.Equal(200, await _policy.RetentionDaysAsync(null));
    }

    [Fact]
    public async Task RetentionDays_UsesThePlatformMaximum_ForADeletedAccount()
    {
        await CreatePlatformSettingsAsync(auditEnabled: true, maxRetentionDays: 200);

        Assert.Equal(200, await _policy.RetentionDaysAsync(Guid.CreateVersion7()));
    }

    [Fact]
    public async Task RetentionDays_UsesTheAccountsRetention_WithinThePlatformMaximum()
    {
        await CreatePlatformSettingsAsync(auditEnabled: true, maxRetentionDays: 200);
        var shortAccount = await CreateAccountAsync();
        await CreateAccountSettingsAsync(shortAccount, AuditActions.None, AuditActions.None, 10);
        var longAccount = await CreateAccountAsync();
        await CreateAccountSettingsAsync(longAccount, AuditActions.None, AuditActions.None, 900);

        Assert.Equal(10, await _policy.RetentionDaysAsync(shortAccount));
        Assert.Equal(200, await _policy.RetentionDaysAsync(longAccount));
    }

    private async Task<Guid> CreateAccountAsync()
    {
        var id = Guid.CreateVersion7();
        await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(new AccountEntity { Id = id, AccountName = $"account-{id}" });
        return id;
    }

    private Task CreateAccountSettingsAsync(
        Guid accountId,
        AuditActions accountMemberActions,
        AuditActions platformMemberActions,
        int retentionDays
    ) =>
        _serviceFactory
            .GetRequiredService<IRepo<AccountAuditSettingsEntity>>()
            .CreateAsync(
                new AccountAuditSettingsEntity
                {
                    AccountId = accountId,
                    AccountMemberActions = accountMemberActions,
                    PlatformMemberActions = platformMemberActions,
                    RetentionDays = retentionDays,
                }
            );

    private Task CreatePlatformSettingsAsync(bool auditEnabled, int maxRetentionDays) =>
        _serviceFactory
            .GetRequiredService<IRepo<PlatformSettingsEntity>>()
            .CreateAsync(
                new PlatformSettingsEntity
                {
                    Id = PlatformConstants.PLATFORM_SETTINGS_ID,
                    AuditEnabled = auditEnabled,
                    AuditMaxRetentionDays = maxRetentionDays,
                    AuditAllowedActions = AuditActions.All,
                    SystemContextActions = AuditActions.None,
                    AccountlessActions = AuditActions.Execute,
                }
            );

    private sealed class ControllableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);
    }
}
