using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Models;

public sealed class RevealAccountKeyRequest(
    Guid accountId,
    KeyType keyType,
    string? currentPassword,
    string? totpCode
)
{
    public Guid AccountId { get; } = accountId;
    public KeyType KeyType { get; } = keyType;

    internal string? CurrentPassword { get; } = currentPassword;
    internal string? TotpCode { get; } = totpCode;
}
