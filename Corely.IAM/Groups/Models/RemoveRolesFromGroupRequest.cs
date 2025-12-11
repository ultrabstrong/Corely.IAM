namespace Corely.IAM.Groups.Models;

internal record RemoveRolesFromGroupRequest(
    List<int> RoleIds,
    int GroupId,
    bool BypassAuthorization = false
);
