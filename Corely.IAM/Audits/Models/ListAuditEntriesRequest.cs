namespace Corely.IAM.Audits.Models;

public record ListAuditEntriesRequest(AuditEntryFilter? Filter = null, int Skip = 0, int Take = 25);
