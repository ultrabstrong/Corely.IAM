namespace Corely.IAM.Users.Models;

public record RevokeUserAuthTokenRequest(
    Guid UserId,
    string TokenId,
    string DeviceId,
    Guid? AccountId = null
);
