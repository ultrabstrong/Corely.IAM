namespace Corely.IAM.Users.Models;

public record GetUserAuthTokenRequest(Guid UserId, string DeviceId, Guid? AccountId = null);
