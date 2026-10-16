using System.Text.RegularExpressions;
using Corely.IAM.Audits.Models;

namespace Corely.IAM.Web.Extensions;

internal static partial class AuditEntryExtensions
{
    private const string SYSTEM_ACTOR = "System";
    private const string UNKNOWN_ACTOR = "Unknown user";
    private const string NO_ACCOUNT = "—";
    private const string ASYNC_SUFFIX = "Async";
    private const string ERROR_SUFFIX = "Error";

    private static readonly Dictionary<string, string> _acronyms = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["mfa"] = "MFA",
        ["totp"] = "TOTP",
        ["id"] = "ID",
        ["ids"] = "IDs",
    };

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

        public string Description() => Sentence(Trimmed(entry.Operation, ASYNC_SUFFIX));

        public bool Succeeded() => entry.ResultCode == AuditEntry.SUCCESS_RESULT_CODE;

        public string ResultText() => Sentence(Trimmed(entry.ResultCode, ERROR_SUFFIX));
    }

    private static string Trimmed(string value, string suffix) =>
        value.Length > suffix.Length && value.EndsWith(suffix, StringComparison.Ordinal)
            ? value[..^suffix.Length]
            : value;

    private static string Sentence(string pascalCase)
    {
        var words = WordPattern()
            .Matches(pascalCase)
            .Select(m => _acronyms.GetValueOrDefault(m.Value) ?? m.Value.ToLowerInvariant())
            .ToList();
        if (words.Count == 0)
        {
            return pascalCase;
        }

        words[0] = char.ToUpperInvariant(words[0][0]) + words[0][1..];
        return string.Join(' ', words);
    }

    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z]?[a-z]+|[0-9]+")]
    private static partial Regex WordPattern();
}
