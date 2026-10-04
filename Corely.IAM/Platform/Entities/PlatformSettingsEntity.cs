using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Audits.Models;

namespace Corely.IAM.Platform.Entities;

internal class PlatformSettingsEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public bool AuditEnabled { get; set; }
    public int AuditMaxRetentionDays { get; set; }
    public AuditActions AuditAllowedActions { get; set; }
    public AuditActions SystemContextActions { get; set; }
    public AuditActions AccountlessActions { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
