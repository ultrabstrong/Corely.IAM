namespace Corely.IAM.Permissions.Models;

public record UpdatePermissionRequest(Guid PermissionId, Guid AccountId, string? Description);
