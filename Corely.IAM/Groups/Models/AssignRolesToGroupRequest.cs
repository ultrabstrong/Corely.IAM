using Corely.IAM.Security.Models;

namespace Corely.IAM.Groups.Models;

internal record AssignRolesToGroupRequest(List<Guid> RoleIds, Guid GroupId, AccountScope AccountId);
