using System.Linq.Expressions;
using Corely.IAM.Audits.Entities;

namespace Corely.IAM.Audits.Providers;

internal sealed record AuditAccess(
    bool Everything,
    IReadOnlySet<Guid> AccountIds,
    Guid? ViewerUserId
)
{
    public static AuditAccess None { get; } = new(false, new HashSet<Guid>(), null);

    public static AuditAccess All { get; } = new(true, new HashSet<Guid>(), null);

    public bool CanReach(Guid? accountId) =>
        Everything || (accountId is { } id && AccountIds.Contains(id));

    public Expression<Func<AuditEntryEntity, bool>> VisibleEntries()
    {
        if (Everything)
            return e => true;

        var accountIds = AccountIds.ToList();
        var viewer = ViewerUserId;
        return e =>
            (viewer != null && e.ActorUserId == viewer)
            || (e.AccountId != null && accountIds.Contains(e.AccountId.Value));
    }
}
