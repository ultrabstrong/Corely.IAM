namespace Corely.IAM.Models;

public record DeregisterUsersFromGroupRequest(List<Guid> UserIds, Guid GroupId);
