namespace Corely.IAM.Groups.Models;

public record UpdateGroupRequest(Guid GroupId, string Name, string? Description);
