using Corely.IAM.Groups.Models;

namespace Corely.IAM.Models;

public record RegisterRolesWithGroupResult(
    AssignRolesToGroupResultCode ResultCode,
    string Message,
    int RegisteredRoleCount,
    List<int> InvalidRoleIds = null
);
