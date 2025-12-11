using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Mappers;
using Corely.IAM.Groups.Models;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Processors;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Groups.Processors;

internal class GroupProcessor : IGroupProcessor
{
    private readonly IRepo<GroupEntity> _groupRepo;
    private readonly IReadonlyRepo<AccountEntity> _accountRepo;
    private readonly IReadonlyRepo<UserEntity> _userRepo;
    private readonly IReadonlyRepo<RoleEntity> _roleRepo;
    private readonly IUserOwnershipProcessor _userOwnershipProcessor;
    private readonly IValidationProvider _validationProvider;
    private readonly ILogger<GroupProcessor> _logger;

    public GroupProcessor(
        IRepo<GroupEntity> groupRepo,
        IReadonlyRepo<AccountEntity> accountRepo,
        IReadonlyRepo<UserEntity> userRepo,
        IReadonlyRepo<RoleEntity> roleRepo,
        IUserOwnershipProcessor userOwnershipProcessor,
        IValidationProvider validationProvider,
        ILogger<GroupProcessor> logger
    )
    {
        _groupRepo = groupRepo.ThrowIfNull(nameof(groupRepo));
        _accountRepo = accountRepo.ThrowIfNull(nameof(accountRepo));
        _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
        _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
        _userOwnershipProcessor = userOwnershipProcessor.ThrowIfNull(
            nameof(userOwnershipProcessor)
        );
        _validationProvider = validationProvider.ThrowIfNull(nameof(validationProvider));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

    public async Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var group = request.ToGroup();
        _validationProvider.ThrowIfInvalid(group);

        if (await _groupRepo.AnyAsync(g => g.AccountId == group.AccountId && g.Name == group.Name))
        {
            _logger.LogWarning("Group with name {GroupName} already exists", group.Name);
            return new CreateGroupResult(
                CreateGroupResultCode.GroupExistsError,
                $"Group with name {group.Name} already exists",
                -1
            );
        }

        var accountEntity = await _accountRepo.GetAsync(a => a.Id == group.AccountId);
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", group.AccountId);
            return new CreateGroupResult(
                CreateGroupResultCode.AccountNotFoundError,
                $"Account with Id {group.AccountId} not found",
                -1
            );
        }

        var groupEntity = group.ToEntity();
        var created = await _groupRepo.CreateAsync(groupEntity);

