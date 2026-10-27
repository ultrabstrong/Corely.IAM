namespace Corely.IAM.Models;

public record RegisterApiKeysWithGroupRequest(List<Guid> ApiKeyIds, Guid GroupId, Guid AccountId);
