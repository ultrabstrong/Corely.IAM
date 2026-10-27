namespace Corely.IAM.Models;

public record RegisterRolesWithApiKeyRequest(List<Guid> RoleIds, Guid ApiKeyId, Guid AccountId);
