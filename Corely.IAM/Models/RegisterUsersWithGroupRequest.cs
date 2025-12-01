namespace Corely.IAM.Models;

public record RegisterUsersWithGroupRequest(List<int> UserIds, int GroupId);
