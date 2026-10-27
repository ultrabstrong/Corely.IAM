namespace Corely.IAM.Models;

public record DeregisterRolesFromApiKeyRequest(List<Guid> RoleIds, Guid ApiKeyId, Guid AccountId);
