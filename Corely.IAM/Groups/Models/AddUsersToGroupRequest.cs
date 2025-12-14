namespace Corely.IAM.Groups.Models;

internal record AddUsersToGroupRequest(List<int> UserIds, int GroupId);
