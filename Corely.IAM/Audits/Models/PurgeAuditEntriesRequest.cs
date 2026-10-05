namespace Corely.IAM.Audits.Models;

public record PurgeAuditEntriesRequest(Guid? AccountId, DateTime? OlderThanUtc = null);
