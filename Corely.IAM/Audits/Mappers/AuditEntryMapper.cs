using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Models;

namespace Corely.IAM.Audits.Mappers;

internal static class AuditEntryMapper
{
    private const char RESOURCE_ID_SEPARATOR = ',';

    extension(AuditEntryEntity entity)
    {
        public AuditEntry ToModel() =>
            new(
                entity.Id,
                entity.OccurredUtc,
                entity.ActorUserId,
                entity.Cohort,
                entity.AccountId,
                entity.Source,
                entity.Service,
                entity.Operation,
                entity.Action,
                entity.ResourceType,
                entity.ResourceIds.ToResourceIdList(),
                entity.ResultCode,
                entity.Details
            );
    }

    extension(IEnumerable<Guid> resourceIds)
    {
        public string? ToResourceIdsColumn()
        {
            var ids = resourceIds.Where(id => id != Guid.Empty).Distinct().ToList();
            return ids.Count == 0 ? null : string.Join(RESOURCE_ID_SEPARATOR, ids);
        }
    }

    extension(string? column)
    {
        public IReadOnlyList<Guid> ToResourceIdList() =>
            string.IsNullOrWhiteSpace(column)
                ? []
                :
                [
                    .. column
                        .Split(RESOURCE_ID_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                        .Select(part => Guid.TryParse(part, out var id) ? id : Guid.Empty)
                        .Where(id => id != Guid.Empty),
                ];
    }
}
