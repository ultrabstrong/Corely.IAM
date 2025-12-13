namespace Corely.IAM.Users.Models;

public record UserAuthTokenRequest(int UserId, Guid? AccountPublicId = null);
