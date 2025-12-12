namespace Corely.IAM.Users.Models;

public record UserAuthTokenRequest(int UserId, int? AccountId = null);
