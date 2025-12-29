namespace Corely.IAM.Groups.Models;

internal record AddUsersToGroupRequest(List<Guid> UserIds, Guid GroupId);
