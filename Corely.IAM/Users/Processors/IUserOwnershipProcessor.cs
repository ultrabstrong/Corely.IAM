using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Processors;

internal interface IUserOwnershipProcessor
{
    Task<IsSoleOwnerOfAccountResult> IsSoleOwnerOfAccountAsync(Guid userId, Guid accountId);

    Task<bool> HasOwnershipOutsideGroupAsync(Guid userId, Guid accountId, Guid excludeGroupId);

    Task<bool> AnyUserHasOwnershipOutsideGroupAsync(
        IEnumerable<Guid> userIds,
        Guid accountId,
        Guid excludeGroupId
    );
}
