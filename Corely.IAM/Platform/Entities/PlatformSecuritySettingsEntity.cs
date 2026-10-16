using Corely.DataAccess.Interfaces.Entities;

namespace Corely.IAM.Platform.Entities;

internal class PlatformSecuritySettingsEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public const int SINGLE_ROW_ID = 1;

    public int Id { get; set; }
    public int PasswordMinimumLength { get; set; }
    public int PasswordMaximumLength { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireDigit { get; set; }
    public bool RequireNonAlphanumeric { get; set; }
    public int MaxLoginAttempts { get; set; }
    public int LockoutCooldownSeconds { get; set; }
    public int AuthTokenTtlSeconds { get; set; }
    public int AuthSessionTtlSeconds { get; set; }
    public int MfaChallengeTimeoutSeconds { get; set; }
    public int TotpRecoveryCodeCount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
