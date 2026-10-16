namespace Corely.IAM.Users.Models;

internal record GetUserAuthTokenRequest(
    Guid UserId,
    string DeviceId,
    Guid? AccountId = null,
    DateTime? SessionStartedUtc = null
);
