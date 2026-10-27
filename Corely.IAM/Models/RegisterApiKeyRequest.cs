namespace Corely.IAM.Models;

public record RegisterApiKeyRequest(
    string Name,
    Guid AccountId,
    string? Description = null,
    DateTime? ExpiresUtc = null
);
