namespace Corely.IAM.Models;

public record RegisterUsersWithGroupRequest(List<Guid> UserIds, Guid GroupId);
