using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Users.Processors;

internal class UserOwnershipProcessor : IUserOwnershipProcessor
{
    private readonly IReadonlyRepo<RoleEntity> _roleRepo;
    private readonly ILogger<UserOwnershipProcessor> _logger;

    public UserOwnershipProcessor(
        IReadonlyRepo<RoleEntity> roleRepo,
        ILogger<UserOwnershipProcessor> logger
    )
    {
        _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

    public async Task<IsSoleOwnerOfAccountResult> IsSoleOwnerOfAccountAsync(
        int userId,
        int accountId
    )
    {
        // Check if user has the owner role (directly or via group)
        var userHasOwnerRole = await _roleRepo.AnyAsync(r =>
            r.AccountId == accountId
            && r.Name == RoleConstants.OWNER_ROLE_NAME
            && (
                r.Users!.Any(u => u.Id == userId)
                || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userId))
            )
        );

        if (!userHasOwnerRole)
        {
            _logger.LogDebug(
                "User {UserId} does not have owner role for account {AccountId}",
                userId,
                accountId
            );
            return new IsSoleOwnerOfAccountResult(IsSoleOwner: false, UserHasOwnerRole: false);
        }

        // Check if another owner exists (directly or via group) who is also in the account
        var otherOwnerExists = await _roleRepo.AnyAsync(r =>
            r.AccountId == accountId
            && r.Name == RoleConstants.OWNER_ROLE_NAME
            && (
                r.Users!.Any(u => u.Id != userId && u.Accounts!.Any(a => a.Id == accountId))
                || r.Groups!.Any(g =>
                    g.Users!.Any(u => u.Id != userId && u.Accounts!.Any(a => a.Id == accountId))
                )
            )
        );

        if (otherOwnerExists)
        {
            _logger.LogDebug(
                "User {UserId} is not sole owner of account {AccountId} - other owners exist",
                userId,
                accountId
            );
            return new IsSoleOwnerOfAccountResult(IsSoleOwner: false, UserHasOwnerRole: true);
        }

        _logger.LogDebug(
            "User {UserId} is the sole owner of account {AccountId}",
            userId,
            accountId
        );

        return new IsSoleOwnerOfAccountResult(IsSoleOwner: true, UserHasOwnerRole: true);
    }

    public async Task<bool> HasOwnershipOutsideGroupAsync(
        int userId,
        int accountId,
        int excludeGroupId
    )
    {
        // Check if user has the owner role directly assigned
        var hasDirectOwnership = await _roleRepo.AnyAsync(r =>
            r.AccountId == accountId
            && r.Name == RoleConstants.OWNER_ROLE_NAME
            && r.Users!.Any(u => u.Id == userId)
        );

        if (hasDirectOwnership)
        {
            _logger.LogDebug(
                "User {UserId} has direct owner role assignment for account {AccountId}",
                userId,
                accountId
            );
            return true;
        }

        // Check if user has the owner role via a different group
        var hasOwnershipViaOtherGroup = await _roleRepo.AnyAsync(r =>
            r.AccountId == accountId
            && r.Name == RoleConstants.OWNER_ROLE_NAME
            && r.Groups!.Any(g => g.Id != excludeGroupId && g.Users!.Any(u => u.Id == userId))
        );

        if (hasOwnershipViaOtherGroup)
        {
            _logger.LogDebug(
                "User {UserId} has owner role via another group for account {AccountId} (excluding group {ExcludeGroupId})",
                userId,
                accountId,
                excludeGroupId
            );
            return true;
        }

        _logger.LogDebug(
            "User {UserId} has no owner role outside of group {ExcludeGroupId} for account {AccountId}",
            userId,
            excludeGroupId,
            accountId
        );
        return false;
    }

    public async Task<bool> AnyUserHasOwnershipOutsideGroupAsync(
        IEnumerable<int> userIds,
        int accountId,
        int excludeGroupId
    )
    {
        foreach (var userId in userIds)
        {
            if (await HasOwnershipOutsideGroupAsync(userId, accountId, excludeGroupId))
            {
                return true;
            }
        }

        _logger.LogDebug(
            "No users in {@UserIds} have owner role outside of group {ExcludeGroupId} for account {AccountId}",
            userIds,
            excludeGroupId,
            accountId
        );
        return false;
    }
}
