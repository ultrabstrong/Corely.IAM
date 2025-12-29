namespace Corely.IAM.Users.Models;

internal record RemoveRolesFromUserRequest(
    List<Guid> RoleIds,
    Guid UserId,
    bool BypassAuthorization = false
);
