namespace Corely.IAM.Accounts.Models;

public record AddUserToAccountRequest(Guid UserId, Guid AccountId);
