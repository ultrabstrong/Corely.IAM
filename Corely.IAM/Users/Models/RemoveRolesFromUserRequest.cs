using Corely.IAM.Security.Models;

namespace Corely.IAM.Users.Models;

internal record RemoveRolesFromUserRequest(List<Guid> RoleIds, Guid UserId, AccountScope AccountId);
