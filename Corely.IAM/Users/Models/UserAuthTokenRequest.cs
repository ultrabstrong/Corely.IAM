namespace Corely.IAM.Users.Models;

internal record UserAuthTokenRequest(int UserId, int? AccountId = null);
