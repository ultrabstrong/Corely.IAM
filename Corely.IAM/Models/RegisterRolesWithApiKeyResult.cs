using Corely.IAM.ApiKeys.Models;

namespace Corely.IAM.Models;

public record RegisterRolesWithApiKeyResult(
    AssignRolesToApiKeyResultCode ResultCode,
    string Message,
    int RegisteredRoleCount,
    List<Guid> InvalidRoleIds
);
