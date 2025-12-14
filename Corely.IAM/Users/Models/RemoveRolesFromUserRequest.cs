namespace Corely.IAM.Users.Models;

internal record RemoveRolesFromUserRequest(
    List<int> RoleIds,
    int UserId,
    bool BypassAuthorization = false
);
