namespace Corely.IAM.Groups.Models;

public record RemoveUsersFromGroupRequest(List<int> UserIds, int GroupId);
