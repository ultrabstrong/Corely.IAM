namespace Corely.IAM.Users.Models;

public record UpdateUserRequest(Guid UserId, string Username, string Email);
