namespace Corely.IAM.Models;

public record DeregisterRolesFromGroupRequest(List<int> RoleIds, int GroupId);
