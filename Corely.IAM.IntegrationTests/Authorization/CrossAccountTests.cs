using Corely.IAM.Groups.Entities;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class CrossAccountTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();
    private Guid _outsiderAccountId;

    public async ValueTask InitializeAsync()
    {
        await _scenario.InitializeAsync();
        _outsiderAccountId = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            null,
            async services =>
                (
                    await services
                        .GetRequiredService<IRegistrationService>()
                        .RegisterAccountAsync(
                            new RegisterAccountRequest("Outsider Co", _scenario.OutsiderUserId)
                        )
                ).CreatedAccountId
        );
    }

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    private Task<T> AsOutsiderOwnerAsync<T>(Func<IServiceProvider, Task<T>> work) =>
        _scenario.ActAsAsync(_scenario.OutsiderUsername, _outsiderAccountId, work);

    [Fact]
    public async Task AnOwnerOfOneAccount_CannotDeleteAnotherAccountsRole()
    {
        await AsOutsiderOwnerAsync(services =>
            services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterRoleAsync(
                    new DeregisterRoleRequest(_scenario.ReaderRoleId, _outsiderAccountId)
                )
        );

        Assert.True(
            await _scenario.Host.QueryAsync(db =>
                db.Set<RoleEntity>().AsNoTracking().AnyAsync(r => r.Id == _scenario.ReaderRoleId)
            )
        );
    }

    [Fact]
    public async Task AnOwnerOfOneAccount_CannotDeleteAnotherAccountsGroup()
    {
        await AsOutsiderOwnerAsync(services =>
            services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterGroupAsync(
                    new DeregisterGroupRequest(_scenario.EditorGroupId, _outsiderAccountId)
                )
        );

        Assert.True(
            await _scenario.Host.QueryAsync(db =>
                db.Set<GroupEntity>().AsNoTracking().AnyAsync(g => g.Id == _scenario.EditorGroupId)
            )
        );
    }

    [Fact]
    public async Task AnOwnerOfOneAccount_CannotAddRolesToAnotherAccountsGroup()
    {
        var outsiderRoleId = await AsOutsiderOwnerAsync(async services =>
            (
                await services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterRoleAsync(new RegisterRoleRequest("Mine", _outsiderAccountId))
            ).CreatedRoleId
        );

        await AsOutsiderOwnerAsync(services =>
            services
                .GetRequiredService<IRegistrationService>()
                .RegisterRolesWithGroupAsync(
                    new RegisterRolesWithGroupRequest(
                        [outsiderRoleId],
                        _scenario.EditorGroupId,
                        _outsiderAccountId
                    )
                )
        );

        Assert.False(
            await _scenario.Host.QueryAsync(db =>
                db.Set<GroupEntity>()
                    .AsNoTracking()
                    .Where(g => g.Id == _scenario.EditorGroupId)
                    .AnyAsync(g => g.Roles!.Any(r => r.Id == outsiderRoleId))
            )
        );
    }

    [Fact]
    public async Task AnOwnerOfOneAccount_CannotRemovePermissionsFromAnotherAccountsRole()
    {
        var before = await PermissionCountAsync(_scenario.EditorRoleId);

        await AsOutsiderOwnerAsync(async services =>
        {
            var permissionIds = await _scenario.Host.QueryAsync(db =>
                db.Set<RoleEntity>()
                    .AsNoTracking()
                    .Where(r => r.Id == _scenario.EditorRoleId)
                    .SelectMany(r => r.Permissions!.Select(p => p.Id))
                    .ToListAsync()
            );
            return await services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterPermissionsFromRoleAsync(
                    new DeregisterPermissionsFromRoleRequest(
                        permissionIds,
                        _scenario.EditorRoleId,
                        _outsiderAccountId
                    )
                );
        });

        Assert.Equal(before, await PermissionCountAsync(_scenario.EditorRoleId));
    }

    [Fact]
    public async Task AnOwnerOfOneAccount_CannotGiveAnotherAccountsRoleToItsMembers()
    {
        await AsOutsiderOwnerAsync(services =>
            services
                .GetRequiredService<IRegistrationService>()
                .RegisterRolesWithUserAsync(
                    new RegisterRolesWithUserRequest(
                        [_scenario.EditorRoleId],
                        _scenario.DirectMemberUserId,
                        _outsiderAccountId
                    )
                )
        );

        Assert.False(
            await UserHoldsRoleAsync(_scenario.DirectMemberUserId, _scenario.EditorRoleId)
        );
    }

    [Fact]
    public async Task AnOwnerOfOneAccount_CannotAddMembersToAnotherAccountsGroup()
    {
        await AsOutsiderOwnerAsync(services =>
            services
                .GetRequiredService<IRegistrationService>()
                .RegisterUsersWithGroupAsync(
                    new RegisterUsersWithGroupRequest(
                        [_scenario.DirectMemberUserId],
                        _scenario.EditorGroupId,
                        _outsiderAccountId
                    )
                )
        );

        Assert.False(
            await _scenario.Host.QueryAsync(db =>
                db.Set<GroupEntity>()
                    .AsNoTracking()
                    .Where(g => g.Id == _scenario.EditorGroupId)
                    .AnyAsync(g => g.Users!.Any(u => u.Id == _scenario.DirectMemberUserId))
            )
        );
    }

    [Fact]
    public async Task AnOwnerStillManagesTheirOwnAccount()
    {
        var deleted = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterRoleAsync(
                        new DeregisterRoleRequest(_scenario.ReaderRoleId, _scenario.AccountId)
                    )
        );

        Assert.Equal(DeregisterRoleResultCode.Success, deleted.ResultCode);
    }

    private Task<bool> UserHoldsRoleAsync(Guid userId, Guid roleId) =>
        _scenario.Host.QueryAsync(db =>
            db.Set<RoleEntity>()
                .AsNoTracking()
                .Where(r => r.Id == roleId)
                .AnyAsync(r => r.Users!.Any(u => u.Id == userId))
        );

    private Task<int> PermissionCountAsync(Guid roleId) =>
        _scenario.Host.QueryAsync(db =>
            db.Set<RoleEntity>()
                .AsNoTracking()
                .Where(r => r.Id == roleId)
                .SelectMany(r => r.Permissions!)
                .CountAsync()
        );
}
