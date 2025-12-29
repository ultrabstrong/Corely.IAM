namespace Corely.IAM.Groups.Models;

internal record CreateGroupRequest(string GroupName, Guid OwnerAccountId);
