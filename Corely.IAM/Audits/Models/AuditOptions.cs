using System.Reflection;
using Corely.IAM.Audits.Constants;

namespace Corely.IAM.Audits.Models;

public class AuditOptions
{
    public const string NAME = "AuditOptions";
    public string? Source { get; set; }
    public int SettingsCacheTtlSeconds { get; set; } = 30;

    public string SourceName() =>
        string.IsNullOrWhiteSpace(Source)
            ? Assembly.GetEntryAssembly()?.GetName().Name ?? AuditConstants.UNKNOWN_SOURCE
            : Source;
}
