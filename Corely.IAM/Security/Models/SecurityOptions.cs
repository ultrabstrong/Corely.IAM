namespace Corely.IAM.Security.Models;

public class SecurityOptions
{
    public const string NAME = "SecurityOptions";
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutCooldownSeconds { get; set; } = 900;
    public int AuthTokenTtlSeconds { get; set; } = 3600;
}
