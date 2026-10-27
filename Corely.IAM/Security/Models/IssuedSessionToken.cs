namespace Corely.IAM.Security.Models;

internal sealed record IssuedSessionToken(
    string Token,
    Guid TokenId,
    string RefreshToken,
    DateTime RefreshExpiresUtc
);
