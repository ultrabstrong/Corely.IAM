using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Processors;

internal interface IUserOwnershipProcessor
{
    /// <summary>
    /// Checks if the specified user is the sole owner of the specified account.
    /// A user is considered a sole owner if they have the Owner role (directly or via a group)
    /// and no other user in the account has the Owner role.
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <param name="accountId">The account ID to check ownership for</param>
    /// <returns>Result indicating whether the user is the sole owner, has the owner role,
    /// and whether ownership comes from a single source (direct OR group) vs multiple sources (direct AND group)</returns>
    Task<IsSoleOwnerOfAccountResult> IsSoleOwnerOfAccountAsync(int userId, int accountId);

    /// <summary>
    /// Checks if the specified user has the Owner role from a source other than the specified group.
    /// This is used when removing users from a group to determine if they retain ownership elsewhere.
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <param name="accountId">The account ID to check ownership for</param>
    /// <param name="excludeGroupId">The group ID to exclude from the ownership check</param>
    /// <returns>True if the user has ownership from direct assignment or another group</returns>
    Task<bool> HasOwnershipOutsideGroupAsync(int userId, int accountId, int excludeGroupId);

    /// <summary>
    /// Checks if any user in the list has the Owner role from a source other than the specified group.
    /// This is used when removing all users from a group or deleting a group to determine if
    /// at least one user retains ownership elsewhere.
    /// </summary>
    /// <param name="userIds">The user IDs to check</param>
    /// <param name="accountId">The account ID to check ownership for</param>
    /// <param name="excludeGroupId">The group ID to exclude from the ownership check</param>
    /// <returns>True if any user has ownership from direct assignment or another group</returns>
    Task<bool> AnyUserHasOwnershipOutsideGroupAsync(
        IEnumerable<int> userIds,
        int accountId,
        int excludeGroupId
    );
}
