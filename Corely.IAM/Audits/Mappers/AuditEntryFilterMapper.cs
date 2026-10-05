using System.Linq.Expressions;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Models;

namespace Corely.IAM.Audits.Mappers;

internal static class AuditEntryFilterMapper
{
    extension(AuditEntryFilter filter)
    {
        public Expression<Func<AuditEntryEntity, bool>> ToPredicate()
        {
            var accountId = filter.AccountId;
            var actorUserId = filter.ActorUserId;
            var cohort = filter.Cohort;
            var action = filter.Action;
            var resourceType = string.IsNullOrWhiteSpace(filter.ResourceType)
                ? null
                : filter.ResourceType;
            var resultCode = string.IsNullOrWhiteSpace(filter.ResultCode)
                ? null
                : filter.ResultCode;
            var fromUtc = filter.FromUtc;
            var toUtc = filter.ToUtc;
            var excludePlatformMembers = !filter.IncludePlatformMembers;

            return e =>
                (accountId == null || e.AccountId == accountId)
                && (actorUserId == null || e.ActorUserId == actorUserId)
                && (cohort == null || e.Cohort == cohort)
                && (action == null || e.Action == action)
                && (resourceType == null || e.ResourceType == resourceType)
                && (resultCode == null || e.ResultCode == resultCode)
                && (fromUtc == null || e.OccurredUtc >= fromUtc)
                && (toUtc == null || e.OccurredUtc <= toUtc)
                && (!excludePlatformMembers || e.Cohort != AuditCohort.PlatformMember);
        }
    }

    extension(PurgeAuditEntriesRequest request)
    {
        public Expression<Func<AuditEntryEntity, bool>> ToPredicate()
        {
            var accountId = request.AccountId;
            var olderThanUtc = request.OlderThanUtc;
            return e =>
                e.AccountId == accountId && (olderThanUtc == null || e.OccurredUtc < olderThanUtc);
        }
    }
}
