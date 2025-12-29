using Corely.IAM.Users.Models;

namespace Corely.IAM.Models;

public record RegisterRolesWithUserResult(
    AssignRolesToUserResultCode ResultCode,
    string Message,
    int RegisteredRoleCount,
    List<Guid> InvalidRoleIds = null
);
