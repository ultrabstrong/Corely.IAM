namespace Corely.IAM.Users.Models;

internal record RevokeUserAuthTokenRequest(
    Guid UserId,
    string TokenId,
    string DeviceId,
    Guid? AccountId = null
);
