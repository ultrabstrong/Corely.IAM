namespace Corely.IAM.Models;

public record DeregisterApiKeysFromGroupRequest(List<Guid> ApiKeyIds, Guid GroupId, Guid AccountId);
