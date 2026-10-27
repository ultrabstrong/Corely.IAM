using Corely.IAM.Security.Models;

namespace Corely.IAM.Groups.Models;

internal record RemoveRolesFromGroupRequest(
    List<Guid> RoleIds,
    Guid GroupId,
    AccountScope AccountId
);