        return new CreateGroupResult(CreateGroupResultCode.Success, string.Empty, created.Id);
    }

    public async Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var groupEntity = await _groupRepo.GetAsync(g => g.Id == request.GroupId);
        if (groupEntity == null)
        {
            _logger.LogWarning("Group with Id {GroupId} not found", request.GroupId);
            return new AddUsersToGroupResult(
                AddUsersToGroupResultCode.GroupNotFoundError,
                $"Group with Id {request.GroupId} not found",
                0,
                request.UserIds
            );
        }

        var userEntities = await _userRepo.ListAsync(u =>
            request.UserIds.Contains(u.Id)
            && !u.Groups!.Any(g => g.Id == groupEntity.Id)
            && u.Accounts!.Any(a => a.Id == groupEntity.AccountId)
        );

        if (userEntities.Count == 0)
        {
            _logger.LogInformation(
                "All user ids are invalid (not found, from different account, or already exist in group) : {@InvalidUserIds}",
                request.UserIds
            );
            return new AddUsersToGroupResult(
                AddUsersToGroupResultCode.InvalidUserIdsError,
                "All user ids are invalid (not found, from different account, or already exist in group)",
                0,
                request.UserIds
            );
        }

        groupEntity.Users ??= [];
        foreach (var user in userEntities)
        {
            groupEntity.Users.Add(user);
        }

        await _groupRepo.UpdateAsync(groupEntity);

        var invalidUserIds = request.UserIds.Except(userEntities.Select(u => u.Id)).ToList();
        if (invalidUserIds.Count > 0)
        {
            _logger.LogInformation(
                "Some user ids are invalid (not found, from different account, or already exist in group) : {@InvalidUserIds}",
                invalidUserIds
            );
            return new AddUsersToGroupResult(
                AddUsersToGroupResultCode.PartialSuccess,
                "Some user ids are invalid (not found, from different account, or already exist in group)",
                userEntities.Count,
                invalidUserIds
            );
        }

        return new AddUsersToGroupResult(
            AddUsersToGroupResultCode.Success,
            string.Empty,
            userEntities.Count,
            invalidUserIds
        );
    }

    public async Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(
        RemoveUsersFromGroupRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var groupEntity = await _groupRepo.GetAsync(
            g => g.Id == request.GroupId,
            include: q => q.Include(g => g.Users).Include(g => g.Roles)
        );
        if (groupEntity == null)
        {
            _logger.LogWarning("Group with Id {GroupId} not found", request.GroupId);
            return new RemoveUsersFromGroupResult(
                RemoveUsersFromGroupResultCode.GroupNotFoundError,
                $"Group with Id {request.GroupId} not found",
                0,
                request.UserIds
            );
        }

        var usersToRemove =
            groupEntity.Users?.Where(u => request.UserIds.Contains(u.Id)).ToList() ?? [];

        var groupHasOwnerRole =
            groupEntity.Roles?.Any(r =>
                r.Name == RoleConstants.OWNER_ROLE_NAME && r.IsSystemDefined
            ) ?? false;
        var totalUsersInGroup = groupEntity.Users?.Count ?? 0;
        var usersRemainingAfterRemoval = totalUsersInGroup - usersToRemove.Count;

        // Ownership check logic:
        // Disallow this action if it results in account not having an owner
        // 1. If group does not have owner role -> allow removal
        // 2. If group has owner role AND some users remain -> allow removal (remaining users still have owner role)
        // 3. If group has owner role AND all users being removed -> check if any user has ownership elsewhere
        if (groupHasOwnerRole && usersRemainingAfterRemoval == 0 && usersToRemove.Count > 0)
        {
            var anyUserHasOwnershipElsewhere =
                await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
                    usersToRemove.Select(u => u.Id),
                    groupEntity.AccountId,
                    request.GroupId
                );

            if (!anyUserHasOwnershipElsewhere)
            {
                _logger.LogWarning(
                    "Cannot remove all users from group {GroupId} - it has the owner role and no user has ownership elsewhere",
                    request.GroupId
                );
                return new RemoveUsersFromGroupResult(
                    RemoveUsersFromGroupResultCode.UserIsSoleOwnerError,
                    "Cannot remove all users from a group with the owner role when no user has ownership elsewhere. "
                        + "Add another owner first, or remove the owner role from this group.",
                    0,
                    [],
                    usersToRemove.Select(u => u.Id).ToList()
                );
            }
        }

        foreach (var user in usersToRemove)
        {
            groupEntity.Users!.Remove(user);
        }

        if (usersToRemove.Count > 0)
        {
            await _groupRepo.UpdateAsync(groupEntity);
        }

        var invalidUserIds = request.UserIds.Except(usersToRemove.Select(u => u.Id)).ToList();
        if (invalidUserIds.Count > 0)
        {
            _logger.LogInformation(
                "Some user ids were not in group (not found or not a member) : {@InvalidUserIds}",
                invalidUserIds
            );
            return new RemoveUsersFromGroupResult(
                RemoveUsersFromGroupResultCode.PartialSuccess,
                "Some user ids were not in group (not found or not a member)",
                usersToRemove.Count,
                invalidUserIds
            );
        }

        return new RemoveUsersFromGroupResult(
            RemoveUsersFromGroupResultCode.Success,
            string.Empty,
            usersToRemove.Count,
            invalidUserIds
        );
    }

    public async Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(
        AssignRolesToGroupRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var groupEntity = await _groupRepo.GetAsync(g => g.Id == request.GroupId);
        if (groupEntity == null)
        {
            _logger.LogWarning("Group with Id {GroupId} not found", request.GroupId);
            return new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.GroupNotFoundError,
                $"Group with Id {request.GroupId} not found",
                0,
                request.RoleIds
            );
        }

        var roleEntities = await _roleRepo.ListAsync(r =>
            request.RoleIds.Contains(r.Id)
            && !r.Groups!.Any(g => g.Id == groupEntity.Id)
            && r.Account!.Id == groupEntity.AccountId
        );

        if (roleEntities.Count == 0)
        {
            _logger.LogInformation(
                "All role ids are invalid (not found, from different account, or already assigned to group) : {@InvalidRoleIds}",
                request.RoleIds
            );
            return new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.InvalidRoleIdsError,
                "All role ids are invalid (not found, from different account, or already assigned to group)",
                0,
                request.RoleIds
            );
        }

        groupEntity.Roles ??= [];
        foreach (var role in roleEntities)
        {
            groupEntity.Roles.Add(role);
        }

        await _groupRepo.UpdateAsync(groupEntity);

        var invalidRoleIds = request.RoleIds.Except(roleEntities.Select(r => r.Id)).ToList();
        if (invalidRoleIds.Count > 0)
        {
            _logger.LogInformation(
                "Some role ids are invalid (not found, from different account, or already assigned to group) : {@InvalidRoleIds}",
                invalidRoleIds
            );
            return new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.PartialSuccess,
                "Some role ids are invalid (not found, from different account, or already assigned to group)",
                roleEntities.Count,
                invalidRoleIds
            );
        }

        return new AssignRolesToGroupResult(
            AssignRolesToGroupResultCode.Success,
            string.Empty,
            roleEntities.Count,
            invalidRoleIds
        );
    }

    public async Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(
        RemoveRolesFromGroupRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var groupEntity = await _groupRepo.GetAsync(
            g => g.Id == request.GroupId,
            include: q => q.Include(g => g.Roles).Include(g => g.Users)
        );

        if (groupEntity == null)
        {
            _logger.LogWarning("Group with Id {GroupId} not found", request.GroupId);
            return new RemoveRolesFromGroupResult(
                RemoveRolesFromGroupResultCode.GroupNotFoundError,
                $"Group with Id {request.GroupId} not found",
                0,
                request.RoleIds
            );
        }

        var rolesToRemove =
            groupEntity.Roles?.Where(r => request.RoleIds.Contains(r.Id)).ToList() ?? [];

        if (rolesToRemove.Count == 0)
        {
            _logger.LogInformation(
                "All role ids are invalid (not found or not assigned to group) : {@InvalidRoleIds}",
                request.RoleIds
            );
            return new RemoveRolesFromGroupResult(
                RemoveRolesFromGroupResultCode.InvalidRoleIdsError,
                "All role ids are invalid (not found or not assigned to group)",
                0,
                request.RoleIds
            );
        }

        // Role removal rules:
        // 1. If owner role is not being removed -> allow removal
        // 2. If owner role is being removed AND group has no users -> allow removal
        // 3. If owner role is being removed AND at least one user has ownership elsewhere -> allow removal
        // 4. If owner role is being removed AND no users have ownership elsewhere -> block
        var blockedOwnerRoleIds = new List<int>();
        var ownerRole = rolesToRemove.FirstOrDefault(r =>
            r.Name == RoleConstants.OWNER_ROLE_NAME && r.IsSystemDefined
        );
        var usersInGroup = groupEntity.Users?.ToList() ?? [];

        if (ownerRole != null && usersInGroup.Count > 0)
        {
            var anyUserHasOwnershipElsewhere =
                await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
                    usersInGroup.Select(u => u.Id),
                    groupEntity.AccountId,
                    request.GroupId
                );

            if (!anyUserHasOwnershipElsewhere)
            {
                blockedOwnerRoleIds.Add(ownerRole.Id);
                _logger.LogWarning(
                    "Cannot remove owner role {RoleId} from group {GroupId} - no user has ownership elsewhere",
                    ownerRole.Id,
                    request.GroupId
                );

                // If only the owner role is being removed, return error
                if (rolesToRemove.Count == 1)
                {
                    return new RemoveRolesFromGroupResult(
                        RemoveRolesFromGroupResultCode.OwnerRoleRemovalBlockedError,
                        "Cannot remove owner role from group - no user has ownership elsewhere. "
                            + "Add another owner first, or remove users from this group.",
                        0,
                        [],
                        blockedOwnerRoleIds
                    );
                }

                // Filter out the blocked owner role
                rolesToRemove = rolesToRemove.Where(r => r.Id != ownerRole.Id).ToList();
            }
        }

        foreach (var role in rolesToRemove)
        {
            groupEntity.Roles!.Remove(role);
        }

        if (rolesToRemove.Count > 0)
        {
            await _groupRepo.UpdateAsync(groupEntity);
        }

        // Calculate invalid IDs (requested but not actually removed, excluding blocked)
        var invalidRoleIds = request
            .RoleIds.Except(rolesToRemove.Select(r => r.Id))
            .Except(blockedOwnerRoleIds)
            .ToList();

        // Return appropriate result
        if (blockedOwnerRoleIds.Count > 0 || invalidRoleIds.Count > 0)
        {
            _logger.LogInformation(
                "Some roles were not removed: invalid {@InvalidRoleIds}, blocked owner {@BlockedOwnerRoleIds}",
                invalidRoleIds,
                blockedOwnerRoleIds
            );
            return new RemoveRolesFromGroupResult(
                RemoveRolesFromGroupResultCode.PartialSuccess,
                blockedOwnerRoleIds.Count > 0
                    ? "Some roles could not be removed (invalid or owner role removal blocked)"
                    : "Some role ids are invalid (not found or not assigned to group)",
                rolesToRemove.Count,
                invalidRoleIds,
                blockedOwnerRoleIds
            );
        }

        return new RemoveRolesFromGroupResult(
            RemoveRolesFromGroupResultCode.Success,
            string.Empty,
            rolesToRemove.Count,
            []
        );
    }

    public async Task<DeleteGroupResult> DeleteGroupAsync(int groupId)
    {
        var groupEntity = await _groupRepo.GetAsync(
            g => g.Id == groupId,
            include: q => q.Include(g => g.Users).Include(g => g.Roles)
        );
        if (groupEntity == null)
        {
            _logger.LogWarning("Group with Id {GroupId} not found", groupId);
            return new DeleteGroupResult(
                DeleteGroupResultCode.GroupNotFoundError,
                $"Group with Id {groupId} not found"
            );
        }

        // Ownership check logic (same as removing all users from the group):
        // If group has owner role AND no user has ownership elsewhere -> block deletion
        var groupHasOwnerRole =
            groupEntity.Roles?.Any(r =>
                r.Name == RoleConstants.OWNER_ROLE_NAME && r.IsSystemDefined
            ) ?? false;
        var usersInGroup = groupEntity.Users?.ToList() ?? [];

        if (groupHasOwnerRole && usersInGroup.Count > 0)
        {
            var anyUserHasOwnershipElsewhere =
                await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
                    usersInGroup.Select(u => u.Id),
                    groupEntity.AccountId,
                    groupId
                );

            if (!anyUserHasOwnershipElsewhere)
            {
                _logger.LogWarning(
                    "Cannot delete group {GroupId} - it has the owner role and no user has ownership elsewhere",
                    groupId
                );
                return new DeleteGroupResult(
                    DeleteGroupResultCode.GroupHasSoleOwnersError,
                    "Cannot delete a group with the owner role when no user has ownership elsewhere. "
                        + "Add another owner first, or remove the owner role from this group."
                );
            }
        }

        await _groupRepo.DeleteAsync(groupEntity);

        _logger.LogInformation("Group with Id {GroupId} deleted", groupId);
        return new DeleteGroupResult(DeleteGroupResultCode.Success, string.Empty);
    }
}
