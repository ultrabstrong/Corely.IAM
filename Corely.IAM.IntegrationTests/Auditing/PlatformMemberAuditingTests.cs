using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Auditing;

public class PlatformMemberAuditingTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();
    private Guid _platformAccountId;

    public async ValueTask InitializeAsync()
    {
        await _scenario.InitializeAsync();

        var result = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest(
                        "Platform",
                        "platform-owner",
                        "platform-owner@example.com"
                    )
                )
        );
        Assert.Equal(BootstrapPlatformResultCode.Success, result.ResultCode);
        _platformAccountId = result.Credentials!.AccountId;

        await GivePlatformRoleAsync(
            _scenario.OutsiderUserId,
            (PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read]),
            (PermissionConstants.GROUP_RESOURCE_TYPE, [AuthAction.Create]),
            (AuditConstants.AUDIT_RESOURCE_TYPE, [AuthAction.Read])
        );
    }

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task APlatformMembersAction_InACustomerAccount_IsRecordedAsAPlatformMember()
    {
        var groupId = await CreateGroupAsPlatformMemberAsync("Entered");

        var entry = await EntryForAsync(groupId);

        Assert.NotNull(entry);
        Assert.Equal(AuditCohort.PlatformMember, entry.Cohort);
        Assert.Equal(_scenario.OutsiderUserId, entry.ActorUserId);
        Assert.Equal(_scenario.AccountId, entry.AccountId);
    }

    [Fact]
    public async Task EnteringACustomerAccount_IsRecordedInThatAccount_WhenItRecordsExecute()
    {
        Assert.Equal(0, await SwitchEntriesAsync());

        await UpdateAccountSettingsAsync(AuditActions.All, AuditActions.All);
        await CreateGroupAsPlatformMemberAsync("Switch");

        Assert.Equal(1, await SwitchEntriesAsync());
    }

    [Fact]
    public async Task AnAccountThatSwitchesPlatformMembersOff_GetsNothingRecordedForThem()
    {
        await UpdateAccountSettingsAsync(AuditActions.All, AuditActions.None);

        var groupId = await CreateGroupAsPlatformMemberAsync("Declined");

        Assert.Null(await EntryForAsync(groupId));
    }

    [Fact]
    public async Task APlatformMemberWithAuditRead_SeesEveryAccountAndEntriesOutsideAnyAccount()
    {
        var entries = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            null,
            services =>
                services
                    .GetRequiredService<IAuditService>()
                    .ListEntriesAsync(new ListAuditEntriesRequest(Take: 1000))
        );

        Assert.Equal(RetrieveResultCode.Success, entries.ResultCode);
        Assert.Contains(entries.Data!.Items, e => e.AccountId == _scenario.OtherAccountId);
        Assert.Contains(
            entries.Data.Items,
            e => e.AccountId is null && e.ActorUserId != _scenario.OutsiderUserId
        );
    }

    [Fact]
    public async Task ACustomerOwner_CannotReadThePlatformSettings()
    {
        var result = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services => services.GetRequiredService<IAuditService>().GetPlatformSettingsAsync()
        );

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
    }

    private async Task UpdateAccountSettingsAsync(
        AuditActions accountMemberActions,
        AuditActions platformMemberActions
    )
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
                            accountMemberActions,
                            platformMemberActions,
                            30
                        )
                    )
        );
        Assert.Equal(ModifyResultCode.Success, updated.ResultCode);
    }

    private Task<int> SwitchEntriesAsync() =>
        _scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>()
                .CountAsync(e =>
                    e.Operation == nameof(IAuthenticationService.SwitchAccountAsync)
                    && e.AccountId == _scenario.AccountId
                    && e.ActorUserId == _scenario.OutsiderUserId
                    && e.Cohort == AuditCohort.PlatformMember
                )
        );

    private Task<Guid> CreateGroupAsPlatformMemberAsync(string name) =>
        _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
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

    private Task<AuditEntryEntity?> EntryForAsync(Guid groupId)
    {
        var id = groupId.ToString();
        return _scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>()
                .SingleOrDefaultAsync(e =>
                    e.Operation == nameof(IRegistrationService.RegisterGroupAsync)
                    && e.ResourceIds != null
                    && e.ResourceIds.Contains(id)
                )
        );
    }

    private Task GivePlatformRoleAsync(
        Guid userId,
        params (string ResourceType, AuthAction[] Actions)[] grants
    ) =>
        _scenario.AsSystemAsync(async services =>
        {
            var registration = services.GetRequiredService<IRegistrationService>();
            await registration.RegisterUserWithAccountAsync(
                new RegisterUserWithAccountRequest(userId, _platformAccountId)
            );
            var role = await registration.RegisterRoleAsync(
                new RegisterRoleRequest("Platform auditors", _platformAccountId)
            );
            var permissionIds = new List<Guid>();
            foreach (var (resourceType, actions) in grants)
            {
                var permission = await registration.RegisterPermissionAsync(
                    new RegisterPermissionRequest(
                        _platformAccountId,
                        resourceType,
                        Guid.Empty,
                        actions.Contains(AuthAction.Create),
                        actions.Contains(AuthAction.Read),
                        actions.Contains(AuthAction.Update),
                        actions.Contains(AuthAction.Delete),
                        actions.Contains(AuthAction.Execute)
                    )
                );
                Assert.Equal(CreatePermissionResultCode.Success, permission.ResultCode);
                permissionIds.Add(permission.CreatedPermissionId);
            }
            await registration.RegisterPermissionsWithRoleAsync(
                new RegisterPermissionsWithRoleRequest(
                    permissionIds,
                    role.CreatedRoleId,
                    _platformAccountId
                )
            );
            await registration.RegisterRolesWithUserAsync(
                new RegisterRolesWithUserRequest([role.CreatedRoleId], userId, _platformAccountId)
            );
            return true;
        });
}
