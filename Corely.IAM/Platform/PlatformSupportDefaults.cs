using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;
using Corely.IAM.Permissions;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Platform;

internal sealed class PlatformSupportDefaults(
    IRepo<AccountEntity> accountRepo,
    IRepo<RoleEntity> roleRepo,
    IRepo<GroupEntity> groupRepo,
    IRepo<PermissionEntity> permissionRepo,
    IResourceTypeRegistry resourceTypeRegistry,
    ILogger<PlatformSupportDefaults> logger
)
{
    public async Task<int> CompleteAsync()
    {
        var platformAccount = await accountRepo.GetAsync(a => a.IsPlatformAccount);
        if (platformAccount is null)
            return 0;

        var types = resourceTypeRegistry.GetAll();
        var viewer = await CompleteRoleAsync(
            platformAccount.Id,
            Role.ACCOUNT_VIEWER_ROLE_NAME,
            "Sees every account and what is in it, and changes nothing",
            [
                .. types
                    .Where(t => t.SupportActions.Contains(AuthAction.Read))
                    .Select(t => new SupportPermission(t.Name, [AuthAction.Read])),
            ]
        );
        var setup = await CompleteRoleAsync(
            platformAccount.Id,
            Role.ACCOUNT_SETUP_ROLE_NAME,
            "Makes the changes support may make in any account, such as approving a grant",
            [
                .. types
                    .Select(t => new SupportPermission(
                        t.Name,
                        [.. t.SupportActions.Where(a => a != AuthAction.Read)]
                    ))
                    .Where(p => p.Actions.Count > 0),
            ]
        );
        var changes =
            viewer.Changes
            + setup.Changes
            + await CompleteGroupAsync(platformAccount.Id, [viewer.Role, setup.Role]);

        if (changes > 0)
        {
            logger.LogInformation(
                "Made {Count} changes to the platform account's support group and roles",
                changes
            );
        }
        return changes;
    }

    private async Task<(RoleEntity Role, int Changes)> CompleteRoleAsync(
        Guid platformAccountId,
        string name,
        string description,
        IReadOnlyList<SupportPermission> wanted
    )
    {
        var changes = 0;
        var role = await roleRepo.GetAsync(
            r => r.AccountId == platformAccountId && r.Name == name,
            include: q => q.Include(r => r.Permissions)
        );
        if (role is null)
        {
            role = new RoleEntity
            {
                Id = Guid.CreateVersion7(),
                AccountId = platformAccountId,
                Name = name,
                Description = description,
                IsSystemDefined = true,
                Permissions = [],
            };
            await roleRepo.CreateAsync(role);
            changes++;
        }

        foreach (var permission in wanted)
        {
            if (role.Permissions!.Any(p => permission.Matches(p)))
                continue;

            var create = permission.Actions.Contains(AuthAction.Create);
            var read = permission.Actions.Contains(AuthAction.Read);
            var update = permission.Actions.Contains(AuthAction.Update);
            var delete = permission.Actions.Contains(AuthAction.Delete);
            var execute = permission.Actions.Contains(AuthAction.Execute);
            var existing = await permissionRepo.GetAsync(
                p =>
                    p.AccountId == platformAccountId
                    && p.ResourceType == permission.ResourceType
                    && p.ResourceId == Guid.Empty
                    && p.Create == create
                    && p.Read == read
                    && p.Update == update
                    && p.Delete == delete
                    && p.Execute == execute,
                include: q => q.Include(p => p.Roles)
            );
            if (existing is null)
            {
                await permissionRepo.CreateAsync(
                    new PermissionEntity
                    {
                        Id = Guid.CreateVersion7(),
                        AccountId = platformAccountId,
                        ResourceType = permission.ResourceType,
                        ResourceId = Guid.Empty,
                        Create = create,
                        Read = read,
                        Update = update,
                        Delete = delete,
                        Execute = execute,
                        Description = PermissionLabelProvider.GetName(
                            permission.ResourceType,
                            Guid.Empty,
                            create,
                            read,
                            update,
                            delete,
                            execute
                        ),
                        IsSystemDefined = true,
                        Roles = [role],
                    }
                );
            }
            else
            {
                existing.IsSystemDefined = true;
                existing.Roles!.Add(role);
                await permissionRepo.UpdateAsync(existing);
            }
            changes++;
        }

        var extras = role.Permissions!.Where(p => !wanted.Any(w => w.Matches(p))).ToList();
        if (extras.Count > 0 || !role.IsSystemDefined)
        {
            foreach (var extra in extras)
                role.Permissions!.Remove(extra);
            changes += extras.Count + (role.IsSystemDefined ? 0 : 1);
            role.IsSystemDefined = true;
            role.Description ??= description;
            await roleRepo.UpdateAsync(role);
        }

        return (role, changes);
    }

    private async Task<int> CompleteGroupAsync(
        Guid platformAccountId,
        IReadOnlyList<RoleEntity> roles
    )
    {
        var group = await groupRepo.GetAsync(
            g => g.AccountId == platformAccountId && g.Name == Group.ACCOUNT_SUPPORT_GROUP_NAME,
            include: q => q.Include(g => g.Roles)
        );
        if (group is null)
        {
            await groupRepo.CreateAsync(
                new GroupEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = platformAccountId,
                    Name = Group.ACCOUNT_SUPPORT_GROUP_NAME,
                    Description = "Platform members who look into accounts and help set them up",
                    Roles = [.. roles],
                }
            );
            return 1;
        }

        var missing = roles.Where(r => !group.Roles!.Any(gr => gr.Id == r.Id)).ToList();
        if (missing.Count == 0)
            return 0;
        foreach (var role in missing)
            group.Roles!.Add(role);
        await groupRepo.UpdateAsync(group);
        return missing.Count;
    }
}
