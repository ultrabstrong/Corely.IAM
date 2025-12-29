namespace Corely.IAM.Groups.Models;

public record RemoveUsersFromGroupRequest(List<Guid> UserIds, Guid GroupId);
