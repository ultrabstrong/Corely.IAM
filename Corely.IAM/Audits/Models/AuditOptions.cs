namespace Corely.IAM.Audits.Models;

public class AuditOptions
{
    public const string NAME = "AuditOptions";
    public string? Source { get; set; }
    public int SettingsCacheTtlSeconds { get; set; } = 30;
}
