using Corely.IAM.Audits.Enums;
using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Enums;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.Web.Extensions;

internal static class SettingChangeExtensions
{
    extension(SettingChange change)
    {
        public string Consequence() =>
            change.Setting switch
            {
                PlatformSetting.AuditEnabled => AuditEnabledText(Now<bool>(change)),
                PlatformSetting.AuditMaxRetentionDays => RetentionText(
                    Was<int>(change),
                    Now<int>(change)
                ),
                PlatformSetting.AuditAllowedActions => ActionsText(
                    change,
                    dropped: action =>
                        $"Accounts can no longer record {action}. Accounts that record {action} now will stop.",
                    added: action => $"Accounts can choose to record {action} again."
                ),
                PlatformSetting.SystemContextActions => ActionsText(
                    change,
                    action => $"{action} by system processes will no longer be recorded.",
                    action => $"{action} by system processes will be recorded."
                ),
                PlatformSetting.AccountlessActions => ActionsText(
                    change,
                    action => $"{action} outside any account will no longer be recorded.",
                    action => $"{action} outside any account will be recorded."
                ),
                PlatformSetting.PasswordMinimumLength => Now<int>(change) < Was<int>(change)
                    ? $"Passwords as short as {Now<int>(change)} characters will be accepted when a password is next set."
                    : $"Passwords shorter than {Now<int>(change)} characters will be refused when a password is next set.",
                PlatformSetting.PasswordMaximumLength => Now<int>(change) > Was<int>(change)
                    ? $"Passwords up to {Now<int>(change)} characters will be accepted when a password is next set."
                    : $"Passwords longer than {Now<int>(change)} characters will be refused when a password is next set.",
                PlatformSetting.RequireUppercase => RuleText(
                    Now<bool>(change),
                    "an uppercase letter"
                ),
                PlatformSetting.RequireLowercase => RuleText(
                    Now<bool>(change),
                    "a lowercase letter"
                ),
                PlatformSetting.RequireDigit => RuleText(Now<bool>(change), "a digit"),
                PlatformSetting.RequireNonAlphanumeric => RuleText(Now<bool>(change), "a symbol"),
                PlatformSetting.MaxLoginAttempts => Now<int>(change) > Was<int>(change)
                    ? $"Users can fail to sign in {Now<int>(change)} times before being locked out, up from {Was<int>(change)}."
                    : $"Users can fail to sign in {Now<int>(change)} times before being locked out, down from {Was<int>(change)}.",
                PlatformSetting.LockoutCooldownSeconds => Now<int>(change) < Was<int>(change)
                    ? $"Locked out users can try again after {Duration(Now<int>(change))}, instead of {Duration(Was<int>(change))}."
                    : $"Locked out users must wait {Duration(Now<int>(change))} before trying again, instead of {Duration(Was<int>(change))}.",
                PlatformSetting.AuthTokenTtlSeconds => Now<int>(change) > Was<int>(change)
                    ? $"Sign in tokens will stay valid for {Duration(Now<int>(change))}, instead of {Duration(Was<int>(change))}."
                    : $"Sign in tokens will expire after {Duration(Now<int>(change))}, instead of {Duration(Was<int>(change))}.",
                PlatformSetting.AuthSessionTtlSeconds => Now<int>(change) > Was<int>(change)
                    ? $"Sessions will last {Duration(Now<int>(change))}, instead of {Duration(Was<int>(change))}."
                    : $"Sessions will end after {Duration(Now<int>(change))}, instead of {Duration(Was<int>(change))}.",
                PlatformSetting.AuthSessionIdleTimeoutSeconds => Now<int>(change) > Was<int>(change)
                    ? $"Idle sessions will stay open for {Duration(Now<int>(change))}, instead of {Duration(Was<int>(change))}."
                    : $"Idle sessions will end after {Duration(Now<int>(change))}, instead of {Duration(Was<int>(change))}.",
                PlatformSetting.RefreshReuseGraceSeconds => Now<int>(change) == 0
                    ? "Any reuse of a replaced refresh token will end its session at once."
                    : $"A replaced refresh token reused within {Duration(Now<int>(change))} will be refused without ending its session.",
                PlatformSetting.MfaChallengeTimeoutSeconds =>
                    $"Users will have {Duration(Now<int>(change))} to enter a code, instead of {Duration(Was<int>(change))}.",
                PlatformSetting.TotpRecoveryCodeCount =>
                    $"New sets of recovery codes will hold {Now<int>(change)} codes, instead of {Was<int>(change)}.",
                PlatformSetting.ApiKeysEnabled => Now<bool>(change)
                    ? "Accounts can create API keys, and API keys can sign in."
                    : "API keys stop signing in and renewing, and none can be created. Signed in keys stop when their access token expires.",
                PlatformSetting.PlatformAccountApiKeysEnabled => Now<bool>(change)
                    ? "The platform account can hold API keys. A platform key signs in without two factor."
                    : "API keys in the platform account stop signing in, and none can be created there.",
                PlatformSetting.ApiKeyMaxLifetimeDays =>
                    $"New API key secrets can live up to {Now<int>(change)} days, instead of {Was<int>(change)}.",
                PlatformSetting.ApiKeyResourceTypes => ResourceTypesText(
                    Was<ResourceTypeSet>(change),
                    Now<ResourceTypeSet>(change)
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(change)),
            };
    }

    private static T Was<T>(SettingChange change) => (T)change.OldValue;

    private static T Now<T>(SettingChange change) => (T)change.NewValue;

    private static string AuditEnabledText(bool enabled) =>
        enabled
            ? "Auditing will resume on every account, as far as each account chooses to record."
            : "Auditing will stop on every account. Only deleting a user or an account will still be recorded.";

    private static string RetentionText(int was, int now) =>
        now < was
            ? $"Audit entries older than {now} days will be deleted, on every account."
            : $"Accounts can keep audit entries for up to {now} days.";

    private static string ResourceTypesText(ResourceTypeSet was, ResourceTypeSet now)
    {
        var added = now.Types.Where(t => !was.Contains(t)).ToList();
        var removed = was.Types.Where(t => !now.Contains(t)).ToList();
        return string.Join(
            " ",
            new[]
            {
                added.Count > 0
                    ? $"API keys can use their permissions on {string.Join(", ", added)}."
                    : null,
                removed.Count > 0
                    ? $"API keys lose their permissions on {string.Join(", ", removed)}."
                    : null,
            }.OfType<string>()
        );
    }

    private static string RuleText(bool required, string need) =>
        required
            ? $"Passwords will need {need} when a password is next set."
            : $"Passwords will no longer need {need} when a password is next set.";

    private static string ActionsText(
        SettingChange change,
        Func<AuditActions, string> dropped,
        Func<AuditActions, string> added
    )
    {
        var was = Was<AuditActions>(change);
        var now = Now<AuditActions>(change);
        var sentences = Enum.GetValues<AuditActions>()
            .Where(action => action != AuditActions.None && action != AuditActions.All)
            .Select(action =>
                (was & action) != 0 && (now & action) == 0 ? dropped(action)
                : (was & action) == 0 && (now & action) != 0 ? added(action)
                : null
            )
            .OfType<string>();
        return string.Join(" ", sentences);
    }

    private static string Duration(int seconds)
    {
        var (unit, size) =
            seconds % 86400 == 0 ? ("day", 86400)
            : seconds % 3600 == 0 ? ("hour", 3600)
            : seconds % 60 == 0 ? ("minute", 60)
            : ("second", 1);
        var count = seconds / size;
        return $"{count} {unit}{(count == 1 ? "" : "s")}";
    }
}
