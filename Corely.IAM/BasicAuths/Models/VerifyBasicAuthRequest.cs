namespace Corely.IAM.BasicAuths.Models;

internal record VerifyBasicAuthRequest(Guid UserId, string Password);
