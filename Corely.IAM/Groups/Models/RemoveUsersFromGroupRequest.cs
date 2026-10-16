namespace Corely.IAM.Groups.Models;

internal record RemoveUsersFromGroupRequest(
    List<Guid> UserIds,
    Guid GroupId,
    Guid AccountId = default
);
