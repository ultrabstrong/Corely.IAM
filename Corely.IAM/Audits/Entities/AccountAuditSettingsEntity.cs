using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Audits.Models;

namespace Corely.IAM.Audits.Entities;

internal class AccountAuditSettingsEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public Guid AccountId { get; set; }
    public AuditActions AccountMemberActions { get; set; }
    public AuditActions PlatformMemberActions { get; set; }
    public int RetentionDays { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
