namespace Corely.IAM.Users.Models;

public record RevokeUserAuthTokenRequest(
    int UserId,
    string TokenId,
    string DeviceId,
    int? AccountId = null
);
