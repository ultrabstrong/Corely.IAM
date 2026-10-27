using Corely.IAM.Security.Models;

namespace Corely.IAM.Groups.Models;

internal record RemoveUsersFromGroupRequest(
    List<Guid> UserIds,
    Guid GroupId,
    AccountScope AccountId
);
