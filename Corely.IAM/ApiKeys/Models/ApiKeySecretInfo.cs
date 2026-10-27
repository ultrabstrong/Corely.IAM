using Corely.IAM.ApiKeys.Enums;

namespace Corely.IAM.ApiKeys.Models;

public record ApiKeySecretInfo(
    ApiKeySlot Slot,
    string Prefix,
    DateTime CreatedUtc,
    DateTime? ExpiresUtc,
    DateTime? LastUsedUtc
);
