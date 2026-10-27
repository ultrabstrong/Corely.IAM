namespace Corely.IAM.ApiKeys.Models;

internal record CreateApiKeyRequest(
    string Name,
    Guid OwnerAccountId,
    string? Description = null,
    DateTime? ExpiresUtc = null
);
