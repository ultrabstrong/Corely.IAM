using Corely.IAM.Groups.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class GrantOnlyWhatYouHoldTests : IAsyncLifetime
{
    private const string REFUSAL = "Cannot grant permissions you do not hold";

    private readonly IamScenario _scenario = new();
    private Guid _delegateRoleId;
    private Guid _powerRoleId;
    private Guid _powerGroupId;
    private Guid _ownerRoleId;

    public async ValueTask InitializeAsync()
    {
        await _scenario.InitializeAsync();

        _delegateRoleId = await AsOwnerAsync(r => CreateRoleAsync(r, "Delegate"));
        foreach (
            var (type, create, update) in new[]
            {
                (PermissionConstants.PERMISSION_RESOURCE_TYPE, true, false),
                (PermissionConstants.ROLE_RESOURCE_TYPE, false, true),
                (PermissionConstants.USER_RESOURCE_TYPE, false, true),
                (PermissionConstants.GROUP_RESOURCE_TYPE, false, true),
            }
        )
        {
            var permissionId = await AsOwnerAsync(r =>
                CreatePermissionAsync(r, type, create: create, read: true, update: update)
            );
            await AsOwnerAsync(r => AttachAsync(r, permissionId, _delegateRoleId));
        }
        await AsOwnerAsync(async r =>
        {
            var assigned = await r.RegisterRolesWithUserAsync(
                new RegisterRolesWithUserRequest(
                    [_delegateRoleId],
                    _scenario.DirectMemberUserId,
                    _scenario.AccountId
                )
            );
            Assert.Equal(AssignRolesToUserResultCode.Success, assigned.ResultCode);
            return true;
        });

        _powerRoleId = await AsOwnerAsync(r => CreateRoleAsync(r, "Power"));
        var userDelete = await AsOwnerAsync(r =>
            CreatePermissionAsync(r, PermissionConstants.USER_RESOURCE_TYPE, delete: true)
        );
        await AsOwnerAsync(r => AttachAsync(r, userDelete, _powerRoleId));
        _powerGroupId = await AsOwnerAsync(async r =>
        {
            var group = await r.RegisterGroupAsync(
                new RegisterGroupRequest("Power group", _scenario.AccountId)
            );
            Assert.Equal(CreateGroupResultCode.Success, group.ResultCode);
            return group.CreatedGroupId;
        });
        await AsOwnerAsync(async r =>
        {
            var assigned = await r.RegisterRolesWithGroupAsync(
                new RegisterRolesWithGroupRequest(
                    [_powerRoleId],
                    _powerGroupId,
                    _scenario.AccountId
                )
            );
            Assert.Equal(AssignRolesToGroupResultCode.Success, assigned.ResultCode);
            return true;
        });

        _ownerRoleId = await _scenario.Host.QueryAsync(db =>
            db.Set<RoleEntity>()
                .Where(r =>
                    r.AccountId == _scenario.AccountId && r.Name == RoleConstants.OWNER_ROLE_NAME
                )
                .Select(r => r.Id)
                .SingleAsync()
        );
    }

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task Delegate_CreatesPermission_ForActionsTheyHold()
    {
        var result = await AsDelegateAsync(r =>
            r.RegisterPermissionAsync(
                new RegisterPermissionRequest(
                    _scenario.AccountId,
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    _scenario.EditorGroupId,
                    Read: true,
                    Update: true
                )
            )
        );

        Assert.Equal(CreatePermissionResultCode.Success, result.ResultCode);
    }

    [Theory]
    [InlineData(PermissionConstants.USER_RESOURCE_TYPE, false, true)]
    [InlineData(IamScenario.INVOICE_RESOURCE_TYPE, true, false)]
    [InlineData(PermissionConstants.ACCOUNT_RESOURCE_TYPE, false, false)]
    public async Task Delegate_CannotCreatePermission_ForActionsTheyDoNotHold(
        string resourceType,
        bool create,
        bool delete
    )
    {
        var result = await AsDelegateAsync(r =>
            r.RegisterPermissionAsync(
                new RegisterPermissionRequest(
                    _scenario.AccountId,
                    resourceType,
                    Guid.Empty,
                    Create: create,
                    Read: !create && !delete,
                    Delete: delete
                )
            )
        );

        Assert.Equal(CreatePermissionResultCode.UnauthorizedError, result.ResultCode);
        Assert.Equal(REFUSAL, result.Message);
    }

    [Fact]
    public async Task Delegate_AssignsRole_WithinWhatTheyHold()
    {
        var result = await AsDelegateAsync(r =>
            r.RegisterRolesWithUserAsync(
                new RegisterRolesWithUserRequest(
                    [_scenario.ReaderRoleId],
                    _scenario.GroupMemberUserId,
                    _scenario.AccountId
                )
            )
        );

        Assert.Equal(AssignRolesToUserResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task Delegate_CannotAssignTheOwnerRole_ToThemselves()
    {
        var result = await AsDelegateAsync(r =>
            r.RegisterRolesWithUserAsync(
                new RegisterRolesWithUserRequest(
                    [_ownerRoleId],
                    _scenario.DirectMemberUserId,
                    _scenario.AccountId
                )
            )
        );

        Assert.Equal(AssignRolesToUserResultCode.UnauthorizedError, result.ResultCode);
        Assert.Equal(REFUSAL, result.Message);
    }

    [Fact]
    public async Task Delegate_CannotJoinAGroup_WhoseRolesHoldMore()
    {
        var result = await AsDelegateAsync(r =>
            r.RegisterUsersWithGroupAsync(
                new RegisterUsersWithGroupRequest(
                    [_scenario.DirectMemberUserId],
                    _powerGroupId,
                    _scenario.AccountId
                )
            )
        );

        Assert.Equal(AddUsersToGroupResultCode.UnauthorizedError, result.ResultCode);
        Assert.Equal(REFUSAL, result.Message);
    }

    [Fact]
    public async Task Delegate_CannotGiveAGroup_ARoleHoldingMore()
    {
        var result = await AsDelegateAsync(r =>
            r.RegisterRolesWithGroupAsync(
                new RegisterRolesWithGroupRequest(
                    [_powerRoleId],
                    _scenario.EditorGroupId,
                    _scenario.AccountId
                )
            )
        );

        Assert.Equal(AssignRolesToGroupResultCode.UnauthorizedError, result.ResultCode);
        Assert.Equal(REFUSAL, result.Message);
    }

    [Fact]
    public async Task Delegate_CannotAttachAPermission_TheyDoNotHold()
    {
        var userDelete = await AsOwnerAsync(r =>
            CreatePermissionAsync(
                r,
                PermissionConstants.USER_RESOURCE_TYPE,
                update: true,
                delete: true
            )
        );

        var result = await AsDelegateAsync(r =>
            r.RegisterPermissionsWithRoleAsync(
                new RegisterPermissionsWithRoleRequest(
                    [userDelete],
                    _delegateRoleId,
                    _scenario.AccountId
                )
            )
        );

        Assert.Equal(AssignPermissionsToRoleResultCode.UnauthorizedError, result.ResultCode);
        Assert.Equal(REFUSAL, result.Message);
    }

    [Fact]
    public async Task Owner_CreatesPermission_WithinOwnerDefaults()
    {
        var result = await AsOwnerAsync(r =>
            r.RegisterPermissionAsync(
                new RegisterPermissionRequest(
                    _scenario.AccountId,
                    IamScenario.INVOICE_RESOURCE_TYPE,
                    Guid.CreateVersion7(),
                    Read: true
                )
            )
        );

        Assert.Equal(CreatePermissionResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task Owner_CannotCreatePermission_BeyondOwnerDefaults()
    {
        var result = await AsOwnerAsync(r =>
            r.RegisterPermissionAsync(
                new RegisterPermissionRequest(
                    _scenario.AccountId,
                    IamScenario.INVOICE_RESOURCE_TYPE,
                    Guid.Empty,
                    Create: true
                )
            )
        );

        Assert.Equal(CreatePermissionResultCode.UnauthorizedError, result.ResultCode);
        Assert.Equal(REFUSAL, result.Message);
    }

    private Task<T> AsOwnerAsync<T>(Func<IRegistrationService, Task<T>> work) =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services => work(services.GetRequiredService<IRegistrationService>())
        );

    [Fact]
    public async Task Delegate_SeesOnlyRolesWithinWhatTheyHold_ForGrantableRoles()
    {
        var grantable = await _scenario.ActAsAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IAuthorizationProvider>()
                    .GetGrantableRoleIdsAsync([_delegateRoleId, _powerRoleId, _ownerRoleId])
        );

        Assert.Equal([_delegateRoleId], grantable);
    }

    [Fact]
    public async Task Delegate_SeesNoGroupWhoseRolesHoldMore_ForGrantableGroups()
    {
        var grantable = await _scenario.ActAsAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IAuthorizationProvider>()
                    .GetGrantableGroupIdsAsync([_powerGroupId])
        );

        Assert.Empty(grantable);
    }

    private Task<T> AsDelegateAsync<T>(Func<IRegistrationService, Task<T>> work) =>
        _scenario.ActAsAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            services => work(services.GetRequiredService<IRegistrationService>())
        );

    private async Task<Guid> CreateRoleAsync(IRegistrationService registration, string name)
    {
        var role = await registration.RegisterRoleAsync(
            new RegisterRoleRequest(name, _scenario.AccountId)
        );
        Assert.Equal(CreateRoleResultCode.Success, role.ResultCode);
        return role.CreatedRoleId;
    }

    private async Task<Guid> CreatePermissionAsync(
        IRegistrationService registration,
        string resourceType,
        bool create = false,
        bool read = false,
        bool update = false,
        bool delete = false
    )
    {
        var result = await registration.RegisterPermissionAsync(
            new RegisterPermissionRequest(
                _scenario.AccountId,
                resourceType,
                Guid.Empty,
                create,
                read,
                update,
                delete
            )
        );
        Assert.Equal(CreatePermissionResultCode.Success, result.ResultCode);
        return result.CreatedPermissionId;
    }

    private async Task<bool> AttachAsync(
        IRegistrationService registration,
        Guid permissionId,
        Guid roleId
    )
    {
        var result = await registration.RegisterPermissionsWithRoleAsync(
            new RegisterPermissionsWithRoleRequest([permissionId], roleId, _scenario.AccountId)
        );
        Assert.Equal(AssignPermissionsToRoleResultCode.Success, result.ResultCode);
        return true;
    }
}
