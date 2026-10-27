using Corely.IAM.Audits.Enums;
using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Enums;
using Corely.IAM.Platform.Models;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class SettingChangeExtensionsTests
{
    private static string Consequence(PlatformSetting setting, object was, object now) =>
        new SettingChange(setting, was, now, false).Consequence();

    [Fact]
    public void Consequence_AllowedActions_DroppingAFlagSaysAccountsStop()
    {
        var text = Consequence(
            PlatformSetting.AuditAllowedActions,
            AuditActions.All,
            AuditActions.All & ~AuditActions.Read
        );

        Assert.Equal(
            "Accounts can no longer record Read. Accounts that record Read now will stop.",
            text
        );
    }

    [Fact]
    public void Consequence_AllowedActions_AddingAFlagSaysAccountsCanChoose()
    {
        var text = Consequence(
            PlatformSetting.AuditAllowedActions,
            AuditActions.Create,
            AuditActions.Create | AuditActions.Read
        );

        Assert.Equal("Accounts can choose to record Read again.", text);
    }

    [Fact]
    public void Consequence_AllowedActions_NamesEveryChangedAction()
    {
        var text = Consequence(
            PlatformSetting.AuditAllowedActions,
            AuditActions.All,
            AuditActions.Create
        );

        Assert.Contains("Read", text);
        Assert.Contains("Update", text);
        Assert.Contains("Delete", text);
        Assert.Contains("Execute", text);
        Assert.DoesNotContain("Create", text);
    }

    [Theory]
    [InlineData(
        PlatformSetting.SystemContextActions,
        true,
        "Read by system processes will no longer be recorded."
    )]
    [InlineData(
        PlatformSetting.SystemContextActions,
        false,
        "Read by system processes will be recorded."
    )]
    [InlineData(
        PlatformSetting.AccountlessActions,
        true,
        "Read outside any account will no longer be recorded."
    )]
    [InlineData(
        PlatformSetting.AccountlessActions,
        false,
        "Read outside any account will be recorded."
    )]
    public void Consequence_LoggedActions(PlatformSetting setting, bool dropped, string expected)
    {
        var (was, now) = dropped
            ? (AuditActions.Read, AuditActions.None)
            : (AuditActions.None, AuditActions.Read);

        Assert.Equal(expected, Consequence(setting, was, now));
    }

    [Theory]
    [InlineData(false, "Auditing will stop on every account.")]
    [InlineData(true, "Auditing will resume on every account")]
    public void Consequence_AuditEnabled(bool enabled, string start)
    {
        Assert.StartsWith(start, Consequence(PlatformSetting.AuditEnabled, !enabled, enabled));
    }

    [Theory]
    [InlineData(365, 30, "Audit entries older than 30 days will be deleted, on every account.")]
    [InlineData(365, 400, "Accounts can keep audit entries for up to 400 days.")]
    public void Consequence_Retention(int was, int now, string expected)
    {
        Assert.Equal(expected, Consequence(PlatformSetting.AuditMaxRetentionDays, was, now));
    }

    [Theory]
    [InlineData(
        PlatformSetting.PasswordMinimumLength,
        8,
        6,
        "Passwords as short as 6 characters will be accepted when a password is next set."
    )]
    [InlineData(
        PlatformSetting.PasswordMinimumLength,
        8,
        12,
        "Passwords shorter than 12 characters will be refused when a password is next set."
    )]
    [InlineData(
        PlatformSetting.PasswordMaximumLength,
        128,
        200,
        "Passwords up to 200 characters will be accepted when a password is next set."
    )]
    [InlineData(
        PlatformSetting.PasswordMaximumLength,
        128,
        64,
        "Passwords longer than 64 characters will be refused when a password is next set."
    )]
    [InlineData(
        PlatformSetting.MaxLoginAttempts,
        5,
        10,
        "Users can fail to sign in 10 times before being locked out, up from 5."
    )]
    [InlineData(
        PlatformSetting.MaxLoginAttempts,
        5,
        3,
        "Users can fail to sign in 3 times before being locked out, down from 5."
    )]
    [InlineData(
        PlatformSetting.LockoutCooldownSeconds,
        900,
        60,
        "Locked out users can try again after 1 minute, instead of 15 minutes."
    )]
    [InlineData(
        PlatformSetting.LockoutCooldownSeconds,
        900,
        3600,
        "Locked out users must wait 1 hour before trying again, instead of 15 minutes."
    )]
    [InlineData(
        PlatformSetting.AuthTokenTtlSeconds,
        3600,
        7200,
        "Sign in tokens will stay valid for 2 hours, instead of 1 hour."
    )]
    [InlineData(
        PlatformSetting.AuthTokenTtlSeconds,
        3600,
        600,
        "Sign in tokens will expire after 10 minutes, instead of 1 hour."
    )]
    [InlineData(
        PlatformSetting.AuthSessionTtlSeconds,
        604800,
        1209600,
        "Sessions will last 14 days, instead of 7 days."
    )]
    [InlineData(
        PlatformSetting.AuthSessionTtlSeconds,
        604800,
        7200,
        "Sessions will end after 2 hours, instead of 7 days."
    )]
    [InlineData(
        PlatformSetting.AuthSessionIdleTimeoutSeconds,
        86400,
        172800,
        "Idle sessions will stay open for 2 days, instead of 1 day."
    )]
    [InlineData(
        PlatformSetting.AuthSessionIdleTimeoutSeconds,
        86400,
        3600,
        "Idle sessions will end after 1 hour, instead of 1 day."
    )]
    [InlineData(
        PlatformSetting.RefreshReuseGraceSeconds,
        30,
        60,
        "A replaced refresh token reused within 1 minute will be refused without ending its session."
    )]
    [InlineData(
        PlatformSetting.RefreshReuseGraceSeconds,
        30,
        0,
        "Any reuse of a replaced refresh token will end its session at once."
    )]
    [InlineData(
        PlatformSetting.MfaChallengeTimeoutSeconds,
        300,
        90,
        "Users will have 90 seconds to enter a code, instead of 5 minutes."
    )]
    [InlineData(
        PlatformSetting.TotpRecoveryCodeCount,
        10,
        20,
        "New sets of recovery codes will hold 20 codes, instead of 10."
    )]
    public void Consequence_Numbers(PlatformSetting setting, int was, int now, string expected)
    {
        Assert.Equal(expected, Consequence(setting, was, now));
    }

    [Theory]
    [InlineData(
        PlatformSetting.RequireUppercase,
        false,
        "Passwords will no longer need an uppercase letter when a password is next set."
    )]
    [InlineData(
        PlatformSetting.RequireLowercase,
        true,
        "Passwords will need a lowercase letter when a password is next set."
    )]
    [InlineData(
        PlatformSetting.RequireDigit,
        false,
        "Passwords will no longer need a digit when a password is next set."
    )]
    [InlineData(
        PlatformSetting.RequireNonAlphanumeric,
        true,
        "Passwords will need a symbol when a password is next set."
    )]
    public void Consequence_CharacterRules(PlatformSetting setting, bool now, string expected)
    {
        Assert.Equal(expected, Consequence(setting, !now, now));
    }
}
