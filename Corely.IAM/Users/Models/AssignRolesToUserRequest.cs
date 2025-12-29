namespace Corely.IAM.Users.Models;

internal record AssignRolesToUserRequest(
    List<Guid> RoleIds,
    Guid UserId,
    bool BypassAuthorization = false
);
