using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Web.Endpoints;

internal sealed record AuditEntryResponse(
    Guid Id,
    DateTime OccurredUtc,
    Guid? ActorUserId,
    Guid? ActorApiKeyId,
    string? ActorName,
    string Service,
    string Operation,
    AuthAction Action,
    string ResourceType,
    IReadOnlyList<Guid> ResourceIds,
    string ResultCode,
    string? Details
)
{
    public static AuditEntryResponse From(AuditEntry entry) =>
        new(
            entry.Id,
            entry.OccurredUtc,
            entry.ActorUserId,
            entry.ActorApiKeyId,
            entry.ActorName,
            entry.Service,
            entry.Operation,
            entry.Action,
            entry.ResourceType,
            entry.ResourceIds,
            entry.ResultCode,
            entry.Details
        );
}
