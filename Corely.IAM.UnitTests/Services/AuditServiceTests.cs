using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Services;

public class AuditServiceTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IAuditAccessProvider> _accessProvider = new();
    private readonly AuditSettingsCache _cache;
    private readonly AuditPolicy _policy;
    private readonly AuditService _service;

    private readonly Guid _viewerId = Guid.CreateVersion7();
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _otherAccountId = Guid.CreateVersion7();
    private readonly DateTime _now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public AuditServiceTests()
    {
        _cache = new AuditSettingsCache(TimeProvider.System, Options.Create(new AuditOptions()));
        _policy = new AuditPolicy(
            _serviceFactory.GetRequiredService<IReadonlyRepo<PlatformSettingsEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountAuditSettingsEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _cache
        );
        _service = new AuditService(
            _serviceFactory.GetRequiredService<IRepo<AuditEntryEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<AccountAuditSettingsEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<PlatformSettingsEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _accessProvider.Object,
            _policy,
            _cache,
            TimeProvider.System,
            NullLogger<AuditService>.Instance
        );
        SetReadAccess(new AuditAccess(false, new HashSet<Guid> { _accountId }, _viewerId));
    }

    [Fact]
    public async Task ListEntries_ReturnsTheReachableAccountsAndTheViewersOwnEntries()
    {
        var inAccount = await AddEntryAsync(accountId: _accountId);
        var own = await AddEntryAsync(accountId: null, actorUserId: _viewerId);
        await AddEntryAsync(accountId: _otherAccountId);
        await AddEntryAsync(accountId: null);

        var result = await _service.ListEntriesAsync(new ListAuditEntriesRequest());

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Equal(2, result.Data!.TotalCount);
        Assert.Equal(
            new[] { inAccount, own }.OrderBy(id => id),
            result.Data.Items.Select(e => e.Id).OrderBy(id => id)
        );
    }

    [Fact]
    public async Task ListEntries_ReturnsEverything_ForAccessToEverything()
    {
        SetReadAccess(AuditAccess.All);
        await AddEntryAsync(accountId: _accountId);
        await AddEntryAsync(accountId: _otherAccountId);
        await AddEntryAsync(accountId: null, cohort: AuditCohort.SystemContext);

        var result = await _service.ListEntriesAsync(new ListAuditEntriesRequest());

        Assert.Equal(3, result.Data!.TotalCount);
    }

    [Fact]
    public async Task ListEntries_ReturnsTheNewestFirst_OnePageAtATime()
    {
        var oldest = await AddEntryAsync(accountId: _accountId, occurredUtc: _now.AddHours(-2));
        var newest = await AddEntryAsync(accountId: _accountId, occurredUtc: _now);
        var middle = await AddEntryAsync(accountId: _accountId, occurredUtc: _now.AddHours(-1));

        var first = await _service.ListEntriesAsync(new ListAuditEntriesRequest(Take: 2));
        var second = await _service.ListEntriesAsync(new ListAuditEntriesRequest(Skip: 2, Take: 2));

        Assert.Equal([newest, middle], first.Data!.Items.Select(e => e.Id));
        Assert.True(first.Data.HasMore);
        Assert.Equal([oldest], second.Data!.Items.Select(e => e.Id));
        Assert.Equal(3, second.Data.TotalCount);
    }

    [Fact]
    public async Task ListEntries_AppliesTheFilters()
    {
        var match = await AddEntryAsync(
            accountId: _accountId,
            action: AuthAction.Delete,
            resultCode: "Success",
            occurredUtc: _now
        );
        await AddEntryAsync(accountId: _accountId, action: AuthAction.Create, occurredUtc: _now);
        await AddEntryAsync(
            accountId: _accountId,
            action: AuthAction.Delete,
            resultCode: "UnauthorizedError",
            occurredUtc: _now
        );
        await AddEntryAsync(
            accountId: _accountId,
            action: AuthAction.Delete,
            occurredUtc: _now.AddDays(-10)
        );

        var result = await _service.ListEntriesAsync(
            new ListAuditEntriesRequest(
                new AuditEntryFilter
                {
                    AccountId = _accountId,
                    Action = AuthAction.Delete,
                    ResultCode = "Success",
                    FromUtc = _now.AddDays(-1),
                }
            )
        );

        Assert.Equal([match], result.Data!.Items.Select(e => e.Id));
    }

    [Fact]
    public async Task ListEntries_LeavesOutPlatformMembers_WhenAsked()
    {
        var member = await AddEntryAsync(accountId: _accountId, cohort: AuditCohort.AccountMember);
        await AddEntryAsync(accountId: _accountId, cohort: AuditCohort.PlatformMember);

        var result = await _service.ListEntriesAsync(
            new ListAuditEntriesRequest(new AuditEntryFilter { IncludePlatformMembers = false })
        );

        Assert.Equal([member], result.Data!.Items.Select(e => e.Id));
    }

    [Fact]
    public async Task ListEntries_NamesLivingAndDeletedUsersAndAccounts()
    {
        var livingUser = Guid.CreateVersion7();
        var deletedUser = Guid.CreateVersion7();
        var deletedAccount = Guid.CreateVersion7();
        SetReadAccess(AuditAccess.All);
        await _serviceFactory
            .GetRequiredService<IRepo<UserEntity>>()
            .CreateAsync(new UserEntity { Id = livingUser, Username = "alive" });
        await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(new AccountEntity { Id = _accountId, AccountName = "Acme" });
        await AddEntryAsync(accountId: _accountId, actorUserId: livingUser);
        await AddEntryAsync(accountId: deletedAccount, actorUserId: deletedUser);
        await AddEntryAsync(
            accountId: null,
            actorUserId: deletedUser,
            service: nameof(IDeregistrationService),
            operation: nameof(IDeregistrationService.DeregisterUserAsync),
            details: "gone-user"
        );
        await AddEntryAsync(
            accountId: deletedAccount,
            service: nameof(IDeregistrationService),
            operation: nameof(IDeregistrationService.DeregisterAccountAsync),
            details: "Gone Inc"
        );

        var items = (await _service.ListEntriesAsync(new ListAuditEntriesRequest())).Data!.Items;

        Assert.Contains(items, e => e.ActorUserId == livingUser && e.ActorName == "alive");
        Assert.Contains(items, e => e.AccountId == _accountId && e.AccountName == "Acme");
        Assert.All(
            items.Where(e => e.ActorUserId == deletedUser),
            e => Assert.Equal("gone-user", e.ActorName)
        );
        Assert.All(
            items.Where(e => e.AccountId == deletedAccount),
            e => Assert.Equal("Gone Inc", e.AccountName)
        );
    }

    [Fact]
    public async Task GetEntry_ReturnsNotFound_ForAnEntryTheViewerCannotSee()
    {
        var hidden = await AddEntryAsync(accountId: _otherAccountId);

        var result = await _service.GetEntryAsync(hidden);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task GetEntry_ReturnsAVisibleEntry()
    {
        var visible = await AddEntryAsync(accountId: _accountId);

        var result = await _service.GetEntryAsync(visible);

        Assert.Equal(visible, result.Item!.Id);
    }

    [Fact]
    public async Task ExportEntries_WritesAHeaderAndOneRowPerEntry()
    {
        await AddEntryAsync(accountId: _accountId, details: "has, comma");
        await AddEntryAsync(accountId: _accountId);

        var result = await _service.ExportEntriesAsync(new AuditEntryFilter());

        var lines = result.Csv!.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(AuditEntry.CSV_HEADER, lines[0]);
        Assert.Equal(3, lines.Length);
        Assert.Contains("\"has, comma\"", result.Csv);
        Assert.Equal(2, result.ExportedCount);
        Assert.False(result.Truncated);
    }

    [Fact]
    public async Task ExportEntries_StopsAtTheCap_AndSaysSo()
    {
        await _serviceFactory
            .GetRequiredService<IRepo<AuditEntryEntity>>()
            .CreateAsync(
                Enumerable
                    .Range(0, AuditConstants.EXPORT_MAX_ENTRIES + 1)
                    .Select(i => NewEntry(_accountId, _now.AddSeconds(-i)))
                    .ToList()
            );

        var result = await _service.ExportEntriesAsync(new AuditEntryFilter());

        Assert.True(result.Truncated);
        Assert.Equal(AuditConstants.EXPORT_MAX_ENTRIES, result.ExportedCount);
        Assert.Contains("10,000", result.Message);
    }

    [Fact]
    public async Task ListAuditAccounts_ReturnsOnlyTheReachableAccounts()
    {
        await CreateAccountAsync(_accountId, "Acme");
        await CreateAccountAsync(_otherAccountId, "Other");

        var result = await _service.ListAuditAccountsAsync(AuthAction.Read);

        Assert.Equal([new AuditAccountOption(_accountId, "Acme")], result.Accounts);
        Assert.False(result.IncludesEverything);
    }

    [Fact]
    public async Task ListAuditAccounts_ReturnsEveryAccount_ForAccessToEverything()
    {
        _accessProvider
            .Setup(x => x.GetAccessAsync(AuthAction.Delete))
            .ReturnsAsync(AuditAccess.All);
        await CreateAccountAsync(_accountId, "Beta");
        await CreateAccountAsync(_otherAccountId, "Alpha");

        var result = await _service.ListAuditAccountsAsync(AuthAction.Delete);

        Assert.Equal(["Alpha", "Beta"], result.Accounts.Select(a => a.Name));
        Assert.True(result.IncludesEverything);
    }

    [Fact]
    public async Task CountPurgeableEntries_CountsOneAccountsEntries_OlderThanTheDate()
    {
        await AddEntryAsync(accountId: _accountId, occurredUtc: _now.AddDays(-10));
        await AddEntryAsync(accountId: _accountId, occurredUtc: _now);
        await AddEntryAsync(accountId: _otherAccountId, occurredUtc: _now.AddDays(-10));

        var older = await _service.CountPurgeableEntriesAsync(
            new PurgeAuditEntriesRequest(_accountId, _now.AddDays(-1))
        );
        var all = await _service.CountPurgeableEntriesAsync(
            new PurgeAuditEntriesRequest(_accountId)
        );

        Assert.Equal(1, older.Count);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetAccountSettings_ReturnsDefaultsAndThePlatformLimits()
    {
        await CreateAccountAsync(_accountId, "Acme");

        var result = await _service.GetAccountSettingsAsync(_accountId);

        Assert.Equal(AccountAuditSettings.Default(_accountId), result.Settings);
        Assert.Equal(AuditActions.All, result.AllowedActions);
        Assert.Equal(AuditConstants.DEFAULT_MAX_RETENTION_DAYS, result.MaxRetentionDays);
    }

    [Fact]
    public async Task GetAccountSettings_ReturnsNotFound_ForAMissingAccount()
    {
        var result = await _service.GetAccountSettingsAsync(Guid.CreateVersion7());

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateAccountSettings_StoresTheSettings_AndTheyApplyAtOnce()
    {
        await CreateAccountAsync(_accountId, "Acme");
        await _policy.GetAccountSettingsAsync(_accountId);

        var result = await _service.UpdateAccountSettingsAsync(
            new UpdateAccountAuditSettingsRequest(
                _accountId,
                AuditActions.Read,
                AuditActions.None,
                5
            )
        );
        var second = await _service.UpdateAccountSettingsAsync(
            new UpdateAccountAuditSettingsRequest(
                _accountId,
                AuditActions.All,
                AuditActions.Delete,
                6
            )
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        Assert.Equal(ModifyResultCode.Success, second.ResultCode);
        Assert.Equal(
            new AccountAuditSettings(_accountId, AuditActions.All, AuditActions.Delete, 6),
            await _policy.GetAccountSettingsAsync(_accountId)
        );
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(AuditConstants.DEFAULT_MAX_RETENTION_DAYS + 1)]
    public async Task UpdateAccountSettings_RefusesARetentionOutsideZeroToThePlatformMaximum(
        int retentionDays
    )
    {
        await CreateAccountAsync(_accountId, "Acme");

        var result = await _service.UpdateAccountSettingsAsync(
            new UpdateAccountAuditSettingsRequest(
                _accountId,
                AuditActions.None,
                AuditActions.None,
                retentionDays
            )
        );

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateAccountSettings_RefusesUnknownActions()
    {
        await CreateAccountAsync(_accountId, "Acme");

        var result = await _service.UpdateAccountSettingsAsync(
            new UpdateAccountAuditSettingsRequest(
                _accountId,
                (AuditActions)64,
                AuditActions.None,
                5
            )
        );

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateAccountSettings_ReturnsNotFound_ForAMissingAccount()
    {
        var result = await _service.UpdateAccountSettingsAsync(
            new UpdateAccountAuditSettingsRequest(
                _accountId,
                AuditActions.None,
                AuditActions.None,
                5
            )
        );

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdatePlatformSettings_StoresTheSettings_AndTheyApplyAtOnce()
    {
        await _policy.GetPlatformSettingsAsync();
        var settings = PlatformSettings.Default with
        {
            AuditEnabled = false,
            AuditMaxRetentionDays = 30,
        };

        var first = await _service.UpdatePlatformSettingsAsync(settings);
        var second = await _service.UpdatePlatformSettingsAsync(
            settings with
            {
                AuditMaxRetentionDays = 40,
            }
        );

        Assert.Equal(ModifyResultCode.Success, first.ResultCode);
        Assert.Equal(ModifyResultCode.Success, second.ResultCode);
        Assert.Equal(
            settings with
            {
                AuditMaxRetentionDays = 40,
            },
            (await _service.GetPlatformSettingsAsync()).Settings
        );
    }

    [Fact]
    public async Task UpdatePlatformSettings_RefusesANegativeMaximum()
    {
        var result = await _service.UpdatePlatformSettingsAsync(
            PlatformSettings.Default with
            {
                AuditMaxRetentionDays = -1,
            }
        );

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }

    private void SetReadAccess(AuditAccess access) =>
        _accessProvider.Setup(x => x.GetAccessAsync(AuthAction.Read)).ReturnsAsync(access);

    private Task CreateAccountAsync(Guid id, string name) =>
        _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(new AccountEntity { Id = id, AccountName = name });

    private async Task<Guid> AddEntryAsync(
        Guid? accountId,
        Guid? actorUserId = null,
        AuditCohort cohort = AuditCohort.AccountMember,
        AuthAction action = AuthAction.Update,
        string resultCode = "Success",
        DateTime? occurredUtc = null,
        string service = nameof(IRegistrationService),
        string operation = "RegisterGroupAsync",
        string? details = null
    )
    {
        var entry = NewEntry(accountId, occurredUtc ?? _now);
        entry.ActorUserId = actorUserId;
        entry.Cohort = cohort;
        entry.Action = action;
        entry.ResultCode = resultCode;
        entry.Service = service;
        entry.Operation = operation;
        entry.Details = details;
        await _serviceFactory.GetRequiredService<IRepo<AuditEntryEntity>>().CreateAsync(entry);
        return entry.Id;
    }

    private static AuditEntryEntity NewEntry(Guid? accountId, DateTime occurredUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            OccurredUtc = occurredUtc,
            AccountId = accountId,
            Cohort = AuditCohort.AccountMember,
            Source = "tests",
            Service = nameof(IRegistrationService),
            Operation = "RegisterGroupAsync",
            Action = AuthAction.Update,
            ResourceType = "group",
            ResultCode = "Success",
        };
}
