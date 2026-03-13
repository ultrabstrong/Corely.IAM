namespace Corely.IAM.Security.Models;

public class SecurityOptions
{
    public const string NAME = "SecurityOptions";
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutCooldownSeconds { get; set; } = 900;
    public int AuthTokenTtlSeconds { get; set; } = 3600;
    public int MfaChallengeTimeoutSeconds { get; set; } = 300;
    public int TotpRecoveryCodeCount { get; set; } = 10;
    public string? GoogleClientId { get; set; }
}
