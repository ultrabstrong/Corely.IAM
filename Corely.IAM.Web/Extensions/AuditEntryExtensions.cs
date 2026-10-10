using Corely.IAM.Audits.Models;

namespace Corely.IAM.Web.Extensions;

internal static class AuditEntryExtensions
{
    private const string SYSTEM_ACTOR = "System";
    private const string UNKNOWN_ACTOR = "Unknown user";
    private const string NO_ACCOUNT = "Outside any account";

    extension(AuditEntry entry)
    {
        public string ActorDisplayName() =>
            entry.ActorName
            ?? entry.ActorUserId?.ToString()
            ?? (entry.Cohort == AuditCohort.SystemContext ? SYSTEM_ACTOR : UNKNOWN_ACTOR);

        public string AccountDisplayName() =>
            entry.AccountName ?? entry.AccountId?.ToString() ?? NO_ACCOUNT;

        public IEnumerable<Guid> ShownResourceIds(int max) => entry.ResourceIds.Take(max);

        public int HiddenResourceIdCount(int max) => Math.Max(entry.ResourceIds.Count - max, 0);
    }
}
