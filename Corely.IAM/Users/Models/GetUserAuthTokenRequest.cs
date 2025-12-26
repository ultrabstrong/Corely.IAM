namespace Corely.IAM.Users.Models;

public record GetUserAuthTokenRequest(int UserId, string DeviceId, Guid? AccountPublicId = null);
