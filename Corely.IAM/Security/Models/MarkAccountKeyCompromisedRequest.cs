using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Models;

public sealed class MarkAccountKeyCompromisedRequest(
    Guid accountId,
    KeyType keyType,
    int version,
    AccountKeyProof proof
)
{
    public Guid AccountId { get; } = accountId;
    public KeyType KeyType { get; } = keyType;
    public int Version { get; } = version;

    internal AccountKeyProof Proof { get; } = proof;
}
