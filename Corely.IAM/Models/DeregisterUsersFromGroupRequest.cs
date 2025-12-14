namespace Corely.IAM.Models;

public record DeregisterUsersFromGroupRequest(List<int> UserIds, int GroupId);
