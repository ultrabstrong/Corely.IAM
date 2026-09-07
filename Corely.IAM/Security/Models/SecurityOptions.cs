namespace Corely.IAM.Security.Models;

public class SecurityOptions
{
    public const string NAME = "SecurityOptions";
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutCooldownSeconds { get; set; } = 900;
    public int AuthTokenTtlSeconds { get; set; } = 3600;
    public int AuthSessionTtlSeconds { get; set; } = 604800;
    public int MfaChallengeTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// How long a user's permissions stay cached before being reloaded. Absolute, measured from
    /// the load, so an active user still gets a refresh. A host that creates a scope per request
    /// never reaches this; one with a long-lived scope - a Blazor Server circuit lasts the whole
    /// browser session - relies on it for permission changes to take effect without a sign-out.
    /// </summary>
    public int PermissionCacheTtlSeconds { get; set; } = 30;
    public int TotpRecoveryCodeCount { get; set; } = 10;
    public string? GoogleClientId { get; set; }
}
