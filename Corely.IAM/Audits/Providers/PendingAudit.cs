using Corely.IAM.Audits.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Audits.Providers;

internal sealed record PendingAudit(
    AuditCall Call,
    string Operation,
    DateTime StartedUtc,
    UserContext? Before,
    Guid? AccountId,
    bool MemberBefore,
    string? Details
)
{
    public bool UsesCurrentAccount => Call.AccountId is null;

    public UserContext? Context(UserContext? after) => Before ?? after;

    public Guid? AccountIdAfter(UserContext? after) =>
        AccountId ?? (UsesCurrentAccount ? after?.CurrentAccount?.Id : null);
}
