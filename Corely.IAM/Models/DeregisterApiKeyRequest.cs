namespace Corely.IAM.Models;

public record DeregisterApiKeyRequest(Guid ApiKeyId, Guid AccountId);
