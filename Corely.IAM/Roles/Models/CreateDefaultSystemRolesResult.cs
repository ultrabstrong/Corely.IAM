namespace Corely.IAM.Roles.Models;

public record CreateDefaultSystemRolesResult(Guid OwnerRoleId, Guid AdminRoleId, Guid ReaderRoleId);
