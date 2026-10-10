using Corely.IAM.Security.Constants;

namespace Corely.IAM.Audits.Models;

public sealed record AuditCall(string Service, AuthAction Action, string ResourceType)
{
    public Guid? AccountId { get; init; }
    public IReadOnlyList<Guid> ResourceIds { get; init; } = [];
    public string? ActorUsername { get; init; }
    public string? ActorMfaChallengeToken { get; init; }
    public AuditDetail Detail { get; init; }
}
