using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Auditing;

public class AuditingTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task CreatingAGroup_WritesAnAccountMemberEntry()
    {
        var groupId = await CreateGroupAsOwnerAsync("Audited");

        var entry = await SingleEntryAsync(
            nameof(IRegistrationService.RegisterGroupAsync),
            groupId
        );

        Assert.Equal(_scenario.OwnerUserId, entry.ActorUserId);
        Assert.Equal(_scenario.AccountId, entry.AccountId);
        Assert.Equal(AuditCohort.AccountMember, entry.Cohort);
        Assert.Equal(AuthAction.Create, entry.Action);
        Assert.Equal(nameof(IRegistrationService), entry.Service);
        Assert.Equal("Success", entry.ResultCode);
    }

    [Fact]
    public async Task ARefusedCall_IsRecordedWithItsRefusalCode()
    {
        var groupId = await CreateGroupAsOwnerAsync("Protected");

        var result = await _scenario.ActAsAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterGroupAsync(new DeregisterGroupRequest(groupId, _scenario.AccountId))
        );

        Assert.NotEqual(DeregisterGroupResultCode.Success, result.ResultCode);
        var entry = await SingleEntryAsync(
            nameof(IDeregistrationService.DeregisterGroupAsync),
            groupId
        );
        Assert.Equal(result.ResultCode.ToString(), entry.ResultCode);
        Assert.Equal(_scenario.DirectMemberUserId, entry.ActorUserId);
    }

    [Fact]
    public async Task Reads_AreNotRecorded_ByDefault()
    {
        await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .ListGroupsAsync(new Groups.Models.ListGroupsRequest(_scenario.AccountId))
        );

        Assert.Equal(0, await CountAsync(nameof(IRetrievalService.ListGroupsAsync)));
    }

    [Fact]
    public async Task AFailedSignIn_ForAnExistingUser_NamesTheActor_OutsideAnyAccount()
    {
        var result = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest(_scenario.DirectMemberUsername, "wrong", "device"))
        );

        Assert.Equal(SignInResultCode.PasswordMismatchError, result.ResultCode);
        var entry = await _scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>()
                .SingleAsync(e =>
                    e.Operation == nameof(IAuthenticationService.SignInAsync)
                    && e.ResultCode == nameof(SignInResultCode.PasswordMismatchError)
                )
        );
        Assert.Equal(_scenario.DirectMemberUserId, entry.ActorUserId);
        Assert.Null(entry.AccountId);
        Assert.Equal(AuditCohort.Accountless, entry.Cohort);
        Assert.Equal(AuthAction.Execute, entry.Action);
    }

    [Fact]
    public async Task SwitchingAuditingOff_StopsRecording_ButNotUserDeletion()
    {
        await UpdatePlatformSettingsAsync(PlatformSettings.Default with { AuditEnabled = false });

        var groupId = await CreateGroupAsOwnerAsync("Unrecorded");
        var deleted = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            null,
            services => services.GetRequiredService<IDeregistrationService>().DeregisterUserAsync()
        );

        Assert.Equal(DeregisterUserResultCode.Success, deleted.ResultCode);
        Assert.Equal(0, await CountAsync(nameof(IRegistrationService.RegisterGroupAsync), groupId));
        var entry = await SingleEntryAsync(
            nameof(IDeregistrationService.DeregisterUserAsync),
            _scenario.OutsiderUserId
        );
        Assert.Equal(_scenario.OutsiderUsername, entry.Details);
        Assert.Equal(_scenario.OutsiderUserId, entry.ActorUserId);
    }

    [Fact]
    public async Task ADeletedUser_IsStillNamed_FromTheirDeletionEntry()
    {
        await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            null,
            services => services.GetRequiredService<IDeregistrationService>().DeregisterUserAsync()
        );

        var entries = await _scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IAuditService>()
                .ListEntriesAsync(
                    new ListAuditEntriesRequest(
                        new AuditEntryFilter { ActorUserId = _scenario.OutsiderUserId },
                        Take: 100
                    )
                )
        );

        Assert.NotEmpty(entries.Data!.Items);
        Assert.All(entries.Data.Items, e => Assert.Equal(_scenario.OutsiderUsername, e.ActorName));
    }

    [Fact]
    public async Task AnAccountThatSwitchesItsMembersOff_GetsNothingRecordedForThem()
    {
        var updated = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IAuditService>()
                    .UpdateAccountSettingsAsync(
                        new UpdateAccountAuditSettingsRequest(
                            _scenario.AccountId,
                            AuditActions.None,
                            AuditActions.All,
                            30
                        )
                    )
        );
        Assert.Equal(ModifyResultCode.Success, updated.ResultCode);

        var groupId = await CreateGroupAsOwnerAsync("Quiet");

        Assert.Equal(0, await CountAsync(nameof(IRegistrationService.RegisterGroupAsync), groupId));
    }

    [Fact]
    public async Task AnOwner_SeesTheirAccountsEntries_AndAMemberWithoutAuditRead_SeesOnlyTheirOwn()
    {
        var groupId = await CreateGroupAsOwnerAsync("Visible");

        var ownerView = await ListAsAsync(_scenario.OwnerUsername, _scenario.AccountId);
        var memberView = await ListAsAsync(_scenario.DirectMemberUsername, _scenario.AccountId);

        Assert.Contains(ownerView, e => e.ResourceIds.Contains(groupId));
        Assert.DoesNotContain(memberView, e => e.ResourceIds.Contains(groupId));
        Assert.All(memberView, e => Assert.Equal(_scenario.DirectMemberUserId, e.ActorUserId));
    }

    [Fact]
    public async Task Purge_RemovesOneAccountsEntries_AndLeavesTheRest()
    {
        await CreateGroupAsOwnerAsync("Purged");
        var otherBefore = await CountInAccountAsync(_scenario.OtherAccountId);
        var cutoff = _scenario.Host.TimeProvider.GetUtcNow().UtcDateTime.AddSeconds(1);

        var (count, purged) = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            async services =>
            {
                var audit = services.GetRequiredService<IAuditService>();
                var request = new PurgeAuditEntriesRequest(_scenario.AccountId, cutoff);
                return (
                    await audit.CountPurgeableEntriesAsync(request),
                    await audit.PurgeEntriesAsync(request)
                );
            }
        );

        Assert.Equal(PurgeAuditEntriesResultCode.Success, purged.ResultCode);
        Assert.True(purged.Count > 0);
        Assert.Equal(count.Count, purged.Count);
        Assert.Equal(
            0,
            await _scenario.Host.QueryAsync(db =>
                db.Set<AuditEntryEntity>()
                    .CountAsync(e =>
                        e.AccountId == _scenario.AccountId
                        && e.OccurredUtc < cutoff
                        && e.Operation != nameof(IAuditService.PurgeEntriesAsync)
                    )
            )
        );
        Assert.Equal(otherBefore, await CountInAccountAsync(_scenario.OtherAccountId));
    }

    [Fact]
    public async Task Purge_IsRefused_ForAMemberWithoutDeleteOnAudit()
    {
        var before = await CountInAccountAsync(_scenario.AccountId);

        var result = await _scenario.ActAsAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IAuditService>()
                    .PurgeEntriesAsync(new PurgeAuditEntriesRequest(_scenario.AccountId))
        );

        Assert.Equal(PurgeAuditEntriesResultCode.UnauthorizedError, result.ResultCode);
        Assert.True(await CountInAccountAsync(_scenario.AccountId) >= before);
    }

    [Fact]
    public async Task DeleteExpiredEntries_AppliesEachAccountsRetention_AndIsSafeToRunTwice()
    {
        var now = _scenario.Host.TimeProvider.GetUtcNow().UtcDateTime;
        var deletedAccountId = Guid.CreateVersion7();
        await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IAuditService>()
                    .UpdateAccountSettingsAsync(
                        new UpdateAccountAuditSettingsRequest(
                            _scenario.AccountId,
                            AuditActions.None,
                            AuditActions.None,
                            10
                        )
                    )
        );
        var shortExpired = await InsertEntryAsync(_scenario.AccountId, now.AddDays(-11));
        var shortKept = await InsertEntryAsync(_scenario.AccountId, now.AddDays(-9));
        var defaultKept = await InsertEntryAsync(_scenario.OtherAccountId, now.AddDays(-11));
        var defaultExpired = await InsertEntryAsync(_scenario.OtherAccountId, now.AddDays(-91));
        var deletedAccountKept = await InsertEntryAsync(deletedAccountId, now.AddDays(-200));
        var accountlessExpired = await InsertEntryAsync(null, now.AddDays(-366));

        var first = await DeleteExpiredAsync();
        var second = await DeleteExpiredAsync();

        Assert.Equal(DeleteExpiredAuditEntriesResultCode.Success, first.ResultCode);
        Assert.Equal(0, second.DeletedCount);
        var remaining = await _scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>().Select(e => e.Id).ToListAsync()
        );
        Assert.DoesNotContain(shortExpired, remaining);
        Assert.Contains(shortKept, remaining);
        Assert.Contains(defaultKept, remaining);
        Assert.DoesNotContain(defaultExpired, remaining);
        Assert.Contains(deletedAccountKept, remaining);
        Assert.DoesNotContain(accountlessExpired, remaining);
    }

    [Fact]
    public async Task DeleteExpiredEntries_IsRefused_OutsideSystemContext()
    {
        var result = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services => services.GetRequiredService<IAuditService>().DeleteExpiredEntriesAsync()
        );

        Assert.Equal(DeleteExpiredAuditEntriesResultCode.UnauthorizedError, result.ResultCode);
    }

    private Task<Guid> CreateGroupAsOwnerAsync(string name) =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            async services =>
            {
                var result = await services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterGroupAsync(new RegisterGroupRequest(name, _scenario.AccountId));
                Assert.Equal(Groups.Models.CreateGroupResultCode.Success, result.ResultCode);
                return result.CreatedGroupId;
            }
        );

    private Task UpdatePlatformSettingsAsync(PlatformSettings settings) =>
        _scenario.AsSystemAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IAuditService>()
                .UpdatePlatformSettingsAsync(settings);
            Assert.Equal(ModifyResultCode.Success, result.ResultCode);
            return true;
        });

    private Task<DeleteExpiredAuditEntriesResult> DeleteExpiredAsync() =>
        _scenario.AsSystemAsync(services =>
            services.GetRequiredService<IAuditService>().DeleteExpiredEntriesAsync()
        );

    private async Task<List<AuditEntry>> ListAsAsync(string username, Guid accountId)
    {
        var result = await _scenario.ActAsAsync(
            username,
            accountId,
            services =>
                services
                    .GetRequiredService<IAuditService>()
                    .ListEntriesAsync(new ListAuditEntriesRequest(Take: 500))
        );
        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        return result.Data!.Items;
    }

    private Task<AuditEntryEntity> SingleEntryAsync(string operation, Guid resourceId)
    {
        var id = resourceId.ToString();
        return _scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>()
                .SingleAsync(e =>
                    e.Operation == operation && e.ResourceIds != null && e.ResourceIds.Contains(id)
                )
        );
    }

    private Task<int> CountAsync(string operation, Guid? resourceId = null)
    {
        var id = resourceId?.ToString();
        return _scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>()
                .CountAsync(e =>
                    e.Operation == operation
                    && (id == null || (e.ResourceIds != null && e.ResourceIds.Contains(id)))
                )
        );
    }

    private Task<int> CountInAccountAsync(Guid accountId) =>
        _scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>().CountAsync(e => e.AccountId == accountId)
        );

    private async Task<Guid> InsertEntryAsync(Guid? accountId, DateTime occurredUtc)
    {
        var entry = new AuditEntryEntity
        {
            Id = Guid.CreateVersion7(),
            OccurredUtc = occurredUtc,
            AccountId = accountId,
            Cohort = accountId is null ? AuditCohort.Accountless : AuditCohort.AccountMember,
            Source = "tests",
            Service = nameof(IRegistrationService),
            Operation = "Seeded",
            Action = AuthAction.Update,
            ResourceType = "group",
            ResultCode = "Success",
        };
        await _scenario.Host.QueryAsync(async db =>
        {
            db.Set<AuditEntryEntity>().Add(entry);
            return await db.SaveChangesAsync();
        });
        return entry.Id;
    }
}
