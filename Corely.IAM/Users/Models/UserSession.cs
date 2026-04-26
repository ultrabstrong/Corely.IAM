namespace Corely.IAM.Users.Models;

public record UserSession(
    Guid SessionId,
    string DeviceId,
    Guid? SignedInAccountId,
    DateTime IssuedUtc,
    DateTime ExpiresUtc,
    bool IsCurrentSession
);
