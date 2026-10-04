namespace Corely.IAM.Audits.Models;

public sealed record AuditOutcome(string ResultCode)
{
    public IReadOnlyList<Guid> ResourceIds { get; init; } = [];
    public Guid? ActorUserId { get; init; }

    public static AuditOutcome Of(Enum resultCode, params IEnumerable<Guid?> resourceIds) =>
        new(resultCode.ToString())
        {
            ResourceIds = [.. resourceIds.Where(id => id.HasValue).Select(id => id!.Value)],
        };
}
