namespace Corely.IAM.Users.Models;

internal record AssignRolesToUserRequest(
    List<int> RoleIds,
    int UserId,
    bool BypassAuthorization = false
);
