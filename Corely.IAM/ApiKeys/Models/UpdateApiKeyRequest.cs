namespace Corely.IAM.ApiKeys.Models;

public record UpdateApiKeyRequest(Guid ApiKeyId, Guid AccountId, string Name, string? Description);
