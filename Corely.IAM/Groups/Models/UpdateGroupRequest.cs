namespace Corely.IAM.Groups.Models;

public record UpdateGroupRequest(Guid GroupId, Guid AccountId, string Name, string? Description);
