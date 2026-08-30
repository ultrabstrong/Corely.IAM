using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

/// <summary>
/// I3 - many-to-many join entities and delete behaviour.
///
/// SQL Server forbids cascade deletes on M:M relationships, so every such relationship uses an
/// explicit join entity with <c>DeleteBehavior.NoAction</c>, and processors must Include and Clear
/// the collections before deleting the parent. That contract is invisible to a mock repo: it only
/// exists in the database's referential integrity rules.
/// </summary>
public class JoinEntityDeleteTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public Task InitializeAsync() => _scenario.InitializeAsync();

    public Task DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task DeletingAGroupWithMembersAndRoles_Succeeds()
    {
        var result = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterGroupAsync(
                        new DeregisterGroupRequest(_scenario.EditorGroupId, _scenario.AccountId)
                    )
        );

        Assert.Equal(DeregisterGroupResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DeletingAGroup_LeavesItsUsersIntact()
    {
        await DeleteEditorGroupAsync();

        var userStillExists = await _scenario.Host.QueryAsync(db =>
            db.Users.AsNoTracking().AnyAsync(u => u.Id == _scenario.GroupMemberUserId)
        );

        Assert.True(userStillExists, "Deleting a group must not delete its members.");
    }

    [Fact]
    public async Task DeletingAGroup_LeavesItsRolesIntact()
    {
        await DeleteEditorGroupAsync();

        var roleStillExists = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetRoleAsync(_scenario.EditorRoleId)
        );

        Assert.Equal(RetrieveResultCode.Success, roleStillExists.ResultCode);
    }

    [Fact]
    public async Task DeletingARoleWithPermissionsAndAssignees_Succeeds()
    {
        var result = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterRoleAsync(
                        new DeregisterRoleRequest(_scenario.ReaderRoleId, _scenario.AccountId)
                    )
        );

        Assert.Equal(DeregisterRoleResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DeletingARole_LeavesItsAssignedUserIntact()
    {
        var deletion = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterRoleAsync(
                        new DeregisterRoleRequest(_scenario.ReaderRoleId, _scenario.AccountId)
                    )
        );
        Assert.Equal(DeregisterRoleResultCode.Success, deletion.ResultCode);

        var userStillExists = await _scenario.Host.QueryAsync(db =>
            db.Users.AsNoTracking().AnyAsync(u => u.Id == _scenario.DirectMemberUserId)
        );

        Assert.True(userStillExists);
    }

    [Fact]
    public async Task ARelationshipCanBeReAddedAfterRemoval()
    {
        var removal = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterUsersFromGroupAsync(
                        new DeregisterUsersFromGroupRequest(
                            [_scenario.GroupMemberUserId],
                            _scenario.EditorGroupId,
                            _scenario.AccountId
                        )
                    )
        );
        Assert.Equal(DeregisterUsersFromGroupResultCode.Success, removal.ResultCode);

        var readd = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterUsersWithGroupAsync(
                        new RegisterUsersWithGroupRequest(
                            [_scenario.GroupMemberUserId],
                            _scenario.EditorGroupId,
                            _scenario.AccountId
                        )
                    )
        );

        Assert.Equal(Groups.Models.AddUsersToGroupResultCode.Success, readd.ResultCode);
    }

    private async Task DeleteEditorGroupAsync()
    {
        var result = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterGroupAsync(
                        new DeregisterGroupRequest(_scenario.EditorGroupId, _scenario.AccountId)
                    )
        );
        Assert.Equal(DeregisterGroupResultCode.Success, result.ResultCode);
    }
}
