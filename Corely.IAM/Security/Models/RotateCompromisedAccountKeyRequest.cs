using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Models;

public sealed class RotateCompromisedAccountKeyRequest(
    Guid accountId,
    KeyType keyType,
    AccountKeyProof proof
)
{
    public Guid AccountId { get; } = accountId;
    public KeyType KeyType { get; } = keyType;

    internal AccountKeyProof Proof { get; } = proof;
}
