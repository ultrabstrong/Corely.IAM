using Corely.IAM.ApiKeys.Enums;

namespace Corely.IAM.ApiKeys.Models;

public record RotateApiKeySecretRequest(
    Guid ApiKeyId,
    Guid AccountId,
    ApiKeySlot Slot,
    DateTime? ExpiresUtc = null
);
